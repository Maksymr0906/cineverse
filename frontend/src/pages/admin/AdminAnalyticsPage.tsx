import { useEffect, useState } from "react";
import { Bar, BarChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from "recharts";
import { analyticsApi } from "@/api/analyticsApi";
import type { RevenueByDay, SessionOccupancy } from "@/api/analyticsApi";
import AdminNav from "@/components/AdminNav";

/**
 * Мінімальний дашборд аналітики (п. "бажано" аудиту): не графіки на всі 20 залів,
 * а один графік "виручка за день" + таблиця "заповнюваність сеансу" на вже наявних даних.
 */
export default function AdminAnalyticsPage() {
  const [revenue, setRevenue] = useState<RevenueByDay[]>([]);
  const [occupancy, setOccupancy] = useState<SessionOccupancy[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    Promise.all([analyticsApi.getRevenueByDay(14), analyticsApi.getSessionOccupancy(7)])
      .then(([r, o]) => {
        setRevenue(r);
        setOccupancy(o);
      })
      .finally(() => setLoading(false));
  }, []);

  const totalRevenue = revenue.reduce((sum, r) => sum + r.revenue, 0);

  return (
    <div className="mx-auto max-w-4xl px-6 py-10">
      <h1 className="marquee-heading text-3xl">Адмінка</h1>
      <div className="mt-4"><AdminNav /></div>

      {loading ? (
        <p className="mt-8 text-ink-muted">Завантаження…</p>
      ) : (
        <>
          <section className="mt-8">
            <div className="flex items-baseline justify-between">
              <h2 className="marquee-heading text-2xl">Виручка за 14 днів</h2>
              <p className="text-xl font-semibold text-marquee">{totalRevenue.toLocaleString("uk-UA")} грн</p>
            </div>
            {revenue.length === 0 ? (
              <p className="mt-4 text-ink-muted">За цей період немає оплачених бронювань.</p>
            ) : (
              <div className="mt-4 h-64 rounded-sm border border-charcoal-700 p-4">
                <ResponsiveContainer width="100%" height="100%">
                  <BarChart data={revenue}>
                    <CartesianGrid strokeDasharray="3 3" stroke="#28282f" />
                    <XAxis dataKey="date" tick={{ fill: "#9a99a2", fontSize: 12 }} />
                    <YAxis tick={{ fill: "#9a99a2", fontSize: 12 }} />
                    <Tooltip
                      contentStyle={{ background: "#17171b", border: "1px solid #28282f", color: "#f5f5f0" }}
                      formatter={(value: number) => [`${value} грн`, "Виручка"]}
                    />
                    <Bar dataKey="revenue" fill="#f2c14e" radius={[3, 3, 0, 0]} />
                  </BarChart>
                </ResponsiveContainer>
              </div>
            )}
          </section>

          <section className="mt-10">
            <h2 className="marquee-heading text-2xl">Заповнюваність сеансів (±7 днів)</h2>
            {occupancy.length === 0 ? (
              <p className="mt-4 text-ink-muted">Немає сеансів у цьому діапазоні.</p>
            ) : (
              <table className="mt-4 w-full text-left text-sm">
                <thead className="text-ink-muted">
                  <tr>
                    <th className="pb-2">Фільм</th>
                    <th className="pb-2">Зал</th>
                    <th className="pb-2">Час</th>
                    <th className="pb-2">Продано</th>
                    <th className="pb-2">Заповнюваність</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-charcoal-700">
                  {occupancy.map((o) => (
                    <tr key={o.sessionId}>
                      <td className="py-2">{o.movieTitle}</td>
                      <td className="py-2 text-ink-muted">{o.cinemaName} — {o.hallName}</td>
                      <td className="py-2 text-ink-muted">{new Date(o.startTime).toLocaleString("uk-UA", { dateStyle: "short", timeStyle: "short" })}</td>
                      <td className="py-2 text-ink-muted">{o.soldSeats}/{o.capacity}</td>
                      <td className="py-2">
                        <div className="flex items-center gap-2">
                          <div className="h-2 w-24 overflow-hidden rounded-full bg-charcoal-800">
                            <div className="h-full bg-marquee" style={{ width: `${Math.min(o.occupancyPercent, 100)}%` }} />
                          </div>
                          <span className="text-ink-muted">{o.occupancyPercent}%</span>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </section>
        </>
      )}
    </div>
  );
}
