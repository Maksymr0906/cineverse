import { useState } from "react";
import type { FormEvent } from "react";
import { Link } from "react-router-dom";
import { authApi } from "@/api/authApi";
import { extractApiErrorMessage } from "@/utils/apiError";

export default function ForgotPasswordPage() {
  const [email, setEmail] = useState("");
  const [submitted, setSubmitted] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError(null);
    try {
      await authApi.forgotPassword(email);
      // Бекенд навмисно відповідає однаково незалежно від того, чи існує такий email
      // (захист від User Enumeration) — тому й тут показуємо один і той самий екран успіху.
      setSubmitted(true);
    } catch (err) {
      setError(extractApiErrorMessage(err, "Не вдалося надіслати запит. Спробуйте ще раз."));
    } finally {
      setLoading(false);
    }
  };

  if (submitted) {
    return (
      <div className="mx-auto max-w-sm px-6 py-20 text-center">
        <h1 className="marquee-heading text-3xl text-marquee">Перевірте пошту</h1>
        <p className="mt-3 text-ink-muted">
          Якщо акаунт з такою адресою існує, на неї надіслано посилання для відновлення пароля.
          Посилання дійсне 30 хвилин.
        </p>
        <Link to="/login" className="mt-6 inline-block text-marquee hover:underline">До входу</Link>
      </div>
    );
  }

  return (
    <div className="mx-auto flex min-h-[60vh] max-w-sm flex-col justify-center px-6">
      <h1 className="marquee-heading text-4xl">Забули пароль?</h1>
      <p className="mt-2 text-sm text-ink-muted">Вкажіть email — надішлемо посилання для встановлення нового пароля.</p>

      <form onSubmit={handleSubmit} className="mt-8 flex flex-col gap-4">
        <input
          type="email"
          required
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          placeholder="you@example.com"
          className="rounded-sm border border-charcoal-600 bg-charcoal-900 px-3 py-2 outline-none focus:border-marquee"
        />
        {error && <p className="text-sm text-accent">{error}</p>}
        <button
          type="submit"
          disabled={loading}
          className="rounded-sm bg-accent py-2.5 font-medium text-ink hover:bg-accent-dim disabled:opacity-60"
        >
          {loading ? "Надсилаємо…" : "Надіслати посилання"}
        </button>
      </form>

      <Link to="/login" className="mt-6 text-sm text-ink-muted hover:text-ink">← До входу</Link>
    </div>
  );
}
