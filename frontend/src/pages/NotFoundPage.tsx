import { Link } from "react-router-dom";

export default function NotFoundPage() {
  return (
    <div className="mx-auto max-w-md px-6 py-24 text-center">
      <p className="marquee-heading text-6xl text-accent">404</p>
      <p className="mt-3 text-ink-muted">Такої сторінки не існує.</p>
      <Link to="/" className="mt-6 inline-block text-marquee hover:underline">До афіші</Link>
    </div>
  );
}
