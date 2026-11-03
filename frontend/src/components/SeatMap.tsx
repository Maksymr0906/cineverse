import type { SeatMapSeat } from "@/types";

interface SeatMapProps {
  seats: SeatMapSeat[];
  selectedSeatIds: string[];
  onToggleSeat: (seat: SeatMapSeat) => void;
}

const SEAT_TYPE_LABEL: Record<string, string> = {
  STANDARD: "Стандарт",
  VIP: "VIP",
  LOVE_SEAT: "Love Seat",
  DISABLED: "Для МГН"
};

function seatClasses(seat: SeatMapSeat, isSelected: boolean): string {
  if (seat.status === "SOLD") return "bg-charcoal-700 text-charcoal-600 cursor-not-allowed";
  if (seat.status === "LOCKED") return "bg-charcoal-700/60 text-charcoal-600 cursor-not-allowed animate-pulse";
  if (isSelected) return "bg-marquee text-charcoal-950 border-marquee";
  if (seat.seatType === "VIP") return "border border-marquee/70 text-marquee hover:bg-marquee/20";
  if (seat.seatType === "LOVE_SEAT") return "border border-accent/70 text-accent hover:bg-accent/20";
  return "border border-charcoal-600 text-ink-muted hover:border-ink hover:text-ink";
}

/** Схема залу: вигнутий "екран" зверху, місця згруповані по рядах. Клікабельні лише FREE. */
export default function SeatMap({ seats, selectedSeatIds, onToggleSeat }: SeatMapProps) {
  const rows = Array.from(new Set(seats.map((s) => s.rowNumber))).sort((a, b) => a - b);

  return (
    <div className="flex flex-col items-center gap-6">
      <div className="relative h-3 w-full max-w-2xl">
        <div className="absolute inset-x-8 top-0 h-3 rounded-[100%] bg-gradient-to-b from-ink/30 to-transparent" />
        <p className="mt-4 text-center text-xs uppercase tracking-[0.3em] text-ink-muted">Екран</p>
      </div>

      <div className="flex flex-col gap-2">
        {rows.map((rowNumber) => (
          <div key={rowNumber} className="flex items-center gap-2">
            <span className="w-5 text-right text-xs text-ink-muted">{rowNumber}</span>
            <div className="flex gap-1.5">
              {seats
                .filter((s) => s.rowNumber === rowNumber)
                .sort((a, b) => a.seatNumber - b.seatNumber)
                .map((seat) => {
                  const isSelected = selectedSeatIds.includes(seat.seatId);
                  const clickable = seat.status === "FREE";
                  return (
                    <button
                      key={seat.seatId}
                      type="button"
                      disabled={!clickable}
                      title={`Ряд ${seat.rowNumber}, місце ${seat.seatNumber} · ${SEAT_TYPE_LABEL[seat.seatType]} · ${seat.price} грн`}
                      onClick={() => clickable && onToggleSeat(seat)}
                      className={`flex h-7 w-7 items-center justify-center rounded-t-md text-[10px] font-medium transition-colors ${seatClasses(seat, isSelected)}`}
                    >
                      {seat.seatNumber}
                    </button>
                  );
                })}
            </div>
          </div>
        ))}
      </div>

      <div className="flex flex-wrap justify-center gap-4 text-xs text-ink-muted">
        <LegendItem swatchClass="border border-charcoal-600" label="Вільно" />
        <LegendItem swatchClass="border border-marquee/70 text-marquee" label="VIP" />
        <LegendItem swatchClass="border border-accent/70 text-accent" label="Love Seat" />
        <LegendItem swatchClass="bg-marquee" label="Обрано" />
        <LegendItem swatchClass="bg-charcoal-700" label="Продано / зайнято" />
      </div>
    </div>
  );
}

function LegendItem({ swatchClass, label }: { swatchClass: string; label: string }) {
  return (
    <div className="flex items-center gap-1.5">
      <span className={`h-3.5 w-3.5 rounded-sm ${swatchClass}`} />
      {label}
    </div>
  );
}
