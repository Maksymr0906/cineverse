import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { bookingsApi } from "@/api/bookingsApi";
import type { Ticket } from "@/types";

export default function TicketsPage() {
  const { id } = useParams<{ id: string }>();
  const [tickets, setTickets] = useState<Ticket[] | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!id) return;
    bookingsApi
      .getTickets(id)
      .then(setTickets)
      .catch(() => setError("Не вдалося завантажити квитки. Можливо, оплата ще не підтверджена."));
  }, [id]);

  if (error) return <div className="p-10 text-center text-accent">{error}</div>;
  if (!tickets) return <div className="p-10 text-center text-ink-muted">Завантаження…</div>;

  return (
    <div className="mx-auto max-w-2xl px-6 py-12">
      <h1 className="marquee-heading text-3xl">Ваші квитки</h1>
      <p className="mt-1 text-sm text-ink-muted">Покажіть QR-код контролеру на вході в зал.</p>

      <div className="mt-8 grid gap-4 sm:grid-cols-2">
        {tickets.map((t) => (
          <div key={t.id} className="rounded-sm border border-charcoal-700 p-5 text-center">
            <img src={t.qrCodeBase64} alt={`QR-код квитка ${t.ticketCode}`} className="mx-auto h-40 w-40" />
            <p className="mt-3 font-mono text-sm text-ink-muted">{t.ticketCode}</p>
            <p className="mt-1">Ряд {t.rowNumber}, місце {t.seatNumber}</p>
            <p className={`mt-1 text-xs uppercase tracking-wide ${t.status === "ACTIVE" ? "text-marquee" : "text-ink-muted"}`}>
              {t.status === "ACTIVE" ? "Дійсний" : t.status === "USED" ? "Використано" : "Скасовано"}
            </p>
          </div>
        ))}
      </div>

      <Link to="/profile" className="mt-10 inline-block text-sm text-ink-muted hover:text-ink">← До особистого кабінету</Link>
    </div>
  );
}
