using CineVerse.Domain.Entities;
using CineVerse.Domain.Enums;
using CineVerse.Infrastructure.Persistence;

namespace CineVerse.UnitTests.Common;

/// <summary>Мінімальний набір пов'язаних сутностей (кінотеатр → зал → місця → фільм → сеанс),
/// який повторно використовується практично в кожному тесті хендлерів бронювання/квитків.</summary>
public static class TestDataBuilder
{
    public static (Session Session, List<Seat> Seats) SeedSessionWithSeats(
        ApplicationDbContext context,
        DateTimeOffset startTime,
        int movieDurationMinutes = 120,
        int seatCount = 4,
        decimal basePrice = 150m)
    {
        var cinema = new Cinema { Name = "Test Cinema", City = "Тестове", Address = "вул. Тестова, 1", Phone = "+380000000000" };
        var hall = new Hall { CinemaId = cinema.Id, Cinema = cinema, Name = "Зал 1", Type = HallType.STANDARD, Capacity = seatCount };

        var seats = new List<Seat>();
        for (var i = 1; i <= seatCount; i++)
        {
            seats.Add(new Seat { HallId = hall.Id, Hall = hall, RowNumber = 1, SeatNumber = i, Type = SeatType.STANDARD });
        }

        var movie = new Movie
        {
            Title = "Test Movie",
            Duration = movieDurationMinutes,
            ReleaseDate = DateOnly.FromDateTime(DateTime.UtcNow),
            AgeRating = "12+",
            Status = MovieStatus.NOW_SHOWING
        };

        var session = new Session
        {
            MovieId = movie.Id,
            Movie = movie,
            HallId = hall.Id,
            Hall = hall,
            StartTime = startTime,
            EndTime = startTime.AddMinutes(movieDurationMinutes + 20),
            Format = "2D",
            BasePrice = basePrice
        };

        context.Cinemas.Add(cinema);
        context.Halls.Add(hall);
        context.Seats.AddRange(seats);
        context.Movies.Add(movie);
        context.Sessions.Add(session);
        context.SaveChanges();

        return (session, seats);
    }

    public static User SeedUser(ApplicationDbContext context, UserRole role = UserRole.CUSTOMER)
    {
        var user = new User
        {
            FirstName = "Тест",
            LastName = "Тестенко",
            Email = $"{Guid.NewGuid():N}@test.local",
            Phone = $"+380{Random.Shared.Next(100000000, 999999999)}",
            PasswordHash = "n/a",
            Role = role,
            IsEmailConfirmed = true
        };
        context.Users.Add(user);
        context.SaveChanges();
        return user;
    }
}
