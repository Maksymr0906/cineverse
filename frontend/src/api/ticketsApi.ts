import { httpClient } from "./httpClient";
import type { TicketPreview, TicketValidationResult } from "@/types";

export const checkerApi = {
  getTicketByCode: (code: string) => httpClient.get<TicketPreview>(`/tickets/${encodeURIComponent(code)}`).then((r) => r.data),

  // Виправлення після аудиту: раніше без generic-параметра (неявний `any`) — result.seatLabel
  // у CheckerPage проходив компіляцію без жодної статичної перевірки на існування поля.
  validateTicket: (ticketCode: string) =>
    httpClient.post<TicketValidationResult>("/tickets/validate", { ticketCode }).then((r) => r.data)
};
