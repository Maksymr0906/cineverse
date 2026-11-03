import { useEffect, useState } from "react";
import type { FormEvent } from "react";
import { moviesApi } from "@/api/moviesApi";
import { cinemasApi } from "@/api/cinemasApi";
import { sessionsApi } from "@/api/sessionsApi";
import AdminNav from "@/components/AdminNav";
import { extractApiErrorMessage } from "@/utils/apiError";
import type { Cinema, MovieListItem, SessionListItem } from "@/types";

const emptyForm = { movieId: "", hallId: "", startTime: "", format: "2D", basePrice: 150 };

/** Перетворює ISO-рядок на значення для &lt;input type="datetime-local"&gt; у ЛОКАЛЬНОМУ часі
 * (звичайний new Date().toISOString() тут не підходить — datetime-local очікує рядок без зони). */
function toDatetimeLocalValue(iso: string): string {
  const d = new Date(iso);
  const pad = (n: number) => String(n).padStart(2, "0");
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

/**
 * Конструктор розкладу (Етап 4): сервер сам обчислює EndTime і перевіряє конфлікти
 * в межах залу — тому форма навмисно проста (фільм/зал/час/формат/ціна), а помилку
 * 409 з описом конфліктуючого сеансу показуємо як є, без дублювання цієї логіки на фронтенді.
 *
 * Редагування (виправлення після аудиту, sessionsApi.updateSession раніше ніде не викликався):
 * бекенд (UpdateSessionCommand) свідомо не дозволяє змінити ФІЛЬМ уже створеного сеансу —
 * лише зал/час/формат/ціну. Тому при редагуванні поле фільму показується, але заблоковане.
 */
export default function AdminSessionsPage() {
  const [movies, setMovies] = useState<MovieListItem[]>([]);
  const [cinemas, setCinemas] = useState<Cinema[]>([]);
  const [sessions, setSessions] = useState<SessionListItem[]>([]);
  const [form, setForm] = useState(emptyForm);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const loadSessions = () => sessionsApi.getSessions({}).then(setSessions);

  useEffect(() => {
    moviesApi.getMovies({ pageSize: 100 }).then((r) => setMovies(r.items));
    cinemasApi.getCinemas().then(setCinemas);
    loadSessions();
  }, []);

  const halls = cinemas.flatMap((c) => c.halls.map((h) => ({ ...h, cinemaName: c.name })));

  const startEditing = (session: SessionListItem) => {
    setForm({
      movieId: session.movieId,
      hallId: session.hallId,
      startTime: toDatetimeLocalValue(session.startTime),
      format: session.format,
      basePrice: session.basePrice
    });
    setEditingId(session.id);
    setError(null);
  };

  const cancelEditing = () => {
    setEditingId(null);
    setForm(emptyForm);
    setError(null);
  };

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setSubmitting(true);
    setError(null);
    try {
      if (editingId) {
        await sessionsApi.updateSession(editingId, {
          hallId: form.hallId,
          startTime: new Date(form.startTime).toISOString(),
          format: form.format,
          basePrice: form.basePrice
        });
      } else {
        await sessionsApi.createSession({
          movieId: form.movieId,
          hallId: form.hallId,
          startTime: new Date(form.startTime).toISOString(),
          format: form.format,
          basePrice: form.basePrice
        });
      }
      setForm(emptyForm);
      setEditingId(null);
      await loadSessions();
    } catch (err) {
      setError(extractApiErrorMessage(err, editingId ? "Не вдалося оновити сеанс." : "Не вдалося додати сеанс."));
    } finally {
      setSubmitting(false);
    }
  };

  const handleDelete = async (id: string) => {
    if (!confirm("Видалити сеанс?")) return;
    try {
      await sessionsApi.deleteSession(id);
      if (editingId === id) cancelEditing();
      await loadSessions();
    } catch (err) {
      alert(extractApiErrorMessage(err, "Не вдалося видалити сеанс (можливо, є підтверджені бронювання)."));
    }
  };

  return (
    <div className="mx-auto max-w-4xl px-6 py-10">
      <h1 className="marquee-heading text-3xl">Адмінка</h1>
      <div className="mt-4"><AdminNav /></div>

      <div className="mt-8 grid gap-10 sm:grid-cols-[1fr_320px]">
        <div>
          <h2 className="text-lg font-medium">Розклад ({sessions.length})</h2>
          <div className="mt-3 divide-y divide-charcoal-700">
            {sessions.map((s) => (
              <div key={s.id} className="flex items-center justify-between py-3">
                <div>
                  <p>{s.movieTitle} — {s.hallName}</p>
                  <p className="text-xs text-ink-muted">{new Date(s.startTime).toLocaleString("uk-UA")} · {s.format} · {s.basePrice} грн</p>
                </div>
                <div className="flex gap-3">
                  <button onClick={() => startEditing(s)} className="text-sm text-marquee hover:underline">Редагувати</button>
                  <button onClick={() => handleDelete(s.id)} className="text-sm text-accent hover:underline">Видалити</button>
                </div>
              </div>
            ))}
          </div>
        </div>

        <form onSubmit={handleSubmit} className="flex flex-col gap-3 rounded-sm border border-charcoal-700 p-4">
          <div className="flex items-center justify-between">
            <h2 className="text-lg font-medium">{editingId ? "Редагувати сеанс" : "Додати сеанс"}</h2>
            {editingId && (
              <button type="button" onClick={cancelEditing} className="text-xs text-ink-muted hover:text-ink">Скасувати</button>
            )}
          </div>

          <select required disabled={!!editingId} value={form.movieId} onChange={(e) => setForm({ ...form, movieId: e.target.value })}
            className="rounded-sm border border-charcoal-600 bg-charcoal-900 px-3 py-2 text-sm disabled:opacity-60">
            <option value="">Оберіть фільм</option>
            {movies.map((m) => <option key={m.id} value={m.id}>{m.title}</option>)}
          </select>
          {editingId && <p className="text-xs text-ink-muted">Фільм уже створеного сеансу змінити не можна — видаліть і створіть новий.</p>}

          <select required value={form.hallId} onChange={(e) => setForm({ ...form, hallId: e.target.value })}
            className="rounded-sm border border-charcoal-600 bg-charcoal-900 px-3 py-2 text-sm">
            <option value="">Оберіть зал</option>
            {halls.map((h) => <option key={h.id} value={h.id}>{h.cinemaName} — {h.name}</option>)}
          </select>

          <input type="datetime-local" required value={form.startTime}
            onChange={(e) => setForm({ ...form, startTime: e.target.value })}
            className="rounded-sm border border-charcoal-600 bg-charcoal-900 px-3 py-2 text-sm" />

          <div className="grid grid-cols-2 gap-2">
            <select value={form.format} onChange={(e) => setForm({ ...form, format: e.target.value })}
              className="rounded-sm border border-charcoal-600 bg-charcoal-900 px-3 py-2 text-sm">
              <option>2D</option><option>3D</option><option>IMAX</option><option>4DX</option>
            </select>
            <input type="number" required placeholder="Базова ціна" value={form.basePrice}
              onChange={(e) => setForm({ ...form, basePrice: Number(e.target.value) })}
              className="rounded-sm border border-charcoal-600 bg-charcoal-900 px-3 py-2 text-sm" />
          </div>

          {error && <p className="text-sm text-accent">{error}</p>}

          <button type="submit" disabled={submitting} className="mt-2 rounded-sm bg-accent py-2 font-medium hover:bg-accent-dim disabled:opacity-60">
            {submitting ? "Зберігаємо…" : editingId ? "Зберегти зміни" : "Додати сеанс"}
          </button>
        </form>
      </div>
    </div>
  );
}
