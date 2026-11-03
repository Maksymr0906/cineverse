import { useEffect, useRef, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { authApi } from "@/api/authApi";
import { extractApiErrorMessage } from "@/utils/apiError";

export default function ConfirmEmailPage() {
  const [searchParams] = useSearchParams();
  const token = searchParams.get("token");
  const [state, setState] = useState<"loading" | "success" | "error">("loading");
  const [message, setMessage] = useState("");

  // Захист від подвійного виклику в React.StrictMode (dev навмисно монтує ефекти двічі).
  // Токен підтвердження — одноразовий: бекенд обнуляє його після першого успішного запиту
  // й повертає 404 при повторному, тож без цього прапорця другий виклик міг показати
  // користувачу помилку, хоча email насправді вже підтверджено першим запитом.
  const requestSentRef = useRef(false);

  useEffect(() => {
    if (!token) {
      setState("error");
      setMessage("Посилання недійсне: відсутній токен підтвердження.");
      return;
    }

    if (requestSentRef.current) return;
    requestSentRef.current = true;

    authApi
      .confirmEmail(token)
      .then(() => setState("success"))
      .catch((err) => {
        setState("error");
        setMessage(extractApiErrorMessage(err, "Не вдалося підтвердити email."));
      });
  }, [token]);

  return (
    <div className="mx-auto max-w-sm px-6 py-20 text-center">
      {state === "loading" && <p className="text-ink-muted">Підтверджуємо email…</p>}

      {state === "success" && (
        <>
          <h1 className="marquee-heading text-3xl text-marquee">Готово!</h1>
          <p className="mt-3 text-ink-muted">Email підтверджено. Тепер можна увійти в систему.</p>
          <Link to="/login" className="mt-6 inline-block rounded-sm bg-accent px-5 py-2.5 font-medium hover:bg-accent-dim">
            Увійти
          </Link>
        </>
      )}

      {state === "error" && (
        <>
          <h1 className="marquee-heading text-3xl text-accent">Не вдалося</h1>
          <p className="mt-3 text-ink-muted">{message}</p>
          <Link to="/register" className="mt-6 inline-block text-marquee hover:underline">До реєстрації</Link>
        </>
      )}
    </div>
  );
}
