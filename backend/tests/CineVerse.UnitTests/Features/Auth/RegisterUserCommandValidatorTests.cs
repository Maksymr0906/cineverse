using CineVerse.Application.Features.Auth.Commands.RegisterUser;
using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace CineVerse.UnitTests.Features.Auth;

public class RegisterUserCommandValidatorTests
{
    private readonly RegisterUserCommandValidator _sut = new();

    private static RegisterUserCommand ValidCommand() => new(
        "Іван", "Тестенко", "ivan@example.com", "+380991234567", "Password1");

    [Fact]
    public void ValidCommand_PassesValidation()
    {
        var result = _sut.TestValidate(ValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("")]
    [InlineData("missing-at-sign.com")]
    public void InvalidEmail_FailsValidation(string email)
    {
        var command = ValidCommand() with { Email = email };
        var result = _sut.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData("short1")]      // < 8 символів
    [InlineData("nouppercase1")] // немає великої літери
    [InlineData("NoDigitsHere")] // немає цифри
    public void WeakPassword_FailsValidation(string password)
    {
        var command = ValidCommand() with { Password = password };
        var result = _sut.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("not-a-phone")]
    public void InvalidPhone_FailsValidation(string phone)
    {
        var command = ValidCommand() with { Phone = phone };
        var result = _sut.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Phone);
    }
}
