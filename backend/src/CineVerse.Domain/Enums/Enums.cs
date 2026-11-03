namespace CineVerse.Domain.Enums;

public enum UserRole { CUSTOMER, CASHIER, MANAGER, ADMIN, CHECKER }

public enum MovieStatus { COMING_SOON, NOW_SHOWING, ENDED }

public enum HallType { STANDARD, IMAX, VIP, THREE_D, FOUR_DX }

public enum SeatType { STANDARD, VIP, LOVE_SEAT, DISABLED }

public enum SeatStatus { FREE, LOCKED, SOLD }

public enum BookingStatus { PENDING, CONFIRMED, CANCELLED, EXPIRED }

public enum TicketStatus { ACTIVE, USED, CANCELLED }

public enum PaymentMethod { CARD, CASH, ONLINE }

public enum PaymentStatus { PENDING, SUCCESS, FAILED }

public enum DiscountType { PERCENT, FIXED }
