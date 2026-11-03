import { httpClient } from "./httpClient";
import type { SessionListItem } from "@/types";

export const sessionsApi = {
  getSessions: (params: { movieId?: string; cinemaId?: string; date?: string }) =>
    httpClient.get<SessionListItem[]>("/sessions", { params }).then((r) => r.data),
  createSession: (data: unknown) => httpClient.post("/sessions", data),
  updateSession: (id: string, data: unknown) => httpClient.put(`/sessions/${id}`, data),
  deleteSession: (id: string) => httpClient.delete(`/sessions/${id}`)
};
