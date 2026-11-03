import { httpClient } from "./httpClient";
import type { Booking, SeatMap, Ticket } from "@/types";

export const bookingsApi = {
  getSeatMap: (sessionId: string) => httpClient.get<SeatMap>(`/bookings/seat-map/${sessionId}`).then((r) => r.data),

  createBooking: (sessionId: string, seatIds: string[], promoCode?: string) =>
    httpClient.post<Booking>("/bookings", { sessionId, seatIds, promoCode: promoCode || undefined }).then((r) => r.data),

  getBookingById: (id: string) => httpClient.get<Booking>(`/bookings/${id}`).then((r) => r.data),

  cancelBooking: (id: string) => httpClient.post(`/bookings/${id}/cancel`),

  getMyBookings: () => httpClient.get<Booking[]>("/bookings/mine").then((r) => r.data),

  getTickets: (bookingId: string) => httpClient.get<Ticket[]>(`/bookings/${bookingId}/tickets`).then((r) => r.data)
};
