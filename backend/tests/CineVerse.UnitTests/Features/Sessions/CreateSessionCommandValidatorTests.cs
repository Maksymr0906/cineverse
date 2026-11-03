using CineVerse.Application.Features.Sessions.Commands.CreateSession;
using FluentValidation.TestHelper;
using Xunit;

namespace CineVerse.UnitTests.Features.Sessions;

public class CreateSessionCommandValidatorTests
{
    private readonly CreateSessionCommandValidator _sut = new();

    [Fact]
    public void ValidCommand_PassesValidation()
    {
        var command = new CreateSessionCommand(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow.AddHours(2), "IMAX", 250m);

        _sut.TestValidate(command).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void StartTimeInThePast_FailsValidation()
    {
        var command = new CreateSessionCommand(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow.AddHours(-1), "2D", 150m);

        _sut.TestValidate(command).ShouldHaveValidationErrorFor(x => x.StartTime);
    }

    [Fact]
    public void ZeroOrNegativePrice_FailsValidation()
    {
        var command = new CreateSessionCommand(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow.AddHours(2), "2D", 0m);

        _sut.TestValidate(command).ShouldHaveValidationErrorFor(x => x.BasePrice);
    }
}
