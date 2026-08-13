using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.Common.Time;

namespace FantasyLeague.Application.Tests.Common.Time;

public sealed class LocationTimeZoneResolverTests
{
    // Case: Resolve
    // Reasoning: This test verifies the Resolve operation.
    // Expected Result: The expected outcome is: Returns Time Zone Id.
    [Theory]
    [InlineData("Istanbul", "Europe/Istanbul")]
    [InlineData(" london ", "Europe/London")]
    [InlineData("NEW YORK", "America/New_York")]
    [InlineData("Berlin", "Europe/Berlin")]
    public void Resolve_ReturnsTimeZoneId(string location, string expected)
    {
        Assert.Equal(expected, LocationTimeZoneResolver.Resolve(location));
    }

    // Case: Resolve when Location Is Unsupported
    // Reasoning: This test verifies Resolve under the Location Is Unsupported condition.
    // Expected Result: The expected outcome is: Throws Bad Request.
    [Fact]
    public void Resolve_WhenLocationIsUnsupported_ThrowsBadRequest()
    {
        Assert.Throws<BadRequestException>(() =>
            LocationTimeZoneResolver.Resolve("Unknown"));
    }
}
