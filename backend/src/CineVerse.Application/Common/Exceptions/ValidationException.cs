using FluentValidation.Results;

namespace CineVerse.Application.Common.Exceptions;

/// <summary>Кидається ValidationBehaviour, коли FluentValidation знаходить помилки вхідних даних. Мапиться на HTTP 400.</summary>
public class ValidationException : Exception
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException() : base("Виникла одна або декілька помилок валідації.")
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(IEnumerable<ValidationFailure> failures) : this()
    {
        Errors = failures
            .GroupBy(f => f.PropertyName, f => f.ErrorMessage)
            .ToDictionary(g => g.Key, g => g.ToArray());
    }
}
