import { useState } from "react";
import type { ChangeEvent, FormEvent } from "react";
import { Link } from "react-router-dom";
import { authApi } from "@/api/authApi";
import { extractApiErrorMessage } from "@/utils/apiError";

export default function RegisterPage() {
  const [form, setForm] = useState({ firstName: "", lastName: "", email: "", phone: "", password: "" });
  const [submitted, setSubmitted] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const update = (key: keyof typeof form) => (e: ChangeEvent<HTMLInputElement>) =>
    setForm((f) => ({ ...f, [key]: e.target.value }));

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError(null);
    try {
      await authApi.register(form);
      setSubmitted(true);
    } catch (err) {
      setError(extractApiErrorMessage(err, "Не вдалося зареєструватись."));
    } finally {
      setLoading(false);
    }
  };

  if (submitted) {
    return (
      <div className="mx-auto max-w-sm px-6 py-20 text-center">
        <h1 className="marquee-heading text-3xl text-marquee">Перевірте пошту</h1>
        <p className="mt-3 text-ink-muted">
          Ми надіслали лист із посиланням для підтвердження на <strong className="text-ink">{form.email}</strong>.
          Перейдіть за ним, щоб завершити реєстрацію.
        </p>
        <Link to="/login" className="mt-6 inline-block text-marquee hover:underline">До входу</Link>
      </div>
    );
  }

  return (
    <div className="mx-auto flex max-w-sm flex-col px-6 py-12">
      <h1 className="marquee-heading text-4xl">Реєстрація</h1>

      <form onSubmit={handleSubmit} className="mt-8 flex flex-col gap-4">
        <div className="grid grid-cols-2 gap-3">
          <Field label="Ім'я" value={form.firstName} onChange={update("firstName")} />
          <Field label="Прізвище" value={form.lastName} onChange={update("lastName")} />
        </div>
        <Field label="Email" type="email" value={form.email} onChange={update("email")} />
        <Field label="Телефон" placeholder="+380..." value={form.phone} onChange={update("phone")} />
        <Field label="Пароль" type="password" value={form.password} onChange={update("password")} />
        <p className="text-xs text-ink-muted">Мінімум 8 символів, хоча б одна велика літера та цифра.</p>

        {error && <p className="text-sm text-accent">{error}</p>}

        <button
          type="submit"
          disabled={loading}
          className="mt-2 rounded-sm bg-accent py-2.5 font-medium text-ink hover:bg-accent-dim disabled:opacity-60 transition-colors"
        >
          {loading ? "Реєструємо…" : "Зареєструватись"}
        </button>
      </form>

      <p className="mt-6 text-sm text-ink-muted">
        Уже є акаунт? <Link to="/login" className="text-marquee hover:underline">Увійти</Link>
      </p>
    </div>
  );
}

function Field({
  label, value, onChange, type = "text", placeholder
}: { label: string; value: string; onChange: (e: ChangeEvent<HTMLInputElement>) => void; type?: string; placeholder?: string }) {
  return (
    <label className="flex flex-col gap-1.5 text-sm">
      {label}
      <input
        type={type}
        required
        value={value}
        placeholder={placeholder}
        onChange={onChange}
        className="rounded-sm border border-charcoal-600 bg-charcoal-900 px-3 py-2 outline-none focus:border-marquee"
      />
    </label>
  );
}
