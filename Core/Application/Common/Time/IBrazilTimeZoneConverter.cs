namespace Application.Common.Time;

public interface IBrazilTimeZoneConverter
{
    string TimeZoneId { get; }
    DateTime ConvertBrasiliaToUtc(DateTime brasiliaDateTime);
    DateTime ConvertUtcToBrasilia(DateTime utcDateTime);
}
