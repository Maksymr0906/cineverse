import { httpClient } from "./httpClient";
import type { Genre } from "@/types";

export const genresApi = {
  getGenres: () => httpClient.get<Genre[]>("/genres").then((r) => r.data),
  createGenre: (name: string) => httpClient.post("/genres", { name })
};
