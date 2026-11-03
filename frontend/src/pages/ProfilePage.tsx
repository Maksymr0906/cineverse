import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { bookingsApi } from "@/api/bookingsApi";
import { useAppSelector } from "@/hooks/redux";
import type { Booking } from "@/types";

const STATUS_LABEL: Record<Booking["status"], string> = {
  PENDING: "Очікує оплати",
  CONFIRMED: "Оплачено",
  CANCELLED: "Скасовано",
  EXPIRED: "Протерміновано"
};

const STATUS_COLOR: Record<Booking["status"], string> = {
  PENDING: "text-marquee",
  CONFIRMED: "text-green-400",
  CANCELLED: "text-ink-muted",
  EXPIRED: "text-ink-muted"
};

export default function ProfilePage() {
  const { user } = useAppSelector((s) => s.auth);
  const [bookings, setBookings] = useState<Booking[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    bookingsApi
      .getMyBookings()
      .then(setBookings)
      .catch(() => setError("Не вдалося завантажити ваші бронювання. Спробуйте оновити сторінку."))
      .finally(() => setLoading(false));
  }, []);

  return (
    <div className="mx-auto max-w-3xl px-6 py-12">
      <h1 className="marquee-heading text-3xl">Особистий кабінет</h1>
      {user && <p className="mt-1 text-ink-muted">{user.firstName} {user.lastName} · {user.email}</p>}

      <h2 className="marquee-heading mt-10 text-xl">Мої бронювання</h2>

      {loading ? (
        <p className="mt-4 text-ink-muted">Завантаження…</p>
      ) : error ? (
        <p className="mt-4 text-accent">{error}</p>
      ) : bookings.length === 0 ? (
        <p className="mt-4 text-ink-muted">Ви ще не бронювали квитки. <Link to="/" className="text-marquee hover:underline">Обрати фільм</Link></p>
      ) : (
        <div className="mt-4 divide-y divide-charcoal-700">
          {bookings.map((b) => (
            <div key={b.id} className="flex items-center justify-between py-4">
              <div>
                <p className="font-medium">{b.movieTitle}</p>
                <p className="text-sm text-ink-muted">
                  {b.cinemaName} · {b.hallName} · {new Date(b.sessionStartTime).toLocaleString("uk-UA", { dateStyle: "medium", timeStyle: "short" })}
                </p>
                <p className="text-sm text-ink-muted">
                  {b.seats.map((s) => `${s.rowNumber}:${s.seatNumber}`).join(", ")}
                </p>
                <p className={`text-sm ${STATUS_COLOR[b.status]}`}>
                  {STATUS_LABEL[b.status]} · {b.totalPrice} грн
                  {b.discountAmount > 0 && <span className="text-marquee"> (промокод {b.appliedPromoCode})</span>}
                </p>
              </div>
              {b.status === "PENDING" && (
                <Link to={`/bookings/${b.id}/checkout`} className="text-sm text-marquee hover:underline">Оплатити</Link>
              )}
              {b.status === "CONFIRMED" && (
                <Link to={`/bookings/${b.id}/tickets`} className="text-sm text-marquee hover:underline">Квитки</Link>
              )}
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
