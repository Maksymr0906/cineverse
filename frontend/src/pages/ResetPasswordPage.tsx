import { useState } from "react";
import type { FormEvent } from "react";
import { Link, useNavigate, useSearchParams } from "react-router-dom";
import { authApi } from "@/api/authApi";
import { extractApiErrorMessage } from "@/utils/apiError";

export default function ResetPasswordPage() {
  const [searchParams] = useSearchParams();
  const token = searchParams.get("token");
  const navigate = useNavigate();

  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [success, setSuccess] = useState(false);

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    if (!token) return;
    setLoading(true);
    setError(null);
    try {
      await authApi.resetPassword(token, password);
      setSuccess(true);
      setTimeout(() => navigate("/login"), 2000);
    } catch (err) {
      setError(extractApiErrorMessage(err, "Не вдалося змінити пароль."));
    } finally {
      setLoading(false);
    }
  };

  if (!token) {
    return (
      <div className="mx-auto max-w-sm px-6 py-20 text-center">
        <h1 className="marquee-heading text-3xl text-accent">Недійсне посилання</h1>
        <p className="mt-3 text-ink-muted">У посиланні відсутній токен. Запросіть відновлення пароля ще раз.</p>
        <Link to="/forgot-password" className="mt-6 inline-block text-marquee hover:underline">Запросити знову</Link>
      </div>
    );
  }

  if (success) {
    return (
      <div className="mx-auto max-w-sm px-6 py-20 text-center">
        <h1 className="marquee-heading text-3xl text-marquee">Пароль змінено</h1>
        <p className="mt-3 text-ink-muted">Зараз ви перейдете на сторінку входу…</p>
      </div>
    );
  }

  return (
    <div className="mx-auto flex min-h-[60vh] max-w-sm flex-col justify-center px-6">
      <h1 className="marquee-heading text-4xl">Новий пароль</h1>

      <form onSubmit={handleSubmit} className="mt-8 flex flex-col gap-4">
        <input
          type="password"
          required
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          placeholder="Новий пароль"
          className="rounded-sm border border-charcoal-600 bg-charcoal-900 px-3 py-2 outline-none focus:border-marquee"
        />
        <p className="text-xs text-ink-muted">Мінімум 8 символів, хоча б одна велика літера та цифра.</p>
        {error && <p className="text-sm text-accent">{error}</p>}
        <button
          type="submit"
          disabled={loading}
          className="rounded-sm bg-accent py-2.5 font-medium text-ink hover:bg-accent-dim disabled:opacity-60"
        >
          {loading ? "Зберігаємо…" : "Встановити пароль"}
        </button>
      </form>
    </div>
  );
}
