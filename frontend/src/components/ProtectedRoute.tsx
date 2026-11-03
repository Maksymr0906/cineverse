import { Navigate, Outlet, useLocation } from "react-router-dom";
import { useAppSelector } from "@/hooks/redux";

/** Пускає далі лише автентифікованих користувачів; інакше — на /login із поверненням після входу. */
export default function ProtectedRoute() {
  const { status } = useAppSelector((s) => s.auth);
  const location = useLocation();

  if (status === "loading" || status === "idle") {
    return <div className="p-10 text-center text-ink-muted">Завантаження…</div>;
  }

  if (status !== "authenticated") {
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  return <Outlet />;
}
