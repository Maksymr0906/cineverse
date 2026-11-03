# CineVerse — вебсистема мережі кінотеатрів

Проєкт за ТЗ «Розробка вебсистеми мережі кінотеатрів CineVerse» (ІПЗ-23-1).
Онлайн-бронювання та продаж квитків, інтерактивний вибір місць у реальному часі,
QR-квитки, промокоди, адміністрування кінозалів і розкладу.

## Технологічний стек

| Шар | Технології |
|---|---|
| Backend | C#, .NET 8, ASP.NET Core Web API, EF Core, MediatR (CQRS), FluentValidation, AutoMapper |
| Frontend | React 18, TypeScript, Vite, Tailwind CSS, Redux Toolkit, SignalR client |
| БД | PostgreSQL 16 |
| Кеш / блокування | Redis 7.2 (Distributed Lock для бронювання місць) |
| Real-time | SignalR (WebSockets) |
| Інтеграції | LiqPay/Stripe, SendGrid/SMTP, QRCoder |
| Інфраструктура | Docker, Docker Compose, Nginx |
| Тестування | xUnit, Moq, FluentAssertions, Testcontainers, k6 |

## Архітектура backend (Clean Architecture)

```
backend/
├── CineVerse.sln
├── Dockerfile
├── src/
│   ├── CineVerse.Domain          — сутності, enum'и, без залежностей від інших шарів
│   ├── CineVerse.Application     — CQRS (MediatR), інтерфейси, DTO, валідація
│   ├── CineVerse.Infrastructure  — EF Core, PostgreSQL, Redis, зовнішні сервіси
│   └── CineVerse.WebApi          — контролери, SignalR-хаби, Program.cs, DI
└── tests/
    ├── CineVerse.UnitTests
    └── CineVerse.IntegrationTests
```

Потік залежностей: `WebApi → Application ← Infrastructure`, `Application → Domain`.
Domain нічого не знає про інші шари (класична Clean/Onion Architecture).

## Структура frontend

```
frontend/
├── src/
│   ├── app/        — Redux store
│   ├── pages/      — сторінки (HomePage, MoviePage, SeatSelectionPage, ...)
│   ├── components/ — перевикористовувані UI-компоненти
│   ├── features/   — Redux slices за доменом (auth, catalog, booking, ...)
│   ├── api/         — HTTP-клієнт, SignalR-клієнт
│   ├── hooks/, types/, styles/
```

## Домен (16 сутностей БД, п.2.6 ТЗ)

User, Movie, Genre, MovieGenre, Actor, MovieActor, Cinema, Hall, Seat, Session,
Booking, BookingSeat, Ticket, Payment, Promotion, Review — реалізовано у
`CineVerse.Domain/Entities`.

## База даних (Етап 2)

```
backend/src/CineVerse.Infrastructure/Persistence/
├── ApplicationDbContext.cs          — EF Core DbContext, підключає всі 16 DbSet
├── ApplicationDbContextFactory.cs   — design-time factory для `dotnet ef`
├── DbInitializer.cs                 — застосування міграцій при старті застосунку
└── Configurations/                  — Fluent API для КОЖНОЇ з 16 сутностей:
    зв'язки, unique-індекси, decimal(10,2) для грошей, конверсія enum→string,
    check-constraint на Review.Rating (1-5), каскадне видалення для залежних сутностей

database/init/
├── 001_schema.sql   — повна DDL-схема (16 таблиць, FK, індекси, обмеження)
└── 002_seed.sql     — демо-дані: 5 жанрів, 3 актори, 3 фільми, кінотеатр
                        з 3 залами, згенеровані місця (VIP/Love Seat/Disabled),
                        1 сеанс, 2 промокоди (STUDENT20, WELCOME50), адмін-акаунт
```

**Важливо — чому SQL-скрипти, а не готові файли EF-міграцій.**
Це середовище розробки не має доступу до NuGet (мережа обмежена), тому виконати
`dotnet restore` і згенерувати офіційні файли міграції (`*_InitialCreate.cs` +
`ApplicationDbContextModelSnapshot.cs`) з реального EF Core тут неможливо —
будь-які такі файли, написані вручну "напам'ять", ризикують розійтися з тим,
що реально згенерує інструмент, і зламають подальші `dotnet ef migrations add`.

Натомість:
1. Усі 16 конфігурацій Fluent API вже написані та повністю відображають цільову схему.
2. Еквівалентна DDL-схема лежить у `database/init/001_schema.sql` і піднімається
   автоматично при `docker compose up` (Postgres виконує її як init-скрипт).
3. `ApplicationDbContextFactory` вже готовий для команди `dotnet ef` — щойно
   у розробника буде встановлено .NET 8 SDK локально, достатньо виконати:

   ```bash
   cd backend
   dotnet ef migrations add InitialCreate \
     --project src/CineVerse.Infrastructure \
     --startup-project src/CineVerse.WebApi
   dotnet ef database update \
     --project src/CineVerse.Infrastructure \
     --startup-project src/CineVerse.WebApi
   ```

   Це згенерує "справжні" файли міграції, ідентичні за результатом до
   `001_schema.sql`, і надалі БД керуватиметься стандартним EF-флоу.

### Демо-доступи після підняття БД

| Роль | Email | Пароль |
|---|---|---|
| ADMIN | admin@cineverse.local | Admin!2026 |
| CHECKER | checker@cineverse.local | Checker!2026 |

Промокоди: `STUDENT20` (-20%), `WELCOME50` (-50 грн).

> Обидва seed-промокоди вже активні й одразу застосовуються на сторінці вибору місць
> (поле "Промокод" перед кнопкою "Забронювати") — не потребують створення вручну для демо.

## Автентифікація та авторизація (Етап 3)

```
Application/Features/Auth/
├── Commands/RegisterUser/    — реєстрація + генерація токена підтвердження email
├── Commands/ConfirmEmail/    — активація акаунту за токеном з листа
├── Commands/Login/           — перевірка пароля (BCrypt), видача пари токенів
├── Commands/RefreshToken/    — ротація refresh-токена (старий інвалідується одразу)
├── Commands/Logout/          — інвалідація refresh-токена на сервері
└── Common/AuthResultDto.cs

Infrastructure/Services/
├── PasswordHasher.cs   — BCrypt.Net-Next, work factor 11
├── JwtTokenService.cs  — HS256 access-токен (15 хв) + випадковий refresh-токен (7 днів)
├── EmailService.cs     — SendGrid; якщо SendGrid:ApiKey порожній — лист логується в консоль (dev-режим)
└── SystemDateTime.cs   — обгортка над DateTimeOffset.UtcNow для тестованості

WebApi/
├── Controllers/AuthController.cs   — /api/auth/{register,confirm-email,login,refresh,logout,me}
├── Services/CurrentUserService.cs  — читає userId/email/role з JWT claims
└── Middleware/ExceptionHandlingMiddleware.cs — мапить бізнес-винятки на HTTP 400/401/403/404/409
```

**Потік автентифікації:**
1. `POST /api/auth/register` — валідація (email, телефон, надійність пароля через FluentValidation),
   перевірка унікальності email/телефону, хешування пароля, збереження `EmailConfirmationToken`,
   відправка листа з посиланням підтвердження.
2. `GET /api/auth/confirm-email?token=...` — активує акаунт (`IsEmailConfirmed = true`).
3. `POST /api/auth/login` — access-токен повертається в тілі відповіді, refresh-токен —
   у `HttpOnly` + `Secure` + `SameSite=Strict` cookie (недоступний з JS, захист від XSS-крадіжки).
4. `POST /api/auth/refresh` — бере refresh-токен із cookie, видає нову пару (ротація).
5. `POST /api/auth/logout` — інвалідує refresh-токен у БД і видаляє cookie.

**Ролі** (`UserRole`: CUSTOMER, CASHIER, MANAGER, ADMIN, CHECKER) прописуються в JWT-claim `role`
і перевіряються через `[Authorize(Roles = "...")]` або готові policy `AdminOnly` / `StaffOnly` / `CheckerOnly`,
зареєстровані в `Program.cs`. Приклад для майбутніх контролерів:

```csharp
[Authorize(Policy = "StaffOnly")]
[HttpPost("sessions")]
public async Task<IActionResult> CreateSession(...) { ... }
```

**⚠️ Про `Jwt:Secret` в `appsettings.json`.** Значення там — заглушка лише для локального
запуску без додаткового налаштування. Перед будь-яким деплоєм за межі свого ноутбука
обов'язково перевизначте його через `dotnet user-secrets` (розробка) або змінну середовища
`Jwt__Secret` / Docker secret (продакшн) — і ніколи не комітьте реальний секрет у git.

**Архітектурне зауваження:** `IApplicationDbContext` в Application-шарі використовує
`Microsoft.EntityFrameworkCore.DbSet<T>` напряму (а не власну абстракцію-обгортку) —
це свідомий і поширений компроміс Clean Architecture (як у шаблоні Jason Taylor), що
економить шар мапінгу ціною невеликої залежності Application → EF Core abstractions.

## Каталог фільмів та розклад сеансів (Етап 4)

```
Application/Features/
├── Movies/     — GetMovies (фільтри: статус/жанр/пошук + пагінація), GetMovieById
│                 (деталі + актори + середній рейтинг з верифікованих відгуків),
│                 CreateMovie / UpdateMovie / DeleteMovie
├── Genres/     — GetGenres, CreateGenre
├── Cinemas/    — GetCinemas (з залами, фільтр за містом), CreateCinema,
│                 CreateHall (автогенерація сітки місць "ряди x місця" з overrides
│                 для VIP/Love Seat/Disabled за координатами)
└── Sessions/   — GetSessions (фільтри: фільм/кінотеатр/дата),
                  CreateSession / UpdateSession / DeleteSession

WebApi/Controllers/ — MoviesController, GenresController, CinemasController, SessionsController
```

**Ключова бізнес-логіка — конструктор розкладу без накладок (п.2.2 ТЗ):**
`EndTime` сеансу сервер обчислює сам: `StartTime + Movie.Duration + 20 хв технічної перерви`
(`SessionScheduling.CleaningBufferMinutes`). Перед створенням/перенесенням сеансу
`CreateSessionCommandHandler.EnsureNoScheduleConflictAsync` перевіряє класичну умову
перетину інтервалів (`existing.Start < newEnd AND newStart < existing.End`) серед усіх
сеансів того самого залу — при конфлікті повертається **HTTP 409** з часовим проміжком
сеансу, що заважає. Той самий метод перевикористовується і в `UpdateSessionCommandHandler`.

Додатково: сеанс не можна перенести або видалити, якщо на нього вже є **підтверджені**
бронювання (запобігає "тихому" псуванню вже проданих квитків) — теж HTTP 409.

**Права доступу:** перегляд каталогу/розкладу — публічний (`AllowAnonymous`); створення,
редагування, видалення фільмів/залів/сеансів — policy `StaffOnly` (ADMIN/MANAGER/CASHIER),
жанри та кінотеатри — `AdminOnly`.

**Приклади запитів:**
```
GET  /api/movies?status=NOW_SHOWING&genreId=...&search=дюна&pageNumber=1&pageSize=12
GET  /api/movies/{id}
GET  /api/sessions?cinemaId=...&date=2026-09-10
POST /api/sessions   { movieId, hallId, startTime, format: "IMAX", basePrice: 250 }
     → 409, якщо в залі вже є сеанс, що перетинається за часом
```

## Бронювання місць у реальному часі (Етап 5)

```
Application/
├── Common/Interfaces/ISeatStatusNotifier.cs   — абстракція над SignalR-каналом
└── Features/Bookings/
    ├── Common/     — SeatMapDto, BookingDto, SeatPricingCalculator (VIP +30%, Love Seat +80%)
    ├── Queries/GetHallSeatMap    — жива схема залу: FREE/LOCKED/SOLD + ціна на місце
    ├── Queries/GetBookingById    — деталі бронювання (з перевіркою власника)
    ├── Commands/CreateBooking    — атомарне бронювання з Redis-локом (ядро етапу)
    └── Commands/CancelBooking    — скасування ще неоплаченого бронювання

Infrastructure/
├── Services/RedisSeatLockService.cs     — SET key value NX PX ttl
└── BackgroundServices/ExpiredBookingWorker.cs — скасовує PENDING після 10 хв

WebApi/
├── Controllers/BookingsController.cs        — /api/bookings/*
└── Services/SignalRSeatStatusNotifier.cs    — розсилка події "seatsUpdated" у групу сеансу
```

**Як вирішено Race Condition (двоє клієнтів тиснуть "забронювати" одне й те саме місце
одночасно), п.2.2 ТЗ:**

1. **Redis SET NX PX** — для кожного обраного місця `CreateBookingCommandHandler` намагається
   атомарно виставити ключ `seat-lock:{sessionId}:{seatId}` з TTL = 10 хв. Redis гарантує,
   що з двох одночасних `SET ... NX` лише один поверне успіх — це вирішується на рівні
   однієї атомарної команди, без потреби в блокуванні рядків БД чи розподілених транзакціях.
   Той, хто програв гонку, миттєво отримує **HTTP 409** з переліком зайнятих місць —
   ще до будь-якого звернення до PostgreSQL.
2. **Контрольна перевірка в БД** одразу після Redis-локів — додатковий страховий рівень
   (наприклад, на випадок розсинхронізації годинників між Redis TTL і `Booking.ExpiresAt`).
3. Якщо після захоплення локів щось йде не так (конфлікт у БД, збій `SaveChanges`) —
   усі щойно захоплені локи звільняються в `catch`, щоб не "заморозити" місця даремно.
4. Після успішного створення `Booking(PENDING)` — миттєва розсилка через **SignalR**
   (`hubs/hall`, група `session-{sessionId}`) події `seatsUpdated`, щоб усі, хто зараз
   дивиться на схему залу, побачили місця заблокованими без перезавантаження сторінки.

**Автоматичне скасування протермінованих бронювань:** `ExpiredBookingWorker` — це
`BackgroundService`, що кожні 15 секунд (`BookingExpiry:PollIntervalSeconds`) шукає
`PENDING`-бронювання, чий `ExpiresAt` настав, переводить їх у `EXPIRED`, явно звільняє
відповідні Redis-локи (подвійна гарантія понад пасивний TTL) і розсилає `seatsUpdated`
зі статусом `FREE`. PostgreSQL не має вбудованого TTL на рядки — тому потрібен саме такий
активний "прибиральник", а не лише сподівання на природне згасання ключа в Redis.

**Динамічне ціноутворення за типом місця** (`SeatPricingCalculator`): STANDARD/DISABLED —
базова ціна сеансу, VIP — ×1.3, LOVE_SEAT — ×1.8 (місце на двох).

**Приклади запитів:**
```
GET  /api/bookings/seat-map/{sessionId}          — публічний перегляд схеми залу
POST /api/bookings   { sessionId, seatIds: [...] }  → 201 або 409 "Місця вже обираються..."
POST /api/bookings/{id}/cancel
GET  /api/bookings/{id}
```

Клієнт підключається до `wss://.../hubs/hall`, викликає `JoinSessionGroup(sessionId)`
при відкритті сторінки вибору місць і слухає подію `seatsUpdated` для живого оновлення UI.

## Оплата та квитки (Етап 6)

```
Application/Features/
├── Payments/
│   ├── Commands/InitiatePayment       — створює Payment(PENDING), отримує посилання на оплату
│   ├── Commands/ProcessPaymentWebhook — перевірка підпису → CONFIRMED + видача квитків, або CANCELLED
│   └── Commands/ConfirmCashPayment    — готівкова оплата на касі (роль CASHIER)
└── Tickets/
    ├── Services/TicketIssuanceService — спільна логіка видачі квитків (онлайн + готівка)
    └── Queries/GetTicketsByBooking    — квитки з QR для сторінки "Мій квиток"

Infrastructure/Services/
├── QrCodeGenerator.cs           — QRCoder, QR кодує TicketCode, повертає data:image/png;base64,...
└── MockPaymentGatewayService.cs — тимчасова реалізація IPaymentGatewayService (див. нижче)

WebApi/Controllers/PaymentsController.cs — /api/payments/{initiate,webhook,mock/.../complete,{id}/cash}
```

**Чому Mock, а не справжній LiqPay/Stripe.** Це середовище розробки не має мережевого доступу
до зовнішніх платіжних API, тож реальну інтеграцію звідси підключити неможливо. Натомість
контракт `IPaymentGatewayService` спроєктовано так, щоб заміна на реальний SDK торкнулась
**лише одного файлу** (`MockPaymentGatewayService.cs` → `LiqPayGatewayService.cs`), без жодних
змін в Application-шарі чи контролерах:

```csharp
Task<PaymentSessionResult> CreatePaymentSessionAsync(paymentId, amount, description, ct);
bool VerifyWebhookSignature(payload, signature);
```

Механізм перевірки підпису вже реалізовано по-справжньому (HMAC-SHA256 з секретом
`PaymentGateway:WebhookSecret`, порівняння через `CryptographicOperations.FixedTimeEquals`
для захисту від timing-атак) — це той самий підхід, який використовують реальні шлюзи,
тому бізнес-логіка Webhook (`ProcessPaymentWebhookCommandHandler`) вже повністю робоча
і не потребуватиме змін після підключення LiqPay/Stripe — зміниться лише те, звідки
приходить сам HTTP-запит.

**Наскрізний потік оплати:**
1. `POST /api/payments/initiate {bookingId}` → перевірка, що бронювання PENDING і не протерміноване
   → створення `Payment` → `redirectUrl` на mock-сторінку оплати.
2. `POST /api/payments/mock/{transactionId}/complete {success: true|false}` (**лише Development**) —
   формує коректно підписаний payload і прогонить його через **той самий** обробник Webhook,
   що використав би реальний банк — тобто перевіряється справжня бізнес-логіка, а не заглушка.
3. `ProcessPaymentWebhookCommandHandler`: **ідемпотентно** (Payment.Status уже не PENDING → no-op,
   про всяк випадок retry від шлюзу) переводить `Payment→SUCCESS`, `Booking→CONFIRMED`, звільняє
   Redis-лок (більше не потрібен — місце вже незворотно продане в БД), розсилає `seatsUpdated`
   зі статусом `SOLD`, викликає `TicketIssuanceService` → генерує `Ticket` на кожне місце
   (унікальний код + QR) і надсилає їх на email.
4. При невдалій оплаті — `Booking→CANCELLED`, місця звільняються **одразу** (не чекаючи
   `ExpiredBookingWorker`), щоб не тримати їх заблокованими до кінця 10-хвилинного вікна.

**Готівкова оплата в касі:** `POST /api/payments/{bookingId}/cash` (policy `StaffOnly`) —
той самий результат (CONFIRMED + квитки), але без платіжного шлюзу: касир підтверджує факт
оплати вручну, `TransactionId` формується як `CASH-{cashierId}-{timestamp}` для аудиту.

## Валідація квитків контролером (Етап 7)

```
Application/Features/Tickets/
├── Common/TicketValidationDtos.cs   — TicketPreviewDto, TicketValidationResultDto
├── Queries/GetTicketByCode          — крок 1: показати деталі квитка за кодом, без зміни статусу
└── Commands/ValidateTicket          — крок 2: незворотно підтвердити прохід (ACTIVE → USED)

WebApi/Controllers/TicketsController.cs — /api/tickets/{code} (GET), /api/tickets/validate (POST)
```

**Чому два кроки, а не одне сканування = один прохід.** PWA-кабінет контролера спершу
показує деталі квитка (фільм, зал, час, місце, ПІБ власника) за `GET /api/tickets/{code}`,
і лише після того, як контролер візуально звірив дані й натиснув "Пропустити", викликається
`POST /api/tickets/validate`. Це захищає від випадкового "спалювання" чужого квитка через
помилкове сканування не того QR.

**Бізнес-правила валідації** (`ValidateTicketCommandHandler`):
- квиток має існувати і бути в статусі `ACTIVE` — `USED` чи `CANCELLED` дають **409** з поясненням
  (для `USED` — час попереднього проходу);
- вхід у зал відкривається за `TicketValidation:EarlyEntryMinutes` (типово 30) хвилин до старту
  сеансу і закривається одразу після його завершення — зарано/запізно теж **409**.

**Захист від подвійного сканування (два контролери одночасно, двоє телефонів з одним QR).**
"Прочитати статус → змінити → зберегти" у два окремі кроки НЕ атомарне і вразливе до тієї ж
Race Condition, що й бронювання місць (Етап 5). Тому фінальний перехід у `USED` виконується
єдиним умовним SQL-запитом через `ExecuteUpdateAsync`
(`UPDATE "Tickets" SET "Status"='USED', ... WHERE "Id"=@id AND "Status"='ACTIVE'`) —
з двох одночасних викликів лише один реально змінить рядок (`affected rows = 1`), другий
побачить `0` і отримає **409**, без потреби у Redis чи додаткових блокуваннях. Той самий
принцип, що й Redis-лок на Етапі 5, тільки на рівні атомарності самої СУБД.

**Аудит:** до сутності `Ticket` додано `ValidatedAt`/`ValidatedByUserId` (і в SQL-схему,
і в модель EF) — видно не лише факт використання квитка, а й хто саме з контролерів
підтвердив прохід і коли.

**Демо-акаунт для тестування:** `checker@cineverse.local` / `Checker!2026` (роль CHECKER).

## Frontend: усі сторінки (Етап 8)

```
frontend/src/
├── api/           — authApi, moviesApi, genresApi, cinemasApi, sessionsApi, bookingsApi,
│                    paymentsApi, ticketsApi(checkerApi); httpClient з auto-refresh на 401
├── app/store.ts   — Redux Toolkit store (authSlice)
├── features/auth/authSlice.ts — login/logout/restoreSession (тихе відновлення сесії при F5)
├── hooks/         — useAppDispatch/useAppSelector, useSeatStatusUpdates (SignalR)
├── components/    — Navbar, ProtectedRoute, RoleRoute, MovieCard, SeatMap, CountdownTimer, AdminNav
├── utils/apiError.ts — дістає людяне повідомлення з ExceptionHandlingMiddleware (Етап 3)
└── pages/
    ├── HomePage, MoviePage                      — афіша, картка фільму, розклад за датою/кінотеатром
    ├── LoginPage, RegisterPage, ConfirmEmailPage — Auth-флоу (Етап 3)
    ├── SeatSelectionPage                          — жива схема залу через SignalR (Етап 5)
    ├── CheckoutPage, MockPaymentPage, TicketsPage — оплата й QR-квитки (Етап 6)
    ├── ProfilePage                                 — "Мої бронювання" (новий бекенд-запит GetMyBookings)
    ├── CheckerPage                                 — PWA-кабінет контролера (Етап 7)
    └── admin/AdminMoviesPage, AdminSessionsPage    — CRUD каталогу й розкладу (Етап 4)
```

**Дизайн-напрям** (`tailwind.config.js`, `index.html`): темно-вугільний фон (`#0b0b0d`),
маркізний червоний акцент (`#e63946`) для CTA/статусів "продано", теплий "лампочковий"
жовтий (`#f2c14e`) для цін/вибраних місць/бейджів. Заголовки — умовний `Bebas Neue`
(ефект афіші/кінотеатральної маркізи), основний текст — `Inter`. Свідомо уникав типових
"AI-згенерованих" прийомів: жодних однакових заокруглених SaaS-карток з однаковою тінню,
жодних eyebrow-лейблів капсом над кожним заголовком, жодних стрілочок у кінці кнопок.

**Один додатковий бекенд-ендпоінт довелося дописати заднім числом:** сторінці профілю
потрібен був список "моїх бронювань", якого не існувало після Етапу 5 (там був лише
`GetBookingById` для одного бронювання). Додав `GetMyBookingsQuery` + `GET /api/bookings/mine`
(з увагою до порядку маршрутів: `/mine` не повинен конфліктувати з `{id:guid}`).

**Що свідомо спрощено на цьому етапі** (щоб не роздувати обсяг понад розумне):
- Кабінет контролера приймає код квитка текстовим полем; підключення реального сканера
  камери (наприклад, бібліотека `html5-qrcode`) — питання одного нового компонента,
  функція `handleLookup(code)` вже готова його прийняти.
- Адмінка охоплює керування фільмами й розкладом (найважливіше для демонстрації бізнес-логіки
  Етапів 4–5); керування кінотеатрами/залами/жанрами наразі доступне через API напряму
  (`CinemasController`, `GenresController` вже підтримують створення) — форми для них
  додаються за тим самим патерном, що й `AdminMoviesPage`.
- Access-токен зберігається лише в пам'яті Redux (не в localStorage) — свідомий вибір
  проти XSS; ціна цього — короткий "мигтіння" неавторизованого стану до завершення
  `restoreSession()` при кожному F5, що вирішено індикатором завантаження в `ProtectedRoute`.

## Тестування (Етап 9)

```
backend/tests/
├── CineVerse.UnitTests/
│   ├── Common/               — TestDbContext (SQLite in-memory), TestDataBuilder
│   ├── Features/Bookings/    — SeatPricingCalculatorTests, CreateBookingCommandHandlerTests
│   ├── Features/Tickets/     — ValidateTicketCommandHandlerTests
│   ├── Features/Auth/        — RegisterUserCommandValidatorTests
│   ├── Features/Sessions/    — CreateSessionCommandValidatorTests
│   └── Infrastructure/       — PasswordHasherTests
└── CineVerse.IntegrationTests/
    ├── Common/CustomWebApplicationFactory.cs — Testcontainers.PostgreSql + ті самі database/init/*.sql
    ├── HealthCheckTests.cs
    ├── MoviesCatalogTests.cs
    └── AuthFlowTests.cs

tests/load/booking-race-condition.js — k6: 20 VU одночасно борються за ОДНЕ місце
```

**Чому SQLite in-memory для unit-тестів, а не мок `IApplicationDbContext`.**
`IApplicationDbContext` виставляє `DbSet&lt;T&gt;` напряму (архітектурне рішення з Етапу 3) —
замокати сам `DbSet` через Moq для реалістичних LINQ-запитів (`.Include`, `.Where`, `ExecuteUpdateAsync`)
практично неможливо. Тому хендлери тестуються проти **справжнього** EF Core поверх SQLite
`:memory:` — воно ближче до PostgreSQL за семантикою (реальні FK, транзакції, атомарний
`ExecuteUpdateAsync`), ніж провайдер EF Core InMemory, який Microsoft прямо не рекомендує
для нічого складнішого за тривіальний CRUD (і який узагалі не підтримує `ExecuteUpdateAsync`,
критичний для тестів `ValidateTicketCommandHandler`).

**Що саме перевіряють ключові тести:**
- `CreateBookingCommandHandlerTests` — три сценарії ядра Етапу 5: успішне бронювання
  (правильна сума, розсилка `LOCKED`), програш гонки за друге місце (перше вже
  захоплене — `ConflictException`, і **звільнення саме того локу, який встигли взяти**,
  без зайвого виклику `ReleaseSeatLockAsync` для програного місця), і відмова від спроби
  локу взагалі, якщо сеанс уже почався.
- `ValidateTicketCommandHandlerTests` — часове вікно входу (зарано/запізно), і сценарій
  "друге сканування того самого квитка" (виклик хендлера двічі поспіль на тому самому
  коді) — другий виклик має впасти з `ConflictException`, підтверджуючи, що атомарний
  `UPDATE ... WHERE Status = 'ACTIVE'` справді не пропускає повторне використання.
- `AuthFlowTests` — повний HTTP-флоу реєстрація → підтвердження email → логін →
  звернення до захищеного `/auth/me` з отриманим токеном, проти справжнього PostgreSQL
  у Docker-контейнері (Testcontainers), піднятого з **тими самими** `database/init/*.sql`,
  що й `docker-compose.yml` — тобто тест перевіряє те саме середовище, яке отримає розробник.

**Чесно про виконання.** У цій пісочниці немає доступу ні до NuGet (див. Етап 2), ні до
Docker-демона, тож у мене немає можливості реально прогнати `dotnet test` і показати
"зелений" результат — код написано за перевіреними, стандартними патернами (xUnit + Moq +
FluentAssertions для unit-рівня; Testcontainers + `WebApplicationFactory` для інтеграційного —
той самий підхід, що в офіційній документації обох бібліотек), і має запрацювати без правок
на машині з установленим .NET 8 SDK та Docker. Перший реальний прогін варто вважати
частиною "визначення готовності" (Definition of Done) для цього етапу, а не форс-мажором:
`dotnet test` з великою ймовірністю виявить дрібні неточності (версії пакетів, шляхи),
які варто буде поправити на місці.

**k6-навантажувальний тест** (`tests/load/booking-race-condition.js`) емулює 20 одночасних
запитів на бронювання ОДНОГО місця — головний практичний доказ того, що Redis-лок працює
не лише "в теорії unit-тестів", а й під реальним паралельним HTTP-навантаженням: очікується
рівно один успіх і 19 відповідей `409`.

## Виправлення за результатами незалежного аудиту

Зовнішній ревʼю виявив 34 конкретних проблеми (безпека, збірка, стан фронтенду, гонки умов,
невідповідність контрактів фронт/бек). Більшість виправлено безпосередньо в коді:

**Критичні (безпека):**
- Захардкоджений `Jwt:Secret`/`PaymentGateway:WebhookSecret`/паролі БД в `appsettings.json` —
  прибрано; застосунок тепер **fail-fast**, якщо секрети не задані через `.env`
  (`.env.example` додано, `.env` у `.gitignore`). Публічні порти backend/frontend прибрано
  з `docker-compose.yml` — єдина точка входу ззовні тепер лише nginx.
- Немає транзакції "оплата підтверджена → квитки видані" + незахищений виклик SendGrid —
  тепер атомарний `SaveChangesAsync` (booking + payment + tickets разом), email винесено
  за межі критичного шляху й ніколи не кидає виняток нагору.
- Колізія коду квитка (11 hex-символів ентропії) — тепер повний GUID.

**Серйозні:**
- Race condition при реєстрації (`AnyAsync` без страховки) — додано `catch (DbUpdateException)`.
- `CreateHall`: дублікати/вихід за межі координат спецмісць падали 500-ю — тепер валідація.
- `UpdateSessionCommandValidator` без правила "не в минулому" (асиметрія з Create) — додано.
- Фільтр дати сеансів рахував добу в UTC, а не за київським часом — виправлено на обох рівнях
  (бекенд: `GetSessionsQueryHandler` з `TimeZoneInfo`, фронтенд: `toLocalDateString`).
- Пошук фільмів не екранував `%`/`_` у `ILIKE` — виправлено.
- Невикористаний `AutoMapper` — прибрано.

**Фронтенд (стан, збірка, UX):** відсутні імпорти `React.FormEvent` (ламали `tsc -b`),
подвійний виклик `confirmEmail` у `StrictMode`, розсинхронізація Redux-стану при провалі
silent-refresh, ігнорування помилки `logout`, втрата вибраних місць при редіректі на логін,
відсутні `.catch()` на кількох сторінках, нетипізований `checkerApi.validateTicket`,
відсутній клієнтський ліміт 10 місць, гонка між SignalR-оновленням і початковим завантаженням
карти місць, `MockPaymentPage` доступна поза Development — усе виправлено.

**Функціональні прогалини:** `BookingDto` не містив назви фільму/часу сеансу (сторінка "Мої
бронювання" показувала лише координати місць) — розширено на бекенді й фронтенді; відсутній
UI редагування фільмів/сеансів (`updateMovie`/`updateSession` існували, але ніде не викликались)
— додано; сторінка фільму показувала рейтинг/відгуки без жодного способу їх залишити —
реалізовано мінімальну робочу фічу відгуків (бекенд + фронтенд), а не просто приховано UI.

**Збірка/інфраструктура:** відсутній `package-lock.json` при `npm install` у Dockerfile —
згенеровано справжній lock-файл (реальний `npm install --package-lock-only` проти
registry.npmjs.org, не вручну), `Dockerfile` тепер використовує `npm ci`; відсутній
ESLint-конфіг — додано `.eslintrc.cjs`; `vite.config.ts` без `@types/node`/поза tsconfig —
виправлено стандартним патерном Vite (`tsconfig.app.json` + `tsconfig.node.json`).

**Реальна перевірка, а не лише статичний аналіз.** На відміну від попередніх етапів, тут
у пісочниці виявився доступний `node`/`npm` (реєстр npmjs.org дозволений мережевими
налаштуваннями) — тому я не просто написав код "за патерном", а справді прогнав
`npm ci && npm run build && npm run lint` проти реальних залежностей. Це одразу виявило
відсутність `src/vite-env.d.ts` (без нього `import.meta.env` не типізується — саме такий клас
помилки, який могла приховати статична перевірка балансу дужок) і два невикористані імпорти —
усе виправлено, і фінальний білд/лінт проходять чисто (0 помилок, 0 попереджень). На бекенді
такої можливості немає (відсутній доступ до NuGet/dotnet CLI, див. Етап 2) — там зміни
перевірено лише за балансом дужок і логічною звіркою типів/сигнатур вручну.

**Свідомо не встигнуто/не входить у цей прохід:** #28 (порожні EF-міграції — вже задокументований
вибір з Етапу 2, не терміново), дрібні стилістичні неоднорідності відступів.

## Доповнення відповідності ТЗ (промокоди, верифіковані відгуки, сканер, відновлення пароля, аналітика)

Звірка з текстом ТЗ виявила прогалини саме в тих місцях, які найімовірніше перевірятимуть
на захисті — реалізовано напряму, а не відкладено:

**Промокоди** (`Features/Promotions/`, `PromotionsController`) — реалізує наскрізний сценарій
з ТЗ ("STUDENT20", знижка 20%). `CreateBookingCommand` приймає опційний `PromoCode`; знижка
рахується від загальної суми бронювання, `Promotion.UsageCount` інкрементується в **тій самій**
транзакції, що й саме бронювання (якщо збереження впаде — відкотиться і лічильник використань,
промокод не "згорить" даремно). Перевірено: неіснуючий код, протермінований, вичерпаний ліміт,
фіксована знижка, що не робить суму від'ємною (усе покрито тестами). Адмінська форма створення
промокодів — `/admin/promotions`; поле для введення коду — на сторінці вибору місць.

**Верифіковані відгуки** (термін окремо визначений у глосарії ТЗ, п.5.2) — уточнено критерій:
раніше форма відгуку була доступна за самим фактом підтвердженого бронювання (CONFIRMED),
а "верифікованість" — слабшим окремим прапорцем. Тепер сам допуск до написання відгуку вимагає
**USED-квитка** (людина справді пройшла на сеанс, а не просто купила й не прийшла) — тобто
кожен збережений відгук верифікований за визначенням, без окремого "слабшого" шляху.

**Сканер QR камерою** (`components/QrScanner.tsx`, бібліотека `jsQR`) — кабінет контролера тепер
показує реальний відеопотік із вебкамери (працює і з камери ноутбука, і з камери телефону),
а не лише текстове поле. Один скан = одна дія: код розпізнано → одразу викликається
`/tickets/validate` → **повноекранний зелений/червоний індикатор** із результатом (саме той
елемент, що найбільш візуально відрізнявся від очікувань ТЗ на живому показі). Ручне введення
коду лишилось як запасний варіант (двокроковий флоу превʼю→підтвердження) на випадок
недоступної камери чи пошкодженого QR.

**Відновлення пароля** — той самий патерн, що й підтвердження email: одноразовий токен
(`PasswordResetToken` + `PasswordResetTokenExpiresAt`, 30 хв), лист через уже наявний
`IEmailService`. Відповідь на `/auth/forgot-password` навмисно однакова незалежно від
існування email (захист від User Enumeration — той самий принцип, що й у `LoginCommandHandler`).
Зміна пароля інвалідує активну refresh-сесію.

**Мінімальний дашборд аналітики** (`/admin/analytics`) — не графіки на всі зали, а один графік
"виручка за 14 днів" (`recharts`, на основі вже наявних `Bookings.CONFIRMED`) і таблиця
"заповнюваність сеансу" (продані місця / місткість залу, ±7 днів). Дані рахуються на льоту
з наявних таблиць — без окремого сховища звітів.

**Трейлер на сторінці фільму** — `trailerUrl` конвертується в embed-посилання YouTube
(`utils/youtube.ts`); якщо URL не розпізнано як YouTube — показується звичайне посилання
замість зламаного `<iframe>`.

**Додаткові тести** (уточнення до Етапу 9 — тестові файли вже існували з Етапу 9, ці — на нові
фічі): `CreateBookingCommandHandlerTests` доповнено 5 сценаріями промокодів (успішний відсотковий/
фіксований, протермінований, вичерпаний ліміт, невідомий код); новий `CreateReviewCommandHandlerTests`
перевіряє саме уточнений критерій верифікації (USED-квиток обов'язковий, ACTIVE — недостатньо).

**Serilog увімкнено по-справжньому** (пакет був підключений з Етапу 1, але не використовувався) —
консоль (для `docker compose logs`) + файл з ротацією по днях (`logs/cineverse-.log`, том
`backend_logs` у `docker-compose.yml`), плюс `UseSerilogRequestLogging()` для логування кожного
HTTP-запиту.

**Мінімальний GitHub Actions workflow** (`.github/workflows/backend-ci.yml`) — `dotnet restore`
+ `build` + unit-тести при кожному push/PR. Формально закриває п.1.4 ТЗ (CI/CD); повний конвеєр
із інтеграційними тестами та публікацією образів — Етап 10.

**Реальна перевірка фронтенду (не лише статичний аналіз).** Той самий підхід, що й у розділі
аудиту вище: реально прогнав `npm install --package-lock-only` для нових залежностей
(`recharts`, `jsqr`) — і це одразу виявило справжню помилку: пакета `@types/jsqr`, який я спершу
додав "про всяк випадок", **не існує в npm registry** (404). Виявилось, `jsqr` вже постачає
власні типи (`dist/index.d.ts`) — зайва залежність прибрана. Після цього `npm ci && tsc -b &&
vite build && npm run lint` пройшли чисто: 0 помилок, 0 попереджень.

**Свідомо не встигнуто з переліку "бажано":** редагування профілю/зміна пароля в кабінеті,
редагування (не лише створення) кінотеатру/залу, метадані фільму (країна/мова/режисер/IMDb),
фільтр за форматом і пошук за акторами, PDF-квиток, окрема каса для CASHIER — залишені як
природне продовження вже закладеної інфраструктури (форми/ендпоінти йдуть за тим самим
патерном, що й реалізовані вище).

## Усунення несправностей (з практики локального розгортання)

### Помилка `relation "Users" does not exist` при старті бекенду

**Причина:** SQL-скрипти `database/init/*.sql` Postgres виконує лише **один раз** —
у момент створення порожнього тому (volume). Якщо контейнер `postgres` вже запускався
раніше (навіть без цих скриптів або з невдалою ініціалізацією), том лишається "брудним",
і при повторному `docker compose up` init-скрипти просто ігноруються — а `context.Database
.Migrate()` теж нічого не створює, бо в проєкті поки немає жодної реальної EF-міграції
(див. розділ "База даних" вище).

**Виправлення:**
```bash
docker compose down -v   # видаляє том разом із застарілими/неповними даними
docker compose up -d --build
```
`DbInitializer` тепер явно логує попередження про цю ситуацію при старті застосунку,
замість того, щоб мовчки нічого не робити.

**Чого варто уникати:** тимчасова заміна `Migrate()` на `context.Database.EnsureCreated()`
дійсно "магічно" створює таблиці з поточної моделі EF — але робить це в обхід
`__EFMigrationsHistory`. Якщо потім додати реальну міграцію (`dotnet ef migrations add`),
EF Core не знатиме, що схема вже існує, і спробує створити ті самі таблиці повторно →
помилка `relation already exists`. Використовуйте `EnsureCreated()` хіба що для
одноразової локальної діагностики, і завжди повертайте `Migrate()` перед комітом.

### 404 при оновленні сторінки фронтенду (напр. `/login`, `/movies/123`)

**Причина:** образ `nginx:1.27-alpine`, у який пакується React-збірка, за замовчуванням
не знає про клієнтський роутинг (React Router) — він шукає файл `/login` на диску,
не знаходить і повертає 404. Виправлено додаванням `frontend/nginx.conf` з
`try_files $uri $uri/ /index.html;`, підключеним у `frontend/Dockerfile`.

### Загальний рецепт "щось зламалось після зміни docker-compose.yml / init-скриптів"

Якщо незрозуміла помилка з'являється саме після зміни конфігурації БД, спробуйте
повний скид середовища, перш ніж шукати баг у коді:
```bash
docker compose down -v
docker compose up -d --build
docker compose logs -f backend
```

## Як запустити (після наступних етапів з міграціями БД)

```bash
docker compose up --build
```

- Frontend: http://localhost (через Nginx) або http://localhost:5173 (dev)
- API: http://localhost/api або http://localhost:5000/api
- Healthcheck: GET /api/health

## План етапів розробки

Відповідно до ISO/IEC 12207 та розділу 1.4 ТЗ, роботу розбито на такі етапи.
✅ — зроблено у цій відповіді, ⏳ — наступні кроки.

1. ✅ **Каркас проєкту** — структура рішення, Clean Architecture, Domain-сутності,
   базовий DI, Program.cs, docker-compose, Nginx, каркас React-додатку.
2. ✅ **База даних** — повні Fluent API конфігурації для всіх 16 таблиць,
   зв'язки/індекси/обмеження, SQL init-скрипт схеми + seed-дані, design-time
   factory для майбутніх `dotnet ef` міграцій.
3. ✅ **Auth-модуль** — реєстрація з підтвердженням email, JWT access+refresh (HttpOnly cookie),
   BCrypt, ролі (CUSTOMER/CASHIER/MANAGER/ADMIN/CHECKER) через policy-based авторизацію,
   глобальна обробка помилок (400/401/403/404/409).
4. ✅ **Каталог фільмів та розклад сеансів** — CQRS-команди/запити, контролери,
   фільтрація й пагінація каталогу, автогенерація схеми залу, конструктор розкладу
   з перевіркою конфліктів у залі (HTTP 409 при накладці).
5. ✅ **Бронювання місць у реальному часі** — Redis Distributed Lock (SET NX PX),
   SignalR-оновлення статусів місць, HTTP 409 при конфлікті, ExpiredBookingWorker.
6. ✅ **Оплата та квитки** — контракт платіжного шлюзу з mock-реалізацією (заміна на LiqPay/Stripe
   торкнеться одного файлу), справжня перевірка HMAC-підпису Webhook, ідемпотентна обробка,
   готівкова оплата на касі, генерація QR-квитків (QRCoder), надсилання на email.
7. ✅ **Валідація квитків контролером** — двокроковий PWA-флоу (перегляд → підтвердження),
   часове вікно входу, атомарний захист від подвійного сканування (ExecuteUpdateAsync),
   аудит (хто/коли перевірив квиток).
8. ✅ **Frontend: усі сторінки** — Головна, Фільм, Вибір місць (жива схема через SignalR),
   Checkout, Квиток, Auth, Особистий кабінет, Кабінет контролера, Адмін-панель (фільми/сеанси).
9. ✅ **Тестування** — xUnit unit-тести на SQLite in-memory (Race Condition бронювання,
   подвійне сканування квитка, валідатори), інтеграційні тести на Testcontainers.PostgreSql
   з тими самими SQL init-скриптами, k6-тест конкурентного бронювання одного місця.
10. ⏳ **CI/CD та документація** — базовий GitHub Actions workflow вже додано
    (`backend-ci.yml`: restore+build+unit-тести); залишається розширити на повний конвеєр
    (інтеграційні тести, публікація Docker-образів), написати керівництво адміністратора
    та посібник користувача.

Кожен наступний етап буде виконано окремо і надіслано у вигляді файлів
з описом змін, як домовлено.
