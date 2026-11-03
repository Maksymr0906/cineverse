-- ============================================================================
-- CineVerse — початкова схема БД (PostgreSQL 16)
-- Відповідає розділу 2.6 ТЗ: 16 взаємопов'язаних таблиць.
-- Цей скрипт автоматично виконується Postgres-контейнером при першому старті
-- (змонтований у /docker-entrypoint-initdb.d, див. docker-compose.yml).
-- ============================================================================

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- ---------- Довідники / основні сутності ----------

CREATE TABLE "Users" (
    "Id"                        UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "FirstName"                 VARCHAR(50) NOT NULL,
    "LastName"                  VARCHAR(50) NOT NULL,
    "Email"                     VARCHAR(100) NOT NULL,
    "PasswordHash"              TEXT NOT NULL,
    "Phone"                     VARCHAR(20) NOT NULL,
    "Role"                      VARCHAR(20) NOT NULL DEFAULT 'CUSTOMER',
    "IsEmailConfirmed"          BOOLEAN NOT NULL DEFAULT FALSE,
    "EmailConfirmationToken"    TEXT NULL,
    "RefreshToken"              TEXT NULL,
    "RefreshTokenExpiresAt"     TIMESTAMPTZ NULL,
    "PasswordResetToken"        TEXT NULL,
    "PasswordResetTokenExpiresAt" TIMESTAMPTZ NULL,
    "CreatedAt"                 TIMESTAMPTZ NOT NULL DEFAULT now(),
    CONSTRAINT "UQ_Users_Email" UNIQUE ("Email"),
    CONSTRAINT "UQ_Users_Phone" UNIQUE ("Phone")
);
CREATE INDEX "IX_Users_PasswordResetToken" ON "Users" ("PasswordResetToken");

CREATE TABLE "Genres" (
    "Id"          UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "Name"        VARCHAR(50) NOT NULL,
    "CreatedAt"   TIMESTAMPTZ NOT NULL DEFAULT now(),
    CONSTRAINT "UQ_Genres_Name" UNIQUE ("Name")
);

CREATE TABLE "Actors" (
    "Id"          UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "Name"        VARCHAR(100) NOT NULL,
    "PhotoUrl"    TEXT NULL,
    "CreatedAt"   TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE "Movies" (
    "Id"            UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "Title"         VARCHAR(150) NOT NULL,
    "Description"   TEXT NOT NULL DEFAULT '',
    "Duration"      INT NOT NULL,
    "ReleaseDate"   DATE NOT NULL,
    "AgeRating"     VARCHAR(10) NOT NULL DEFAULT '',
    "PosterUrl"     TEXT NOT NULL DEFAULT '',
    "TrailerUrl"    TEXT NOT NULL DEFAULT '',
    "Status"        VARCHAR(20) NOT NULL DEFAULT 'COMING_SOON',
    "CreatedAt"     TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE INDEX "IX_Movies_Status" ON "Movies" ("Status");
CREATE INDEX "IX_Movies_Title" ON "Movies" ("Title");

CREATE TABLE "MovieGenres" (
    "MovieId"  UUID NOT NULL REFERENCES "Movies"("Id") ON DELETE CASCADE,
    "GenreId"  UUID NOT NULL REFERENCES "Genres"("Id") ON DELETE CASCADE,
    PRIMARY KEY ("MovieId", "GenreId")
);

CREATE TABLE "MovieActors" (
    "MovieId"        UUID NOT NULL REFERENCES "Movies"("Id") ON DELETE CASCADE,
    "ActorId"        UUID NOT NULL REFERENCES "Actors"("Id") ON DELETE CASCADE,
    "CharacterName"  VARCHAR(100) NOT NULL DEFAULT '',
    PRIMARY KEY ("MovieId", "ActorId")
);

CREATE TABLE "Cinemas" (
    "Id"        UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "Name"      VARCHAR(100) NOT NULL,
    "Address"   TEXT NOT NULL DEFAULT '',
    "City"      VARCHAR(50) NOT NULL,
    "Phone"     VARCHAR(20) NOT NULL DEFAULT '',
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE INDEX "IX_Cinemas_City" ON "Cinemas" ("City");

CREATE TABLE "Halls" (
    "Id"        UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "CinemaId"  UUID NOT NULL REFERENCES "Cinemas"("Id") ON DELETE CASCADE,
    "Name"      VARCHAR(50) NOT NULL,
    "Type"      VARCHAR(20) NOT NULL DEFAULT 'STANDARD',
    "Capacity"  INT NOT NULL,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE INDEX "IX_Halls_CinemaId" ON "Halls" ("CinemaId");

CREATE TABLE "Seats" (
    "Id"          UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "HallId"      UUID NOT NULL REFERENCES "Halls"("Id") ON DELETE CASCADE,
    "RowNumber"   INT NOT NULL,
    "SeatNumber"  INT NOT NULL,
    "Type"        VARCHAR(20) NOT NULL DEFAULT 'STANDARD',
    "CreatedAt"   TIMESTAMPTZ NOT NULL DEFAULT now(),
    CONSTRAINT "UQ_Seats_Hall_Row_Seat" UNIQUE ("HallId", "RowNumber", "SeatNumber")
);

CREATE TABLE "Sessions" (
    "Id"         UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "MovieId"    UUID NOT NULL REFERENCES "Movies"("Id") ON DELETE RESTRICT,
    "HallId"     UUID NOT NULL REFERENCES "Halls"("Id") ON DELETE RESTRICT,
    "StartTime"  TIMESTAMPTZ NOT NULL,
    "EndTime"    TIMESTAMPTZ NOT NULL,
    "Format"     VARCHAR(20) NOT NULL DEFAULT '2D',
    "BasePrice"  DECIMAL(10,2) NOT NULL,
    "CreatedAt"  TIMESTAMPTZ NOT NULL DEFAULT now()
);
-- Індекс для швидкої перевірки конфлікту розкладу в межах залу (п.2.2 ТЗ)
CREATE INDEX "IX_Sessions_Hall_Time" ON "Sessions" ("HallId", "StartTime", "EndTime");
CREATE INDEX "IX_Sessions_MovieId" ON "Sessions" ("MovieId");

CREATE TABLE "Promotions" (
    "Id"             UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "Code"           VARCHAR(30) NOT NULL,
    "DiscountType"   VARCHAR(20) NOT NULL DEFAULT 'PERCENT',
    "DiscountValue"  DECIMAL(10,2) NOT NULL,
    "ExpiresAt"      TIMESTAMPTZ NOT NULL,
    "UsageLimit"     INT NULL,
    "UsageCount"     INT NOT NULL DEFAULT 0,
    "CreatedAt"      TIMESTAMPTZ NOT NULL DEFAULT now(),
    CONSTRAINT "UQ_Promotions_Code" UNIQUE ("Code")
);

CREATE TABLE "Bookings" (
    "Id"          UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "UserId"      UUID NOT NULL REFERENCES "Users"("Id") ON DELETE RESTRICT,
    "SessionId"   UUID NOT NULL REFERENCES "Sessions"("Id") ON DELETE RESTRICT,
    "Status"      VARCHAR(20) NOT NULL DEFAULT 'PENDING',
    "TotalPrice"  DECIMAL(10,2) NOT NULL,
    "ExpiresAt"   TIMESTAMPTZ NOT NULL,
    "PromotionId" UUID NULL REFERENCES "Promotions"("Id") ON DELETE SET NULL,
    "DiscountAmount" DECIMAL(10,2) NOT NULL DEFAULT 0,
    "CreatedAt"   TIMESTAMPTZ NOT NULL DEFAULT now()
);
-- Для фонового ExpiredBookingWorker: пошук прострочених PENDING-бронювань
CREATE INDEX "IX_Bookings_Status_ExpiresAt" ON "Bookings" ("Status", "ExpiresAt");
CREATE INDEX "IX_Bookings_UserId" ON "Bookings" ("UserId");

CREATE TABLE "BookingSeats" (
    "Id"         UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "BookingId"  UUID NOT NULL REFERENCES "Bookings"("Id") ON DELETE CASCADE,
    "SeatId"     UUID NOT NULL REFERENCES "Seats"("Id") ON DELETE RESTRICT,
    "Price"      DECIMAL(10,2) NOT NULL,
    "CreatedAt"  TIMESTAMPTZ NOT NULL DEFAULT now(),
    CONSTRAINT "UQ_BookingSeats_Booking_Seat" UNIQUE ("BookingId", "SeatId")
);

CREATE TABLE "Tickets" (
    "Id"                UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "BookingId"         UUID NOT NULL REFERENCES "Bookings"("Id") ON DELETE CASCADE,
    "SessionId"         UUID NOT NULL REFERENCES "Sessions"("Id") ON DELETE RESTRICT,
    "SeatId"            UUID NOT NULL REFERENCES "Seats"("Id") ON DELETE RESTRICT,
    "TicketCode"        VARCHAR(50) NOT NULL,
    "QrCode"            TEXT NOT NULL DEFAULT '',
    "Status"            VARCHAR(20) NOT NULL DEFAULT 'ACTIVE',
    "ValidatedAt"       TIMESTAMPTZ NULL,
    "ValidatedByUserId" UUID NULL REFERENCES "Users"("Id") ON DELETE SET NULL,
    "CreatedAt"         TIMESTAMPTZ NOT NULL DEFAULT now(),
    CONSTRAINT "UQ_Tickets_TicketCode" UNIQUE ("TicketCode")
);

CREATE TABLE "Payments" (
    "Id"             UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "BookingId"      UUID NOT NULL UNIQUE REFERENCES "Bookings"("Id") ON DELETE CASCADE,
    "Amount"         DECIMAL(10,2) NOT NULL,
    "Method"         VARCHAR(20) NOT NULL DEFAULT 'ONLINE',
    "Status"         VARCHAR(20) NOT NULL DEFAULT 'PENDING',
    "TransactionId"  VARCHAR(100) NULL,
    "CreatedAt"      TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE INDEX "IX_Payments_TransactionId" ON "Payments" ("TransactionId");

CREATE TABLE "Reviews" (
    "Id"         UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "UserId"     UUID NOT NULL REFERENCES "Users"("Id") ON DELETE CASCADE,
    "MovieId"    UUID NOT NULL REFERENCES "Movies"("Id") ON DELETE CASCADE,
    "Rating"     INT NOT NULL,
    "Comment"    TEXT NOT NULL DEFAULT '',
    "IsVerified" BOOLEAN NOT NULL DEFAULT FALSE,
    "CreatedAt"  TIMESTAMPTZ NOT NULL DEFAULT now(),
    CONSTRAINT "UQ_Reviews_User_Movie" UNIQUE ("UserId", "MovieId"),
    CONSTRAINT "CK_Reviews_Rating_Range" CHECK ("Rating" >= 1 AND "Rating" <= 5)
);
