import { Link } from "react-router-dom";
import type { MovieListItem } from "@/types";

/** Афішна картка фільму: постер домінує, мінімум чрому — без рамок/тіней у стилі типової SaaS-картки. */
export default function MovieCard({ movie }: { movie: MovieListItem }) {
  return (
    <Link to={`/movies/${movie.id}`} className="group block w-40 shrink-0 sm:w-48">
      <div className="relative aspect-[2/3] overflow-hidden rounded-sm bg-charcoal-800">
        {movie.posterUrl ? (
          <img
            src={movie.posterUrl}
            alt={movie.title}
            className="h-full w-full object-cover transition-transform duration-300 group-hover:scale-105"
            onError={(e) => (e.currentTarget.style.display = "none")}
          />
        ) : (
          <div className="flex h-full items-center justify-center text-ink-muted">Немає постера</div>
        )}
        {movie.status === "NOW_SHOWING" && (
          <span className="absolute left-2 top-2 rounded-sm bg-marquee px-2 py-0.5 text-xs font-semibold text-charcoal-950">
            У прокаті
          </span>
        )}
      </div>
      <h3 className="mt-2 line-clamp-1 text-sm font-medium text-ink group-hover:text-accent transition-colors">
        {movie.title}
      </h3>
      <p className="text-xs text-ink-muted">{movie.genres.slice(0, 2).join(", ") || movie.ageRating}</p>
    </Link>
  );
}
