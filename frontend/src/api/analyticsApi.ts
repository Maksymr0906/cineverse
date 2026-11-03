import { httpClient } from "./httpClient";

export interface RevenueByDay {
  date: string;
  revenue: number;
  bookingsCount: number;
}

export interface SessionOccupancy {
  sessionId: string;
  movieTitle: string;
  hallName: string;
  cinemaName: string;
  startTime: string;
  capacity: number;
  soldSeats: number;
  occupancyPercent: number;
}

export const analyticsApi = {
  getRevenueByDay: (days = 14) => httpClient.get<RevenueByDay[]>("/analytics/revenue-by-day", { params: { days } }).then((r) => r.data),
  getSessionOccupancy: (upcomingDays = 7) =>
    httpClient.get<SessionOccupancy[]>("/analytics/session-occupancy", { params: { upcomingDays } }).then((r) => r.data)
};
