import { useEffect, useMemo, useRef, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { bookingsApi } from "@/api/bookingsApi";
import { useSeatStatusUpdates } from "@/hooks/useSignalR";
import { useAppSelector } from "@/hooks/redux";
import SeatMap from "@/components/SeatMap";
import { extractApiErrorMessage } from "@/utils/apiError";
import type { SeatMap as SeatMapType, SeatMapSeat } from "@/types";

const MAX_SEATS_PER_BOOKING = 10; // Дзеркалить CreateBookingCommandValidator на бекенді (Етап 5).
const SEAT_SELECTION_STORAGE_KEY_PREFIX = "cineverse:pending-seats:";

/**
 * Сторінка вибору місць (Етап 5): живі статуси через SignalR — коли хтось інший
 * обирає/звільняє місце, ця сторінка оновлюється миттєво, без перезавантаження.
 */
export default function SeatSelectionPage() {
  const { sessionId } = useParams<{ sessionId: string }>();
  const navigate = useNavigate();
  const { status: authStatus } = useAppSelector((s) => s.auth);

  const [seatMap, setSeatMap] = useState<SeatMapType | null>(null);
  const [selectedSeatIds, setSelectedSeatIds] = useState<string[]>(() => restorePendingSelection(sessionId));
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [limitWarning, setLimitWarning] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [promoCode, setPromoCode] = useState("");

  const liveUpdate = useSeatStatusUpdates(sessionId);

  // Виправлення після аудиту: якщо оновлення від SignalR приходить ДО того, як резолвиться
  // початковий REST-запит getSeatMap (цілком реальна гонка — з'єднання й GET ідуть паралельно),
  // те оновлення раніше просто губилось назавжди (ефект нижче мав ранній вихід на !seatMap).
  // Тепер такі "передчасні" оновлення складаються в чергу й застосовуються одразу після
  // завантаження карти місць.
  const pendingUpdatesRef = useRef<{ seatId: string; status: string }[]>([]);

  useEffect(() => {
    if (!sessionId) return;
    bookingsApi
      .getSeatMap(sessionId)
      .then((map) => {
        if (pendingUpdatesRef.current.length > 0) {
          map = applySeatUpdates(map, pendingUpdatesRef.current);
          pendingUpdatesRef.current = [];
        }
        setSeatMap(map);
      })
      .catch(() => setError("Не вдалося завантажити схему залу."))
      .finally(() => setLoading(false));
  }, [sessionId]);

  useEffect(() => {
    if (!liveUpdate) return;

    setSeatMap((prev) => {
      if (!prev) {
        // Карта ще не завантажена — не втрачаємо оновлення, а відкладаємо його.
        pendingUpdatesRef.current.push(...liveUpdate);
        return prev;
      }
      return applySeatUpdates(prev, liveUpdate);
    });

    // Якщо місце, яке я щойно обрав, хтось перехопив (малоймовірно, але можливо між подіями) — знімаємо вибір.
    setSelectedSeatIds((prev) => prev.filter((id) => !liveUpdate.some((c) => c.seatId === id && c.status !== "FREE")));
  }, [liveUpdate]);

  // Зберігаємо поточний вибір при кожній зміні — щоб пережити редірект на /login і назад.
  useEffect(() => {
    if (!sessionId) return;
    savePendingSelection(sessionId, selectedSeatIds);
  }, [sessionId, selectedSeatIds]);

  const toggleSeat = (seat: SeatMapSeat) => {
    setLimitWarning(null);
    setSelectedSeatIds((prev) => {
      if (prev.includes(seat.seatId)) {
        return prev.filter((id) => id !== seat.seatId);
      }
      if (prev.length >= MAX_SEATS_PER_BOOKING) {
        setLimitWarning(`За одне бронювання можна обрати не більше ${MAX_SEATS_PER_BOOKING} місць.`);
        return prev;
      }
      return [...prev, seat.seatId];
    });
  };

  const selectedSeats = useMemo(
    () => seatMap?.seats.filter((s) => selectedSeatIds.includes(s.seatId)) ?? [],
    [seatMap, selectedSeatIds]
  );
  const total = selectedSeats.reduce((sum, s) => sum + s.price, 0);

  const handleBook = async () => {
    if (!sessionId || selectedSeatIds.length === 0) return;

    if (authStatus !== "authenticated") {
      // selectedSeatIds уже збережено в sessionStorage ефектом вище — після логіну користувача
      // поверне сюди ж, і вибір відновиться (див. restorePendingSelection при ініціалізації стану).
      navigate("/login", { state: { from: { pathname: `/sessions/${sessionId}/seats` } } });
      return;
    }

    setSubmitting(true);
    setError(null);
    try {
      const booking = await bookingsApi.createBooking(sessionId, selectedSeatIds, promoCode.trim() || undefined);
      clearPendingSelection(sessionId);
      navigate(`/bookings/${booking.id}/checkout`);
    } catch (err) {
      setError(extractApiErrorMessage(err, "Не вдалося створити бронювання."));
      // Оновлюємо карту місць — можливо, хтось встиг зайняти обране місце першим.
      if (sessionId) bookingsApi.getSeatMap(sessionId).then(setSeatMap);
      setSelectedSeatIds([]);
    } finally {
      setSubmitting(false);
    }
  };

  if (loading) return <div className="p-10 text-center text-ink-muted">Завантажуємо схему залу…</div>;
  if (!seatMap) return <div className="p-10 text-center text-accent">{error ?? "Сеанс не знайдено."}</div>;

  return (
    <div className="mx-auto max-w-4xl px-6 py-10">
      <h1 className="marquee-heading text-3xl">{seatMap.hallName}</h1>
      <p className="text-sm text-ink-muted">
        Оберіть місця (не більше {MAX_SEATS_PER_BOOKING}) — статуси оновлюються в реальному часі для всіх глядачів.
      </p>

      <div className="mt-8">
        <SeatMap seats={seatMap.seats} selectedSeatIds={selectedSeatIds} onToggleSeat={toggleSeat} />
      </div>

      {limitWarning && <p className="mt-4 text-center text-sm text-marquee">{limitWarning}</p>}
      {error && <p className="mt-4 text-center text-sm text-accent">{error}</p>}

      <div className="mx-auto mt-8 max-w-xs">
        <label className="flex flex-col gap-1.5 text-sm text-ink-muted">
          Промокод (необов'язково)
          <input
            value={promoCode}
            onChange={(e) => setPromoCode(e.target.value)}
            placeholder="Напр. STUDENT20"
            className="rounded-sm border border-charcoal-600 bg-charcoal-900 px-3 py-2 text-sm uppercase outline-none focus:border-marquee"
          />
        </label>
      </div>

      <div className="sticky bottom-0 mt-10 flex items-center justify-between border-t border-charcoal-700 bg-charcoal-950/95 py-4 backdrop-blur">
        <div>
          <p className="text-sm text-ink-muted">
            {selectedSeats.length > 0
              ? selectedSeats.map((s) => `${s.rowNumber}:${s.seatNumber}`).join(", ")
              : "Місця не обрано"}
          </p>
          <p className="text-xl font-semibold text-marquee">{total} грн</p>
        </div>
        <button
          onClick={handleBook}
          disabled={selectedSeatIds.length === 0 || submitting}
          className="rounded-sm bg-accent px-6 py-3 font-medium text-ink hover:bg-accent-dim disabled:opacity-50 transition-colors"
        >
          {submitting ? "Бронюємо…" : "Забронювати"}
        </button>
      </div>
    </div>
  );
}

function applySeatUpdates(map: SeatMapType, changes: { seatId: string; status: string }[]): SeatMapType {
  const updates = new Map(changes.map((c) => [c.seatId, c.status]));
  return {
    ...map,
    seats: map.seats.map((s) => (updates.has(s.seatId) ? { ...s, status: updates.get(s.seatId) as SeatMapSeat["status"] } : s))
  };
}

function storageKey(sessionId: string) {
  return `${SEAT_SELECTION_STORAGE_KEY_PREFIX}${sessionId}`;
}

function restorePendingSelection(sessionId: string | undefined): string[] {
  if (!sessionId) return [];
  try {
    const raw = sessionStorage.getItem(storageKey(sessionId));
    return raw ? (JSON.parse(raw) as string[]) : [];
  } catch {
    return [];
  }
}

function savePendingSelection(sessionId: string, seatIds: string[]) {
  try {
    if (seatIds.length === 0) {
      sessionStorage.removeItem(storageKey(sessionId));
    } else {
      sessionStorage.setItem(storageKey(sessionId), JSON.stringify(seatIds));
    }
  } catch {
    // sessionStorage може бути недоступний (приватний режим тощо) — це не критично,
    // просто вибір не переживе редірект на логін.
  }
}

function clearPendingSelection(sessionId: string) {
  try {
    sessionStorage.removeItem(storageKey(sessionId));
  } catch {
    /* no-op */
  }
}
