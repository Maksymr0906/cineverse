export type UserRole = "CUSTOMER" | "CASHIER" | "MANAGER" | "ADMIN" | "CHECKER";

export interface AuthUser {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  role: UserRole;
}

export interface MovieListItem {
  id: string;
  title: string;
  duration: number;
  releaseDate: string;
  ageRating: string;
  posterUrl: string;
  status: "COMING_SOON" | "NOW_SHOWING" | "ENDED";
  genres: string[];
}

export interface Actor {
  id: string;
  name: string;
  photoUrl: string | null;
  characterName: string;
}

export interface MovieDetails {
  id: string;
  title: string;
  description: string;
  duration: number;
  releaseDate: string;
  ageRating: string;
  posterUrl: string;
  trailerUrl: string;
  status: string;
  genres: string[];
  actors: Actor[];
  averageRating: number;
  reviewsCount: number;
}

export interface Genre {
  id: string;
  name: string;
}

export interface Hall {
  id: string;
  name: string;
  type: string;
  capacity: number;
}

export interface Cinema {
  id: string;
  name: string;
  address: string;
  city: string;
  phone: string;
  halls: Hall[];
}

export interface SessionListItem {
  id: string;
  movieId: string;
  movieTitle: string;
  posterUrl: string;
  hallId: string;
  hallName: string;
  cinemaId: string;
  cinemaName: string;
  startTime: string;
  endTime: string;
  format: string;
  basePrice: number;
}

export type SeatStatus = "FREE" | "LOCKED" | "SOLD";

export interface SeatMapSeat {
  seatId: string;
  rowNumber: number;
  seatNumber: number;
  seatType: "STANDARD" | "VIP" | "LOVE_SEAT" | "DISABLED";
  status: SeatStatus;
  price: number;
}

export interface SeatMap {
  sessionId: string;
  hallId: string;
  hallName: string;
  seats: SeatMapSeat[];
}

export interface BookingSeatResult {
  seatId: string;
  rowNumber: number;
  seatNumber: number;
  price: number;
}

export interface Booking {
  id: string;
  sessionId: string;
  status: "PENDING" | "CONFIRMED" | "CANCELLED" | "EXPIRED";
  totalPrice: number;
  expiresAt: string;
  seats: BookingSeatResult[];
  movieTitle: string;
  posterUrl: string;
  hallName: string;
  cinemaName: string;
  sessionStartTime: string;
  appliedPromoCode: string | null;
  discountAmount: number;
}

export interface Ticket {
  id: string;
  rowNumber: number;
  seatNumber: number;
  ticketCode: string;
  qrCodeBase64: string;
  status: "ACTIVE" | "USED" | "CANCELLED";
}

export interface TicketPreview {
  ticketId: string;
  ticketCode: string;
  status: string;
  movieTitle: string;
  hallName: string;
  sessionStartTime: string;
  rowNumber: number;
  seatNumber: number;
  holderFullName: string;
  validatedAt: string | null;
}

export interface TicketValidationResult {
  ticketId: string;
  ticketCode: string;
  validatedAt: string;
  seatLabel: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

export interface ApiError {
  status: number;
  title: string;
  errors?: Record<string, string[]>;
}
