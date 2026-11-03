import { useEffect, useState } from "react";
import { toLocalDateString } from "@/utils/date";
import type { FormEvent } from "react";
import { moviesApi } from "@/api/moviesApi";
import { genresApi } from "@/api/genresApi";
import AdminNav from "@/components/AdminNav";
import { extractApiErrorMessage } from "@/utils/apiError";
import type { Genre, MovieListItem } from "@/types";

const emptyForm = {
  title: "", description: "", duration: 120, releaseDate: toLocalDateString(new Date()),
  ageRating: "12+", posterUrl: "", trailerUrl: "", status: "COMING_SOON", genreIds: [] as string[]
};

/** Керування каталогом фільмів (кабінет адміністратора, п.2.5 ТЗ). Одна форма для створення
 * й редагування (moviesApi.updateMovie раніше існував в api-шарі, але жодна сторінка його
 * не викликала — виправлення після аудиту). */
export default function AdminMoviesPage() {
  const [movies, setMovies] = useState<MovieListItem[]>([]);
  const [genres, setGenres] = useState<Genre[]>([]);
  const [form, setForm] = useState(emptyForm);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [loadingForEdit, setLoadingForEdit] = useState(false);

  const loadMovies = () => moviesApi.getMovies({ pageSize: 50 }).then((r) => setMovies(r.items));

  useEffect(() => {
    loadMovies();
    genresApi.getGenres().then(setGenres);
  }, []);

  const toggleGenre = (id: string) =>
    setForm((f) => ({ ...f, genreIds: f.genreIds.includes(id) ? f.genreIds.filter((g) => g !== id) : [...f.genreIds, id] }));

  const startEditing = async (movieId: string) => {
    setLoadingForEdit(true);
    setError(null);
    try {
      const movie = await moviesApi.getMovieById(movieId);
      // GetMovieById повертає жанри як НАЗВИ (movie.genres: string[]), а форма оперує ID-шниками —
      // зіставляємо за вже завантаженим довідником genres.
      const genreIds = genres.filter((g) => movie.genres.includes(g.name)).map((g) => g.id);

      setForm({
        title: movie.title,
        description: movie.description,
        duration: movie.duration,
        releaseDate: movie.releaseDate,
        ageRating: movie.ageRating,
        posterUrl: movie.posterUrl,
        trailerUrl: movie.trailerUrl,
        status: movie.status,
        genreIds
      });
      setEditingId(movieId);
    } catch (err) {
      setError(extractApiErrorMessage(err, "Не вдалося завантажити фільм для редагування."));
    } finally {
      setLoadingForEdit(false);
    }
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
        await moviesApi.updateMovie(editingId, form);
      } else {
        await moviesApi.createMovie({ ...form, actors: [] });
      }
      setForm(emptyForm);
      setEditingId(null);
      await loadMovies();
    } catch (err) {
      setError(extractApiErrorMessage(err, editingId ? "Не вдалося оновити фільм." : "Не вдалося створити фільм."));
    } finally {
      setSubmitting(false);
    }
  };

  const handleDelete = async (id: string) => {
    if (!confirm("Видалити фільм? Це неможливо, якщо на нього вже є сеанси.")) return;
    try {
      await moviesApi.deleteMovie(id);
      if (editingId === id) cancelEditing();
      await loadMovies();
    } catch (err) {
      alert(extractApiErrorMessage(err, "Не вдалося видалити фільм."));
    }
  };

  return (
    <div className="mx-auto max-w-4xl px-6 py-10">
      <h1 className="marquee-heading text-3xl">Адмінка</h1>
      <div className="mt-4"><AdminNav /></div>

      <div className="mt-8 grid gap-10 sm:grid-cols-[1fr_320px]">
        <div>
          <h2 className="text-lg font-medium">Каталог ({movies.length})</h2>
          <div className="mt-3 divide-y divide-charcoal-700">
            {movies.map((m) => (
              <div key={m.id} className="flex items-center justify-between py-3">
                <div>
                  <p>{m.title}</p>
                  <p className="text-xs text-ink-muted">{m.status} · {m.genres.join(", ")}</p>
                </div>
                <div className="flex gap-3">
                  <button onClick={() => startEditing(m.id)} disabled={loadingForEdit} className="text-sm text-marquee hover:underline disabled:opacity-60">
                    Редагувати
                  </button>
                  <button onClick={() => handleDelete(m.id)} className="text-sm text-accent hover:underline">Видалити</button>
                </div>
              </div>
            ))}
          </div>
        </div>

        <form onSubmit={handleSubmit} className="flex flex-col gap-3 rounded-sm border border-charcoal-700 p-4">
          <div className="flex items-center justify-between">
            <h2 className="text-lg font-medium">{editingId ? "Редагувати фільм" : "Додати фільм"}</h2>
            {editingId && (
              <button type="button" onClick={cancelEditing} className="text-xs text-ink-muted hover:text-ink">Скасувати</button>
            )}
          </div>
          <input required placeholder="Назва" value={form.title} onChange={(e) => setForm({ ...form, title: e.target.value })}
            className="rounded-sm border border-charcoal-600 bg-charcoal-900 px-3 py-2 text-sm" />
          <textarea placeholder="Опис" value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })}
            className="rounded-sm border border-charcoal-600 bg-charcoal-900 px-3 py-2 text-sm" rows={3} />
          <div className="grid grid-cols-2 gap-2">
            <input type="number" required placeholder="Тривалість, хв" value={form.duration}
              onChange={(e) => setForm({ ...form, duration: Number(e.target.value) })}
              className="rounded-sm border border-charcoal-600 bg-charcoal-900 px-3 py-2 text-sm" />
            <input placeholder="Віковий рейтинг" value={form.ageRating} onChange={(e) => setForm({ ...form, ageRating: e.target.value })}
              className="rounded-sm border border-charcoal-600 bg-charcoal-900 px-3 py-2 text-sm" />
          </div>
          <input type="date" value={form.releaseDate} onChange={(e) => setForm({ ...form, releaseDate: e.target.value })}
            className="rounded-sm border border-charcoal-600 bg-charcoal-900 px-3 py-2 text-sm" />
          <input placeholder="URL постера" value={form.posterUrl} onChange={(e) => setForm({ ...form, posterUrl: e.target.value })}
            className="rounded-sm border border-charcoal-600 bg-charcoal-900 px-3 py-2 text-sm" />
          <select value={form.status} onChange={(e) => setForm({ ...form, status: e.target.value })}
            className="rounded-sm border border-charcoal-600 bg-charcoal-900 px-3 py-2 text-sm">
            <option value="COMING_SOON">COMING_SOON</option>
            <option value="NOW_SHOWING">NOW_SHOWING</option>
            <option value="ENDED">ENDED</option>
          </select>

          <div>
            <p className="mb-1 text-xs text-ink-muted">Жанри <span className="text-accent">*</span> (мінімум один)</p>
            <div className="flex flex-wrap gap-1.5">
              {genres.map((g) => (
                <button type="button" key={g.id} onClick={() => toggleGenre(g.id)}
                  className={`rounded-sm px-2 py-1 text-xs ${form.genreIds.includes(g.id) ? "bg-marquee text-charcoal-950" : "border border-charcoal-600 text-ink-muted"}`}>
                  {g.name}
                </button>
              ))}
            </div>
          </div>

          {error && <p className="text-sm text-accent">{error}</p>}

          <button type="submit" disabled={submitting || form.genreIds.length === 0} className="mt-2 rounded-sm bg-accent py-2 font-medium hover:bg-accent-dim disabled:opacity-60">
            {submitting ? "Зберігаємо…" : editingId ? "Зберегти зміни" : "Додати фільм"}
          </button>
        </form>
      </div>
    </div>
  );
}
