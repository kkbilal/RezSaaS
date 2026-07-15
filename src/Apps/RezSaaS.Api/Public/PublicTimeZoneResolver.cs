using RezSaaS.BuildingBlocks.Time;

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

    // Ciftyonlu cozum mantigi ortak katmana (BuildingBlocks/TimeZoneResolution) tasindi;
    // ayni mantik sube olustururken de (Organization) dogrulama/normalizasyon icin gerekiyordu.
    // Burada sadece delege ediyoruz -- kod tek yerde yasar.
    public static bool TryFind(
        string timeZoneId,
        out TimeZoneInfo? timeZoneInfo)
    {
        return TimeZoneResolution.TryFind(timeZoneId, out timeZoneInfo);
    }
}
