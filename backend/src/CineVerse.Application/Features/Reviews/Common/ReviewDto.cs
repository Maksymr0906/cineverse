namespace CineVerse.Application.Features.Reviews.Common;

public record ReviewDto(Guid Id, string AuthorFirstName, int Rating, string Comment, bool IsVerified, DateTimeOffset CreatedAt);
