using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.Common.Time;

namespace FantasyLeague.Application.Tests;

public sealed class LocationTimeZoneResolverTests
{
    [Theory]
    [InlineData("Istanbul", "Europe/Istanbul")]
    [InlineData(" london ", "Europe/London")]
    [InlineData("NEW YORK", "America/New_York")]
    [InlineData("Berlin", "Europe/Berlin")]
    public void Resolve_ReturnsTimeZoneId(string location, string expected)
    {
        Assert.Equal(expected, LocationTimeZoneResolver.Resolve(location));
    }

    [Fact]
    public void Resolve_WhenLocationIsUnsupported_ThrowsBadRequest()
    {
        Assert.Throws<BadRequestException>(() =>
            LocationTimeZoneResolver.Resolve("Unknown"));
    }
}
