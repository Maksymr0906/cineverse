using CineVerse.Infrastructure.Services;
using FluentAssertions;
using Xunit;

namespace CineVerse.UnitTests.Infrastructure;

public class PasswordHasherTests
{
    private readonly PasswordHasher _sut = new();

    [Fact]
    public void Hash_ProducesDifferentHash_ForSamePasswordEachTime()
    {
        // BCrypt генерує випадкову сіль на кожен виклик — два хеші того самого пароля НЕ повинні збігатись.
        var hash1 = _sut.Hash("Password123");
        var hash2 = _sut.Hash("Password123");

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void Verify_ReturnsTrue_ForCorrectPassword()
    {
        var hash = _sut.Hash("CorrectHorseBatteryStaple1");

        _sut.Verify("CorrectHorseBatteryStaple1", hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_ReturnsFalse_ForIncorrectPassword()
    {
        var hash = _sut.Hash("CorrectHorseBatteryStaple1");

        _sut.Verify("WrongPassword1", hash).Should().BeFalse();
    }

    [Fact]
    public void Verify_ReturnsFalse_ForCorruptedHash_InsteadOfThrowing()
    {
        // Захист від 500-помилки: якщо в БД опиниться пошкоджений/чужого формату хеш,
        // авторизація має коректно провалитись, а не впасти з необробленим винятком.
        var act = () => _sut.Verify("AnyPassword1", "not-a-valid-bcrypt-hash");

        act.Should().NotThrow();
        _sut.Verify("AnyPassword1", "not-a-valid-bcrypt-hash").Should().BeFalse();
    }
}
