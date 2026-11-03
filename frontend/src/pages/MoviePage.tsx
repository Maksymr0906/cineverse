import { useEffect, useMemo, useState } from "react";
import { toLocalDateString } from "@/utils/date";
import { Link, useNavigate, useParams } from "react-router-dom";
import { moviesApi } from "@/api/moviesApi";
import { sessionsApi } from "@/api/sessionsApi";
import { cinemasApi } from "@/api/cinemasApi";
import MovieReviews from "@/components/MovieReviews";
import { toYoutubeEmbedUrl } from "@/utils/youtube";
import type { Cinema, MovieDetails, SessionListItem } from "@/types";

function formatDateLabel(iso: string) {
  const date = new Date(iso);
  return date.toLocaleDateString("uk-UA", { weekday: "short", day: "numeric", month: "short" });
}

export default function MoviePage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [movie, setMovie] = useState<MovieDetails | null>(null);
  const [sessions, setSessions] = useState<SessionListItem[]>([]);
  const [cinemas, setCinemas] = useState<Cinema[]>([]);
  const [selectedCinemaId, setSelectedCinemaId] = useState<string>("");
  const [selectedDate, setSelectedDate] = useState<string>(toLocalDateString(new Date()));
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!id) return;
    setLoading(true);
    Promise.all([moviesApi.getMovieById(id), cinemasApi.getCinemas()])
      .then(([m, c]) => {
        setMovie(m);
        setCinemas(c);
      })
      .finally(() => setLoading(false));
  }, [id]);

  useEffect(() => {
    if (!id) return;
    sessionsApi
      .getSessions({ movieId: id, cinemaId: selectedCinemaId || undefined, date: selectedDate })
      .then(setSessions);
  }, [id, selectedCinemaId, selectedDate]);

  const dateOptions = useMemo(() => {
    return Array.from({ length: 7 }, (_, i) => {
      const d = new Date();
      d.setDate(d.getDate() + i);
      return toLocalDateString(d);
    });
  }, []);

  if (loading) return <div className="p-10 text-center text-ink-muted">Завантаження…</div>;
  if (!movie) return <div className="p-10 text-center text-accent">Фільм не знайдено.</div>;

  return (
    <div className="mx-auto max-w-5xl px-6 py-10">
      <div className="grid gap-8 sm:grid-cols-[240px_1fr]">
        <img src={movie.posterUrl} alt={movie.title} className="w-full rounded-sm bg-charcoal-800 object-cover" />

        <div>
          <h1 className="marquee-heading text-5xl">{movie.title}</h1>
          <p className="mt-2 text-sm text-ink-muted">
            {movie.genres.join(", ")} · {movie.duration} хв · {movie.ageRating}
            {movie.reviewsCount > 0 && ` · ★ ${movie.averageRating.toFixed(1)} (${movie.reviewsCount})`}
          </p>
          <p className="mt-4 max-w-2xl text-ink-muted">{movie.description}</p>

          {movie.actors.length > 0 && (
            <p className="mt-4 text-sm text-ink-muted">
              <span className="text-ink">У ролях: </span>
              {movie.actors.map((a) => a.name).join(", ")}
            </p>
          )}

          {movie.trailerUrl && (
            (() => {
              const embedUrl = toYoutubeEmbedUrl(movie.trailerUrl);
              return embedUrl ? (
                <div className="mt-6 aspect-video max-w-2xl overflow-hidden rounded-sm bg-charcoal-800">
                  <iframe
                    src={embedUrl}
                    title={`Трейлер: ${movie.title}`}
                    className="h-full w-full"
                    allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture"
                    allowFullScreen
                  />
                </div>
              ) : (
                <a href={movie.trailerUrl} target="_blank" rel="noreferrer" className="mt-4 inline-block text-sm text-marquee hover:underline">
                  Дивитись трейлер ↗
                </a>
              );
            })()
          )}
        </div>
      </div>

      <section className="mt-12">
        <h2 className="marquee-heading text-2xl">Сеанси</h2>

        <div className="mt-4 flex flex-wrap gap-2">
          {dateOptions.map((d) => (
            <button
              key={d}
              onClick={() => setSelectedDate(d)}
              className={`rounded-sm px-3 py-1.5 text-sm transition-colors ${
                d === selectedDate ? "bg-marquee text-charcoal-950" : "border border-charcoal-600 text-ink-muted hover:text-ink"
              }`}
            >
              {formatDateLabel(d)}
            </button>
          ))}
        </div>

        <select
          value={selectedCinemaId}
          onChange={(e) => setSelectedCinemaId(e.target.value)}
          className="mt-4 rounded-sm border border-charcoal-600 bg-charcoal-900 px-3 py-2 text-sm"
        >
          <option value="">Усі кінотеатри</option>
          {cinemas.map((c) => (
            <option key={c.id} value={c.id}>{c.name} — {c.city}</option>
          ))}
        </select>

        {sessions.length === 0 ? (
          <p className="mt-6 text-ink-muted">На обрану дату сеансів немає. Оберіть інший день.</p>
        ) : (
          <div className="mt-6 space-y-4">
            {Object.entries(groupByCinema(sessions)).map(([cinemaName, group]) => (
              <div key={cinemaName}>
                <p className="text-sm text-ink-muted">{cinemaName}</p>
                <div className="mt-2 flex flex-wrap gap-2">
                  {group.map((s) => (
                    <button
                      key={s.id}
                      onClick={() => navigate(`/sessions/${s.id}/seats`)}
                      className="rounded-sm border border-charcoal-600 px-4 py-2 text-sm hover:border-marquee hover:text-marquee transition-colors"
                    >
                      {new Date(s.startTime).toLocaleTimeString("uk-UA", { hour: "2-digit", minute: "2-digit" })}
                      <span className="ml-2 text-xs text-ink-muted">{s.format} · {s.basePrice} грн</span>
                    </button>
                  ))}
                </div>
              </div>
            ))}
          </div>
        )}
      </section>

      {id && <MovieReviews movieId={id} />}

      <Link to="/" className="mt-10 inline-block text-sm text-ink-muted hover:text-ink">← До афіші</Link>
    </div>
  );
}

function groupByCinema(sessions: SessionListItem[]): Record<string, SessionListItem[]> {
  return sessions.reduce<Record<string, SessionListItem[]>>((acc, s) => {
    (acc[s.cinemaName] ??= []).push(s);
    return acc;
  }, {});
}
