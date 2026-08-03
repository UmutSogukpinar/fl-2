using FantasyLeague.Application.Common.Time;

namespace FantasyLeague.Application.Tests.Common.Time;

public sealed class DateTimeUtcConverterTests
{
    // Case: Convert To Utc
    // Reasoning: This test verifies the Convert To Utc operation.
    // Expected Result: The expected outcome is: Uses Selected Time Zone For Unspecified Date.
    [Fact]
    public void ConvertToUtc_UsesSelectedTimeZoneForUnspecifiedDate()
    {
        var localDate = new DateTime(
            2026, 8, 10, 20, 0, 0, DateTimeKind.Unspecified);

        var result = DateTimeUtcConverter.ConvertToUtc(
            localDate, "Europe/Istanbul");

        Assert.Equal(
            new DateTime(2026, 8, 10, 17, 0, 0, DateTimeKind.Utc),
            result);
    }

    // Case: Convert To Utc
    // Reasoning: This test verifies the Convert To Utc operation.
    // Expected Result: The expected outcome is: Preserves Utc Date.
    [Fact]
    public void ConvertToUtc_PreservesUtcDate()
    {
        var utcDate = new DateTime(
            2026, 8, 10, 17, 0, 0, DateTimeKind.Utc);

        Assert.Equal(
            utcDate,
            DateTimeUtcConverter.ConvertToUtc(utcDate, "Europe/Istanbul"));
    }
}
