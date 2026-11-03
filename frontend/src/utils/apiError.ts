import axios from "axios";
import type { ApiError } from "@/types";

/**
 * Наш ExceptionHandlingMiddleware (бекенд, Етап 3) завжди повертає JSON виду
 * { status, title, errors? }. Ця утиліта дістає з нього людяне повідомлення
 * для показу в UI, з розумним fallback для мережевих/неочікуваних помилок.
 */
export function extractApiErrorMessage(error: unknown, fallback = "Щось пішло не так. Спробуйте ще раз."): string {
  if (axios.isAxiosError<ApiError>(error)) {
    const data = error.response?.data;
    if (data?.errors) {
      const firstField = Object.values(data.errors)[0];
      if (firstField?.length) return firstField[0];
    }
    if (data?.title) return data.title;
    if (error.code === "ERR_NETWORK") return "Немає з'єднання з сервером. Перевірте підключення.";
  }
  return fallback;
}
