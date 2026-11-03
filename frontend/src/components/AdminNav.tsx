import { NavLink } from "react-router-dom";
import { useAppSelector } from "@/hooks/redux";

const linkClass = ({ isActive }: { isActive: boolean }) =>
  `px-3 py-1.5 text-sm rounded-sm transition-colors ${isActive ? "bg-marquee text-charcoal-950" : "text-ink-muted hover:text-ink"}`;

export default function AdminNav() {
  // "Кінотеатри" веде на маршрут, захищений лише для ADMIN (бекенд-політика AdminOnly,
  // на відміну від StaffOnly решти вкладок) — ховаємо посилання для MANAGER, а не показуємо
  // пункт меню, який однаково впаде в 403.
  const { user } = useAppSelector((s) => s.auth);
  const isAdmin = user?.role === "ADMIN";

  return (
    <nav className="flex gap-2 border-b border-charcoal-700 pb-4">
      <NavLink to="/admin/movies" className={linkClass}>Фільми</NavLink>
      <NavLink to="/admin/sessions" className={linkClass}>Сеанси</NavLink>
      {isAdmin && <NavLink to="/admin/cinemas" className={linkClass}>Кінотеатри</NavLink>}
      <NavLink to="/admin/promotions" className={linkClass}>Промокоди</NavLink>
      <NavLink to="/admin/analytics" className={linkClass}>Аналітика</NavLink>
    </nav>
  );
}
