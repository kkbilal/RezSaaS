namespace RezSaaS.Api.PublicApi;

public static class PublicTimeZoneResolver
{
    public static DateTimeOffset ConvertLocalToUtc(
        DateTime localTime,
        TimeZoneInfo timeZoneInfo)
    {
        DateTime utcDateTime = TimeZoneInfo.ConvertTimeToUtc(localTime, timeZoneInfo);
        return new DateTimeOffset(utcDateTime, TimeSpan.Zero);
    }

    public static DateTime ConvertUtcToLocal(
        DateTimeOffset utcTime,
        TimeZoneInfo timeZoneInfo)
    {
        return TimeZoneInfo.ConvertTime(utcTime, timeZoneInfo).DateTime;
    }

    public static bool TryFind(
        string timeZoneId,
        out TimeZoneInfo? timeZoneInfo)
    {
        if (TryFindTimeZoneById(timeZoneId, out timeZoneInfo))
        {
            return true;
        }

        if (TimeZoneInfo.TryConvertIanaIdToWindowsId(timeZoneId, out string? windowsTimeZoneId)
            && TryFindTimeZoneById(windowsTimeZoneId, out timeZoneInfo))
        {
            return true;
        }

        if (TimeZoneInfo.TryConvertWindowsIdToIanaId(timeZoneId, out string? ianaTimeZoneId)
            && TryFindTimeZoneById(ianaTimeZoneId, out timeZoneInfo))
        {
            return true;
        }

        timeZoneInfo = null;
        return false;
    }

    private static bool TryFindTimeZoneById(
        string timeZoneId,
        out TimeZoneInfo? timeZoneInfo)
    {
        try
        {
            timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            timeZoneInfo = null;
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            timeZoneInfo = null;
            return false;
        }
    }
}
