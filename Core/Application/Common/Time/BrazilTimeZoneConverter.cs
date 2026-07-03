namespace Application.Common.Time;

public sealed class BrazilTimeZoneConverter : IBrazilTimeZoneConverter
{
    private static readonly string[] TimeZoneIds =
    [
        "E. South America Standard Time",
        "America/Sao_Paulo"
    ];

    private readonly TimeZoneInfo _timeZone;

    public BrazilTimeZoneConverter()
        : this(GetBrazilTimeZone())
    {
    }

    internal BrazilTimeZoneConverter(TimeZoneInfo timeZone)
    {
        _timeZone = timeZone;
    }

    public string TimeZoneId => _timeZone.Id;

    public DateTime ConvertBrasiliaToUtc(DateTime brasiliaDateTime)
    {
        var unspecified = DateTime.SpecifyKind(brasiliaDateTime, DateTimeKind.Unspecified);
        var utcDateTime = TimeZoneInfo.ConvertTimeToUtc(unspecified, _timeZone);
        return DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
    }

    public DateTime ConvertUtcToBrasilia(DateTime utcDateTime)
    {
        var utc = utcDateTime.Kind == DateTimeKind.Utc
            ? utcDateTime
            : DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);

        var brasiliaDateTime = TimeZoneInfo.ConvertTimeFromUtc(utc, _timeZone);
        return DateTime.SpecifyKind(brasiliaDateTime, DateTimeKind.Unspecified);
    }

    private static TimeZoneInfo GetBrazilTimeZone()
    {
        foreach (var timeZoneId in TimeZoneIds)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        throw new InvalidOperationException(
            "Nao foi possivel localizar o timezone de Brasilia. IDs tentados: " +
            string.Join(", ", TimeZoneIds));
    }
}
