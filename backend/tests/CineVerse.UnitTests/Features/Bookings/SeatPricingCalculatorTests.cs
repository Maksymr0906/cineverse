using CineVerse.Application.Features.Bookings.Common;
using CineVerse.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace CineVerse.UnitTests.Features.Bookings;

public class SeatPricingCalculatorTests
{
    [Theory]
    [InlineData(SeatType.STANDARD, 100, 100)]
    [InlineData(SeatType.DISABLED, 100, 100)]
    [InlineData(SeatType.VIP, 100, 130)]
    [InlineData(SeatType.LOVE_SEAT, 100, 180)]
    public void CalculatePrice_ReturnsExpectedMultiplier(SeatType seatType, decimal basePrice, decimal expected)
    {
        var result = SeatPricingCalculator.CalculatePrice(basePrice, seatType);

        result.Should().Be(expected);
    }

    [Fact]
    public void CalculatePrice_RoundsToTwoDecimalPlaces()
    {
        // 33.33 * 1.3 = 43.329 — має округлитись до копійок, а не лишитись "неохайним" числом.
        var result = SeatPricingCalculator.CalculatePrice(33.33m, SeatType.VIP);

        result.Should().Be(43.33m);
    }
}
