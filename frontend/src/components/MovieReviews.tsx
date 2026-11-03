import { useEffect, useState } from "react";
import type { FormEvent } from "react";
import { reviewsApi } from "@/api/reviewsApi";
import type { Review } from "@/api/reviewsApi";
import { useAppSelector } from "@/hooks/redux";
import { extractApiErrorMessage } from "@/utils/apiError";

/**
 * Реалізовано після аудиту: раніше сторінка фільму показувала середній рейтинг і кількість
 * відгуків, не даючи жодного способу відгук залишити. Право писати відгук перевіряється
 * на бекенді (потрібне підтверджене бронювання на цей фільм) — тут лише показуємо зрозумілу
 * помилку, якщо користувач цієї умови не виконав, а не ховаємо форму заздалегідь (бекенд
 * усе одно лишається джерелом істини для цього правила).
 */
export default function MovieReviews({ movieId }: { movieId: string }) {
  const { status: authStatus } = useAppSelector((s) => s.auth);
  const [reviews, setReviews] = useState<Review[]>([]);
  const [loading, setLoading] = useState(true);
  const [rating, setRating] = useState(5);
  const [comment, setComment] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [submitted, setSubmitted] = useState(false);

  useEffect(() => {
    reviewsApi
      .getReviews(movieId)
      .then(setReviews)
      .catch(() => setError("Не вдалося завантажити відгуки."))
      .finally(() => setLoading(false));
  }, [movieId]);

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setSubmitting(true);
    setError(null);
    try {
      await reviewsApi.createReview(movieId, rating, comment);
      setSubmitted(true);
      setComment("");
      const updated = await reviewsApi.getReviews(movieId);
      setReviews(updated);
    } catch (err) {
      setError(extractApiErrorMessage(err, "Не вдалося зберегти відгук."));
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <section className="mt-12">
      <h2 className="marquee-heading text-2xl">Відгуки</h2>

      {loading ? (
        <p className="mt-4 text-ink-muted">Завантаження…</p>
      ) : reviews.length === 0 ? (
        <p className="mt-4 text-ink-muted">Поки що немає відгуків на цей фільм.</p>
      ) : (
        <div className="mt-4 space-y-4">
          {reviews.map((r) => (
            <div key={r.id} className="border-b border-charcoal-700 pb-4">
              <p className="text-sm">
                <span className="font-medium">{r.authorFirstName}</span>{" "}
                <span className="text-marquee">{"★".repeat(r.rating)}{"☆".repeat(5 - r.rating)}</span>
                {r.isVerified && <span className="ml-2 text-xs text-ink-muted">Підтверджений перегляд</span>}
              </p>
              {r.comment && <p className="mt-1 text-sm text-ink-muted">{r.comment}</p>}
            </div>
          ))}
        </div>
      )}

      {authStatus === "authenticated" && !submitted && (
        <form onSubmit={handleSubmit} className="mt-6 flex flex-col gap-3 rounded-sm border border-charcoal-700 p-4">
          <p className="text-sm text-ink-muted">Залишити відгук (доступно, якщо ви купували квиток на цей фільм)</p>
          <div className="flex gap-1">
            {[1, 2, 3, 4, 5].map((n) => (
              <button
                type="button"
                key={n}
                onClick={() => setRating(n)}
                className={`text-2xl ${n <= rating ? "text-marquee" : "text-charcoal-600"}`}
                aria-label={`${n} з 5`}
              >
                ★
              </button>
            ))}
          </div>
          <textarea
            value={comment}
            onChange={(e) => setComment(e.target.value)}
            placeholder="Ваші враження від фільму (необов'язково)"
            rows={3}
            className="rounded-sm border border-charcoal-600 bg-charcoal-900 px-3 py-2 text-sm"
          />
          {error && <p className="text-sm text-accent">{error}</p>}
          <button
            type="submit"
            disabled={submitting}
            className="self-start rounded-sm bg-accent px-4 py-2 text-sm font-medium hover:bg-accent-dim disabled:opacity-60"
          >
            {submitting ? "Зберігаємо…" : "Залишити відгук"}
          </button>
        </form>
      )}

      {submitted && <p className="mt-4 text-sm text-marquee">Дякуємо за відгук!</p>}
    </section>
  );
}
