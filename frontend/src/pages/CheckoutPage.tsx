import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { bookingsApi } from "@/api/bookingsApi";
import { paymentsApi } from "@/api/paymentsApi";
import CountdownTimer from "@/components/CountdownTimer";
import { extractApiErrorMessage } from "@/utils/apiError";
import type { Booking } from "@/types";

/** Сторінка оформлення: показує склад бронювання, таймер до ExpiresAt (10 хв) і кнопку оплати. */
export default function CheckoutPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [booking, setBooking] = useState<Booking | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [redirecting, setRedirecting] = useState(false);
  const [cancelling, setCancelling] = useState(false);

  useEffect(() => {
    if (!id) return;
    bookingsApi.getBookingById(id).then(setBooking).catch(() => setError("Бронювання не знайдено."));
  }, [id]);

  const handlePay = async () => {
    if (!id) return;
    setRedirecting(true);
    try {
      const { redirectUrl } = await paymentsApi.initiate(id);
      window.location.href = redirectUrl;
    } catch (err) {
      setError(extractApiErrorMessage(err, "Не вдалося перейти до оплати."));
      setRedirecting(false);
    }
  };

  const handleCancel = async () => {
    if (!id) return;
    setCancelling(true);
    setError(null);
    try {
      await bookingsApi.cancelBooking(id);
      navigate("/");
    } catch (err) {
      // Виправлення після аудиту: раніше тут не було try/catch — якщо скасування падало
      // (напр. бронювання вже протерміноване чи вже оплачене іншим шляхом), користувач
      // не бачив нічого, а navigate("/") просто мовчки не виконувався.
      setError(extractApiErrorMessage(err, "Не вдалося скасувати бронювання."));
    } finally {
      setCancelling(false);
    }
  };

  if (error && !booking) return <div className="p-10 text-center text-accent">{error}</div>;
  if (!booking) return <div className="p-10 text-center text-ink-muted">Завантаження…</div>;

  if (booking.status !== "PENDING") {
    return (
      <div className="mx-auto max-w-md px-6 py-20 text-center">
        <p className="text-ink-muted">Це бронювання вже має статус «{booking.status}».</p>
        {booking.status === "CONFIRMED" && (
          <button onClick={() => navigate(`/bookings/${booking.id}/tickets`)} className="mt-4 text-marquee hover:underline">
            Переглянути квитки
          </button>
        )}
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-md px-6 py-14">
      <h1 className="marquee-heading text-3xl">Оформлення</h1>

      <div className="mt-6 rounded-sm border border-charcoal-700 p-5">
        <p className="font-medium">{booking.movieTitle}</p>
        <p className="text-sm text-ink-muted">
          {booking.cinemaName} · {booking.hallName} ·{" "}
          {new Date(booking.sessionStartTime).toLocaleString("uk-UA", { dateStyle: "medium", timeStyle: "short" })}
        </p>

        <p className="mt-4 text-sm text-ink-muted">Місця</p>
        <p className="mt-1">{booking.seats.map((s) => `Ряд ${s.rowNumber}, місце ${s.seatNumber}`).join("; ")}</p>

        <p className="mt-4 text-sm text-ink-muted">До сплати</p>
        {booking.discountAmount > 0 && (
          <p className="mt-1 text-sm text-ink-muted">
            Знижка за промокодом <span className="text-marquee">{booking.appliedPromoCode}</span>: −{booking.discountAmount} грн
          </p>
        )}
        <p className="mt-1 text-2xl font-semibold text-marquee">{booking.totalPrice} грн</p>

        <p className="mt-4 text-sm text-ink-muted">
          Час на оплату: <CountdownTimer expiresAt={booking.expiresAt} onExpire={() => setError("Час на оплату вичерпано. Оберіть місця повторно.")} />
        </p>
      </div>

      {error && <p className="mt-4 text-sm text-accent">{error}</p>}

      <div className="mt-6 flex flex-col gap-3">
        <button
          onClick={handlePay}
          disabled={redirecting || cancelling}
          className="rounded-sm bg-accent py-3 font-medium text-ink hover:bg-accent-dim disabled:opacity-60 transition-colors"
        >
          {redirecting ? "Переходимо до оплати…" : "Оплатити онлайн"}
        </button>
        <button
          onClick={handleCancel}
          disabled={redirecting || cancelling}
          className="rounded-sm border border-charcoal-600 py-3 text-ink-muted hover:text-ink disabled:opacity-60 transition-colors"
        >
          {cancelling ? "Скасовуємо…" : "Скасувати бронювання"}
        </button>
      </div>
    </div>
  );
}
