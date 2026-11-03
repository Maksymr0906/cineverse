import { httpClient } from "./httpClient";
import type { MovieDetails, MovieListItem, PagedResult } from "@/types";

export const moviesApi = {
  getMovies: (params: { status?: string; genreId?: string; search?: string; pageNumber?: number; pageSize?: number }) =>
    httpClient.get<PagedResult<MovieListItem>>("/movies", { params }).then((r) => r.data),

  getMovieById: (id: string) => httpClient.get<MovieDetails>(`/movies/${id}`).then((r) => r.data),

  createMovie: (data: unknown) => httpClient.post("/movies", data),
  updateMovie: (id: string, data: unknown) => httpClient.put(`/movies/${id}`, data),
  deleteMovie: (id: string) => httpClient.delete(`/movies/${id}`)
};
