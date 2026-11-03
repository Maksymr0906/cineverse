import http from "k6/http";
import { check } from "k6";

/**
 * Навантажувальний/конкурентний тест ядра Етапу 5: N віртуальних користувачів одночасно
 * намагаються забронювати ОДНЕ Й ТЕ САМЕ місце на той самий сеанс. Якщо Redis-лок
 * (SET NX PX) справді атомарний, очікуваний результат — рівно ОДИН HTTP 201/200,
 * решта — 409 Conflict. Якщо колись у коді з'явиться регресія (наприклад, хтось
 * прибере Redis-лок і лишить тільки перевірку в БД), цей тест має це виявити:
 * кількість успішних відповідей стане > 1.
 *
 * Запуск (потребує встановленого k6: https://k6.io/docs/get-started/installation/):
 *   SESSION_ID=<guid> SEAT_ID=<guid> ACCESS_TOKEN=<jwt> k6 run tests/load/booking-race-condition.js
 *
 * SESSION_ID/SEAT_ID беруться з БД (напр. через /api/sessions і /api/bookings/seat-map/{id}),
 * ACCESS_TOKEN — з POST /api/auth/login. Кожен VU має логінитись під ОКРЕМИМ користувачем
 * у реальному сценарії; для простоти цей скрипт демонструє гонку одним токеном
 * (сервер це не забороняє, оскільки Race Condition тут — про місце, а не про користувача).
 */
export const options = {
  scenarios: {
    race_for_one_seat: {
      executor: "shared-iterations",
      vus: 20,       // 20 "одночасних" спроб забронювати те саме місце
      iterations: 20,
      maxDuration: "30s"
    }
  }
};

const BASE_URL = __ENV.BASE_URL || "http://localhost:5000";
const SESSION_ID = __ENV.SESSION_ID;
const SEAT_ID = __ENV.SEAT_ID;
const ACCESS_TOKEN = __ENV.ACCESS_TOKEN;

export default function () {
  const res = http.post(
    `${BASE_URL}/api/bookings`,
    JSON.stringify({ sessionId: SESSION_ID, seatIds: [SEAT_ID] }),
    { headers: { "Content-Type": "application/json", Authorization: `Bearer ${ACCESS_TOKEN}` } }
  );

  check(res, {
    "статус 200/201 (переміг гонку) або 409 (місце вже зайняте)": (r) => [200, 201, 409].includes(r.status)
  });
}

// Після прогону: подивіться у власному моніторингу/логах бекенду, скільки записів Booking
// реально створилось для SESSION_ID+SEAT_ID — має бути РІВНО ОДИН.
