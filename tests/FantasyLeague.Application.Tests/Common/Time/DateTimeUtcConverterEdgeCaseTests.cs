using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.Common.Time;

namespace FantasyLeague.Application.Tests.Common.Time;

public sealed class DateTimeUtcConverterEdgeCaseTests
{
    [Fact]
    public void ConvertToUtc_WithNull_ReturnsNull()
    {
        Assert.Null(DateTimeUtcConverter.ConvertToUtc(null, "UTC"));
    }

    [Fact]
    public void ConvertToUtc_WithLocalValue_UsesMachineLocalOffset()
    {
        var local = new DateTime(2026, 1, 15, 12, 30, 0, DateTimeKind.Local);

        var result = DateTimeUtcConverter.ConvertToUtc(local, "ignored-zone");

        Assert.Equal(local.ToUniversalTime(), result);
        Assert.Equal(DateTimeKind.Utc, result?.Kind);
    }

    [Fact]
    public void ConvertToUtc_WithUtcValue_DoesNotResolveInvalidTimeZone()
    {
        var utc = new DateTime(2026, 1, 15, 12, 30, 0, DateTimeKind.Utc);

        Assert.Equal(utc, DateTimeUtcConverter.ConvertToUtc(utc, "not-a-time-zone"));
    }

    [Fact]
    public void ConvertToUtc_WithUnknownTimeZone_ThrowsTimeZoneNotFound()
    {
        var value = new DateTime(2026, 1, 15, 12, 30, 0, DateTimeKind.Unspecified);

        Assert.Throws<TimeZoneNotFoundException>(() =>
            DateTimeUtcConverter.ConvertToUtc(value, "not-a-time-zone"));
    }

    [Fact]
    public void ConvertToUtc_DuringDstGap_ThrowsBadRequest()
    {
        var missingLocalTime = new DateTime(2026, 3, 8, 2, 30, 0, DateTimeKind.Unspecified);

        var exception = Assert.Throws<BadRequestException>(() =>
            DateTimeUtcConverter.ConvertToUtc(missingLocalTime, "America/New_York"));

        Assert.Contains("does not exist", exception.Message);
    }

    [Fact]
    public void ConvertToUtc_DuringDstOverlap_ThrowsBadRequest()
    {
        var ambiguousLocalTime = new DateTime(2026, 11, 1, 1, 30, 0, DateTimeKind.Unspecified);

        var exception = Assert.Throws<BadRequestException>(() =>
            DateTimeUtcConverter.ConvertToUtc(ambiguousLocalTime, "America/New_York"));

        Assert.Contains("ambiguous", exception.Message);
    }

    [Theory]
    [InlineData(" utc ", "UTC")]
    [InlineData("istanbul", "Europe/Istanbul")]
    [InlineData(" NEW YORK ", "America/New_York")]
    public void LocationResolver_WithCaseAndWhitespace_NormalizesLookup(
        string location, string expected)
    {
        Assert.Equal(expected, LocationTimeZoneResolver.Resolve(location));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void LocationResolver_WithMissingLocation_ThrowsBadRequest(string? location)
    {
        Assert.Throws<BadRequestException>(() =>
            LocationTimeZoneResolver.Resolve(location!));
    }
}
