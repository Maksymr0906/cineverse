import { Outlet } from "react-router-dom";
import { useAppSelector } from "@/hooks/redux";
import type { UserRole } from "@/types";

/** Додатково до ProtectedRoute — пускає лише перелічені ролі, інакше показує 403. */
export default function RoleRoute({ allowedRoles }: { allowedRoles: UserRole[] }) {
  const { user } = useAppSelector((s) => s.auth);

  if (!user || !allowedRoles.includes(user.role)) {
    return (
      <div className="mx-auto max-w-md p-10 text-center">
        <p className="marquee-heading text-4xl text-accent">403</p>
        <p className="mt-2 text-ink-muted">У вас немає доступу до цієї сторінки.</p>
      </div>
    );
  }

  return <Outlet />;
}
