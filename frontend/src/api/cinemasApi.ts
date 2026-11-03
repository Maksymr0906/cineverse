import { httpClient } from "./httpClient";
import type { Cinema } from "@/types";

export const cinemasApi = {
  getCinemas: (city?: string) => httpClient.get<Cinema[]>("/cinemas", { params: { city } }).then((r) => r.data),
  createCinema: (data: unknown) => httpClient.post("/cinemas", data),
  createHall: (data: unknown) => httpClient.post("/cinemas/halls", data),
  // Лише назва й тип — розсадку існуючого залу свідомо не можна редагувати цим шляхом
  // (див. коментар у backend-команді UpdateHallCommand).
  updateHall: (id: string, data: { name: string; type: string }) =>
    httpClient.put(`/cinemas/halls/${id}`, { hallId: id, ...data })
};
