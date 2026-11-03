import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { moviesApi } from "@/api/moviesApi";
import MovieCard from "@/components/MovieCard";
import type { MovieListItem } from "@/types";

/**
 * Головна сторінка (п.2.1 ТЗ): афішний "hero" з найпомітнішою прем'єрою замість
 * типової сітки карток одразу зверху, далі — ряди "У прокаті" / "Скоро у прокаті".
 */
export default function HomePage() {
  const [nowShowing, setNowShowing] = useState<MovieListItem[]>([]);
  const [comingSoon, setComingSoon] = useState<MovieListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    Promise.all([
      moviesApi.getMovies({ status: "NOW_SHOWING", pageSize: 12 }),
      moviesApi.getMovies({ status: "COMING_SOON", pageSize: 12 })
    ])
      .then(([now, soon]) => {
        setNowShowing(now.items);
        setComingSoon(soon.items);
      })
      .catch(() => setError("Не вдалося завантажити афішу. Перевірте, чи запущено бекенд."))
      .finally(() => setLoading(false));
  }, []);

  const hero = nowShowing[0];

  if (loading) {
    return <div className="p-10 text-center text-ink-muted">Завантажуємо афішу…</div>;
  }

  if (error) {
    return <div className="p-10 text-center text-accent">{error}</div>;
  }

  return (
    <div className="pb-16">
      {hero && (
        <section className="relative mb-12 overflow-hidden">
          <div className="relative h-[52vh] min-h-[360px] w-full">
            {hero.posterUrl && (
              <img src={hero.posterUrl} alt="" className="h-full w-full object-cover opacity-30" />
            )}
            <div className="absolute inset-0 bg-gradient-to-t from-charcoal-950 via-charcoal-950/60 to-transparent" />
            <div className="absolute inset-x-0 bottom-0 mx-auto max-w-6xl px-6 pb-10">
              <p className="text-xs uppercase tracking-[0.3em] text-marquee">Зараз у прокаті</p>
              <h1 className="marquee-heading mt-2 text-6xl leading-none text-ink sm:text-7xl">{hero.title}</h1>
              <p className="mt-3 max-w-lg text-sm text-ink-muted">
                {hero.genres.join(" · ")} {hero.genres.length > 0 && "·"} {hero.duration} хв · {hero.ageRating}
              </p>
              <Link
                to={`/movies/${hero.id}`}
                className="mt-5 inline-block rounded-sm bg-accent px-6 py-3 font-medium text-ink hover:bg-accent-dim transition-colors"
              >
                Обрати сеанс
              </Link>
            </div>
          </div>
        </section>
      )}

      <div className="mx-auto max-w-6xl px-6">
        <MovieRow title="У прокаті" movies={nowShowing} />
        <MovieRow title="Скоро у прокаті" movies={comingSoon} />
      </div>
    </div>
  );
}

function MovieRow({ title, movies }: { title: string; movies: MovieListItem[] }) {
  if (movies.length === 0) return null;
  return (
    <section className="mb-10">
      <h2 className="marquee-heading mb-4 text-2xl text-ink">{title}</h2>
      <div className="flex gap-4 overflow-x-auto pb-2">
        {movies.map((m) => (
          <MovieCard key={m.id} movie={m} />
        ))}
      </div>
    </section>
  );
}
