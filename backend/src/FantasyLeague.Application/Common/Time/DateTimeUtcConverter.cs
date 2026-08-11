using FantasyLeague.Application.Common.Exceptions;

namespace FantasyLeague.Application.Common.Time;

public static class DateTimeUtcConverter
{
    public static DateTime? ConvertToUtc(DateTime? dateTime, string timeZoneId)
    {
        if (dateTime is null)
        {
            return null;
        }

        if (dateTime.Value.Kind == DateTimeKind.Utc)
        {
            return dateTime.Value;
        }

        if (dateTime.Value.Kind == DateTimeKind.Local)
        {
            return dateTime.Value.ToUniversalTime();
        }

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        var localDateTime = DateTime.SpecifyKind(dateTime.Value, DateTimeKind.Unspecified);

        if (timeZone.IsInvalidTime(localDateTime))
        {
            throw new BadRequestException(
                "DraftDate does not exist in the selected time zone due to a daylight-saving transition.");
        }

        if (timeZone.IsAmbiguousTime(localDateTime))
        {
            throw new BadRequestException(
                "DraftDate is ambiguous in the selected time zone due to a daylight-saving transition.");
        }

        return TimeZoneInfo.ConvertTimeToUtc(localDateTime, timeZone);
    }
}
