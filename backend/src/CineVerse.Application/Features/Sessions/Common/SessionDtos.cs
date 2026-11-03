namespace CineVerse.Application.Features.Sessions.Common;

/// <summary>Технологічна перерва між сеансами в одному залі (прибирання, реклама) — п.2.2 ТЗ.</summary>
public static class SessionScheduling
{
    public const int CleaningBufferMinutes = 20;
}

public record SessionDto(
    Guid Id,
    Guid MovieId,
    string MovieTitle,
    string PosterUrl,
    Guid HallId,
    string HallName,
    Guid CinemaId,
    string CinemaName,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    string Format,
    decimal BasePrice
);
