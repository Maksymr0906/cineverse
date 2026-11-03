import { Link, useNavigate } from "react-router-dom";
import { useAppDispatch, useAppSelector } from "@/hooks/redux";
import { logout } from "@/features/auth/authSlice";

/** Верхня навігація. Посилання на кабінет контролера/адмінку показуються лише відповідним ролям. */
export default function Navbar() {
  const { user, status } = useAppSelector((s) => s.auth);
  const dispatch = useAppDispatch();
  const navigate = useNavigate();

  // Виправлення після аудиту: раніше isAdmin (тоді "isStaff") включав CASHIER, хоча RoleRoute
  // на /admin/movies і /admin/sessions пускає лише ["ADMIN","MANAGER"] — касир бачив пункт
  // меню "Адмінка", клікав і отримував 403. Назва теж уточнена: цей прапорець саме про
  // керування каталогом/розкладом, а не про весь "персонал" узагалі.
  const isCatalogAdmin = user && ["ADMIN", "MANAGER"].includes(user.role);
  const isChecker = user && ["ADMIN", "CHECKER"].includes(user.role);

  const handleLogout = async () => {
    await dispatch(logout());
    navigate("/");
  };

  return (
    <header className="sticky top-0 z-30 border-b border-charcoal-700/60 bg-charcoal-950/90 backdrop-blur">
      <div className="mx-auto flex max-w-6xl items-center justify-between px-6 py-4">
        <Link to="/" className="marquee-heading text-3xl text-ink hover:text-accent transition-colors">
          CineVerse
        </Link>

        <nav className="flex items-center gap-6 text-sm text-ink-muted">
          <Link to="/" className="hover:text-ink transition-colors">Афіша</Link>
          {isChecker && <Link to="/checker" className="hover:text-ink transition-colors">Контролер</Link>}
          {isCatalogAdmin && <Link to="/admin/movies" className="hover:text-ink transition-colors">Адмінка</Link>}

          {status === "authenticated" && user ? (
            <div className="flex items-center gap-4">
              <Link to="/profile" className="hover:text-ink transition-colors">{user.firstName}</Link>
              <button onClick={handleLogout} className="text-ink-muted hover:text-accent transition-colors">
                Вийти
              </button>
            </div>
          ) : (
            <Link
              to="/login"
              className="rounded-sm bg-accent px-4 py-2 font-medium text-ink hover:bg-accent-dim transition-colors"
            >
              Увійти
            </Link>
          )}
        </nav>
      </div>
    </header>
  );
}
