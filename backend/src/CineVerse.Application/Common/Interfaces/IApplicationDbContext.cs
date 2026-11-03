using CineVerse.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CineVerse.Application.Common.Interfaces;

/// <summary>
/// Абстракція над EF Core DbContext, щоб Application-шар не залежав напряму
/// від Infrastructure (дотримання правил Clean Architecture).
/// </summary>
public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Movie> Movies { get; }
    DbSet<Genre> Genres { get; }
    DbSet<Actor> Actors { get; }
    DbSet<Cinema> Cinemas { get; }
    DbSet<Hall> Halls { get; }
    DbSet<Seat> Seats { get; }
    DbSet<Session> Sessions { get; }
    DbSet<Booking> Bookings { get; }
    DbSet<BookingSeat> BookingSeats { get; }
    DbSet<Ticket> Tickets { get; }
    DbSet<Payment> Payments { get; }
    DbSet<Promotion> Promotions { get; }
    DbSet<Review> Reviews { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
