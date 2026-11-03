import { useState } from "react";
import { useNavigate, useParams, useSearchParams } from "react-router-dom";
import { paymentsApi } from "@/api/paymentsApi";
import { extractApiErrorMessage } from "@/utils/apiError";

/**
 * Сторінка, що імітує сторінку банку — вона лише існує тому, що в проєкті ще немає доступу
 * до реального LiqPay/Stripe (див. README, розділ "Оплата та квитки"). Раніше ховалась за
 * import.meta.env.DEV, дзеркально до перевірки IsDevelopment() на бекенді — але оскільки
 * реального шлюзу все одно немає, це блокувало єдиний робочий спосіб завершити оплату навіть
 * у "Production"-збірці. Захист від майбутнього реального шлюзу тепер лежить на бекенді
 * (PaymentsController.MockComplete перевіряє, чи зареєстрований саме MockPaymentGatewayService).
 */
export default function MockPaymentPage() {
  const { transactionId } = useParams<{ transactionId: string }>();
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const paymentId = searchParams.get("paymentId");
  const amount = searchParams.get("amount");

  const complete = async (success: boolean) => {
    if (!transactionId) return;
    setLoading(true);
    setError(null);
    try {
      await paymentsApi.mockComplete(transactionId, success);
      navigate(success ? "/profile" : "/");
    } catch (err) {
      // Виправлення після аудиту: раніше без try/catch запит, що впав, лишав loading=true
      // назавжди — кнопки блокувались, і користувач не бачив жодного пояснення.
      setError(extractApiErrorMessage(err, "Не вдалося обробити імітацію оплати."));
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="mx-auto max-w-sm px-6 py-20 text-center">
      <p className="text-xs uppercase tracking-[0.3em] text-ink-muted">Імітація банківської сторінки</p>
      <h1 className="marquee-heading mt-2 text-3xl">Оплата {amount} грн</h1>
      <p className="mt-2 text-xs text-ink-muted">Payment ID: {paymentId}</p>

      {error && <p className="mt-4 text-sm text-accent">{error}</p>}

      <div className="mt-8 flex flex-col gap-3">
        <button
          onClick={() => complete(true)}
          disabled={loading}
          className="rounded-sm bg-marquee py-3 font-medium text-charcoal-950 hover:opacity-90 disabled:opacity-60"
        >
          Оплата успішна
        </button>
        <button
          onClick={() => complete(false)}
          disabled={loading}
          className="rounded-sm border border-charcoal-600 py-3 text-ink-muted hover:text-ink"
        >
          Скасувати оплату
        </button>
      </div>
    </div>
  );
}
