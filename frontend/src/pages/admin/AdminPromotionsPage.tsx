import { useEffect, useState } from "react";
import type { FormEvent } from "react";
import { promotionsApi } from "@/api/promotionsApi";
import type { Promotion } from "@/api/promotionsApi";
import AdminNav from "@/components/AdminNav";
import { extractApiErrorMessage } from "@/utils/apiError";

const emptyForm = { code: "", discountType: "PERCENT", discountValue: 10, expiresAt: "", usageLimit: "" };

/**
 * Керування промокодами (п.2.1 ТЗ, сценарій "STUDENT20" — знижка 20%). Мінімальна форма:
 * список активних кодів + створення нового. Промокод одразу можна протестувати на сторінці
 * вибору місць (поле "Промокод" перед кнопкою "Забронювати").
 */
export default function AdminPromotionsPage() {
  const [promotions, setPromotions] = useState<Promotion[]>([]);
  const [form, setForm] = useState(emptyForm);
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const load = () => promotionsApi.getPromotions().then(setPromotions);

  useEffect(() => {
    load();
  }, []);

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setSubmitting(true);
    setError(null);
    try {
      await promotionsApi.createPromotion({
        code: form.code,
        discountType: form.discountType,
        discountValue: form.discountValue,
        expiresAt: new Date(form.expiresAt).toISOString(),
        usageLimit: form.usageLimit ? Number(form.usageLimit) : null
      });
      setForm(emptyForm);
      await load();
    } catch (err) {
      setError(extractApiErrorMessage(err, "Не вдалося створити промокод."));
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="mx-auto max-w-4xl px-6 py-10">
      <h1 className="marquee-heading text-3xl">Адмінка</h1>
      <div className="mt-4"><AdminNav /></div>

      <div className="mt-8 grid gap-10 sm:grid-cols-[1fr_320px]">
        <div>
          <h2 className="text-lg font-medium">Промокоди ({promotions.length})</h2>
          <div className="mt-3 divide-y divide-charcoal-700">
            {promotions.map((p) => (
              <div key={p.id} className="py-3">
                <p className="font-mono">{p.code}</p>
                <p className="text-xs text-ink-muted">
                  {p.discountType === "PERCENT" ? `-${p.discountValue}%` : `-${p.discountValue} грн`} · до{" "}
                  {new Date(p.expiresAt).toLocaleDateString("uk-UA")} · використано {p.usageCount}
                  {p.usageLimit ? ` з ${p.usageLimit}` : ""}
                </p>
              </div>
            ))}
            {promotions.length === 0 && <p className="py-3 text-ink-muted">Промокодів ще немає.</p>}
          </div>
        </div>

        <form onSubmit={handleSubmit} className="flex flex-col gap-3 rounded-sm border border-charcoal-700 p-4">
          <h2 className="text-lg font-medium">Новий промокод</h2>
          <input required placeholder="Код, напр. STUDENT20" value={form.code}
            onChange={(e) => setForm({ ...form, code: e.target.value.toUpperCase() })}
            className="rounded-sm border border-charcoal-600 bg-charcoal-900 px-3 py-2 text-sm font-mono uppercase" />

          <div className="grid grid-cols-2 gap-2">
            <select value={form.discountType} onChange={(e) => setForm({ ...form, discountType: e.target.value })}
              className="rounded-sm border border-charcoal-600 bg-charcoal-900 px-3 py-2 text-sm">
              <option value="PERCENT">Відсоток (%)</option>
              <option value="FIXED">Фіксована сума (грн)</option>
            </select>
            <input type="number" required min={1} value={form.discountValue}
              onChange={(e) => setForm({ ...form, discountValue: Number(e.target.value) })}
              className="rounded-sm border border-charcoal-600 bg-charcoal-900 px-3 py-2 text-sm" />
          </div>

          <label className="flex flex-col gap-1 text-xs text-ink-muted">
            Діє до
            <input type="date" required value={form.expiresAt}
              onChange={(e) => setForm({ ...form, expiresAt: e.target.value })}
              className="rounded-sm border border-charcoal-600 bg-charcoal-900 px-3 py-2 text-sm text-ink" />
          </label>

          <label className="flex flex-col gap-1 text-xs text-ink-muted">
            Ліміт використань (необов'язково)
            <input type="number" min={1} value={form.usageLimit}
              onChange={(e) => setForm({ ...form, usageLimit: e.target.value })}
              placeholder="Без обмежень"
              className="rounded-sm border border-charcoal-600 bg-charcoal-900 px-3 py-2 text-sm text-ink" />
          </label>

          {error && <p className="text-sm text-accent">{error}</p>}

          <button type="submit" disabled={submitting} className="mt-2 rounded-sm bg-accent py-2 font-medium hover:bg-accent-dim disabled:opacity-60">
            {submitting ? "Зберігаємо…" : "Створити промокод"}
          </button>
        </form>
      </div>
    </div>
  );
}
