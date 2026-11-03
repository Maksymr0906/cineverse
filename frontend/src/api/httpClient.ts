import axios from "axios";
import type { AxiosError } from "axios";

/**
 * Єдиний axios-інстанс для звернень до CineVerse API.
 * withCredentials: true — обов'язково для HttpOnly refresh-token cookie (Етап 3).
 */
export const httpClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? "/api",
  withCredentials: true
});

let accessToken: string | null = null;

/** Викликається з authSlice після логіну/оновлення токена — тримаємо токен лише в пам'яті (не localStorage),
 * щоб зменшити поверхню атаки XSS. При перезавантаженні сторінки access-токен відновлюється через /auth/refresh. */
export function setAccessToken(token: string | null) {
  accessToken = token;
}

httpClient.interceptors.request.use((config) => {
  if (accessToken) {
    config.headers.Authorization = `Bearer ${accessToken}`;
  }
  return config;
});

let refreshPromise: Promise<string | null> | null = null;

httpClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as (typeof error.config & { _retry?: boolean }) | undefined;

    // 401 на /auth/* не повинен запускати нову спробу оновлення токена (уникаємо нескінченного циклу).
    if (error.response?.status === 401 && originalRequest && !originalRequest._retry && !originalRequest.url?.includes("/auth/")) {
      originalRequest._retry = true;

      refreshPromise ??= httpClient
        .post<{ accessToken: string }>("/auth/refresh")
        .then((res) => {
          setAccessToken(res.data.accessToken);
          return res.data.accessToken;
        })
        .catch(async () => {
          setAccessToken(null);
          // Виправлення після аудиту: раніше тут скидався лише токен у пам'яті axios-модуля,
          // а Redux-стан (authSlice) так і лишався "authenticated" — Navbar/ProtectedRoute
          // продовжували показувати користувача залогіненим. Динамічний імпорт (а не import
          // на верхньому рівні файлу) навмисно уникає циклічної залежності
          // store.ts → authSlice.ts → authApi.ts → httpClient.ts → store.ts.
          const { store } = await import("@/app/store");
          const { sessionExpired } = await import("@/features/auth/authSlice");
          store.dispatch(sessionExpired());
          return null;
        })
        .finally(() => {
          refreshPromise = null;
        });

      const newToken = await refreshPromise;
      if (newToken) {
        originalRequest.headers = originalRequest.headers ?? {};
        originalRequest.headers.Authorization = `Bearer ${newToken}`;
        return httpClient(originalRequest);
      }
    }

    return Promise.reject(error);
  }
);
