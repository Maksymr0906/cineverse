import { useState } from "react";
import type { FormEvent } from "react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import { useAppDispatch, useAppSelector } from "@/hooks/redux";
import { login } from "@/features/auth/authSlice";

export default function LoginPage() {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const location = useLocation();
  const { status, error } = useAppSelector((s) => s.auth);

  const from = (location.state as { from?: { pathname: string } })?.from?.pathname ?? "/";

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    const result = await dispatch(login({ email, password }));
    if (login.fulfilled.match(result)) {
      navigate(from, { replace: true });
    }
  };

  return (
    <div className="mx-auto flex min-h-[70vh] max-w-sm flex-col justify-center px-6">
      <h1 className="marquee-heading text-4xl">Вхід</h1>
      <p className="mt-2 text-sm text-ink-muted">Увійдіть, щоб бронювати квитки та бачити свою історію переглядів.</p>

      <form onSubmit={handleSubmit} className="mt-8 flex flex-col gap-4">
        <label className="flex flex-col gap-1.5 text-sm">
          Email
          <input
            type="email"
            required
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            className="rounded-sm border border-charcoal-600 bg-charcoal-900 px-3 py-2 outline-none focus:border-marquee"
          />
        </label>

        <label className="flex flex-col gap-1.5 text-sm">
          Пароль
          <input
            type="password"
            required
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            className="rounded-sm border border-charcoal-600 bg-charcoal-900 px-3 py-2 outline-none focus:border-marquee"
          />
        </label>

        <Link to="/forgot-password" className="self-end text-xs text-ink-muted hover:text-marquee">Забули пароль?</Link>

        {error && <p className="text-sm text-accent">{error}</p>}

        <button
          type="submit"
          disabled={status === "loading"}
          className="mt-2 rounded-sm bg-accent py-2.5 font-medium text-ink hover:bg-accent-dim disabled:opacity-60 transition-colors"
        >
          {status === "loading" ? "Входимо…" : "Увійти"}
        </button>
      </form>

      <p className="mt-6 text-sm text-ink-muted">
        Ще немає акаунту? <Link to="/register" className="text-marquee hover:underline">Зареєструватись</Link>
      </p>
    </div>
  );
}
