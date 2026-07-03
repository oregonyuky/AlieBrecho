using Application.Common.Time;
using Xunit;

namespace Application.Tests;

public class BrazilTimeZoneConverterTests
{
    private readonly IBrazilTimeZoneConverter _converter = new BrazilTimeZoneConverter();

    [Fact]
    public void ConvertBrasiliaToUtc_ConvertsTwentyHoursToTwentyThreeUtc()
    {
        var brasiliaDateTime = new DateTime(2026, 7, 15, 20, 0, 0, DateTimeKind.Unspecified);

        var utcDateTime = _converter.ConvertBrasiliaToUtc(brasiliaDateTime);

        Assert.Equal(DateTimeKind.Utc, utcDateTime.Kind);
        Assert.Equal(new DateTime(2026, 7, 15, 23, 0, 0, DateTimeKind.Utc), utcDateTime);
    }

    [Fact]
    public void ConvertUtcToBrasilia_ConvertsTwentyThreeUtcToTwentyHoursBrasilia()
    {
        var utcDateTime = new DateTime(2026, 7, 15, 23, 0, 0, DateTimeKind.Utc);

        var brasiliaDateTime = _converter.ConvertUtcToBrasilia(utcDateTime);

        Assert.Equal(DateTimeKind.Unspecified, brasiliaDateTime.Kind);
        Assert.Equal(new DateTime(2026, 7, 15, 20, 0, 0, DateTimeKind.Unspecified), brasiliaDateTime);
    }
}
