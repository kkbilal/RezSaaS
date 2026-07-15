namespace RezSaaS.BuildingBlocks.Time;

/// <summary>
/// Zaman dilimi kimligini cozer, dogrular ve IANA'ya normalize eder.
/// </summary>
/// <remarks>
/// NEDEN BUILDINGBLOCKS'TA:
/// Iki taraf da bu mantiga ihtiyac duyuyor:
///   - Organization modulu: sube olustururken TimeZoneId'yi DOGRULAMAK ve saklamadan once
///     IANA'ya NORMALIZE etmek (aksi halde "Turkey Standard Time" saklanip frontend Intl'i
///     RangeError ile patlatir; "Istanbul" gibi gecersiz deger ise SONSUZA KADAR 0 slot uretir).
///   - Api/Public: slot ararken TimeZoneInfo cozmek.
/// Bu yuzden mantik ortak katmanda, iki taraf da buradan kullanir.
///
/// TryFind ciftyonlu fallback yapar: once dogrudan, sonra IANA->Windows, sonra Windows->IANA.
/// Boylece hem "Europe/Istanbul" hem "Turkey Standard Time" cozulebilir; ama SAKLAMA icin
/// her zaman IANA'ya normalize edilir (Intl yalnizca IANA'yi anlar).
/// </remarks>
public static class TimeZoneResolution
{
    public static bool TryFind(string? timeZoneId, out TimeZoneInfo? timeZoneInfo)
    {
        timeZoneInfo = null;

        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return false;
        }

        string trimmed = timeZoneId.Trim();

        if (TryFindById(trimmed, out timeZoneInfo))
        {
            return true;
        }

        if (TimeZoneInfo.TryConvertIanaIdToWindowsId(trimmed, out string? windowsTimeZoneId)
            && TryFindById(windowsTimeZoneId, out timeZoneInfo))
        {
            return true;
        }

        if (TimeZoneInfo.TryConvertWindowsIdToIanaId(trimmed, out string? ianaTimeZoneId)
            && TryFindById(ianaTimeZoneId, out timeZoneInfo))
        {
            return true;
        }

        timeZoneInfo = null;
        return false;
    }

    public static bool IsValid(string? timeZoneId)
    {
        return TryFind(timeZoneId, out _);
    }

    /// <summary>
    /// Girdiyi IANA kimligine cevirir (or. "Turkey Standard Time" -> "Europe/Istanbul").
    /// Zaten IANA ise oldugu gibi (trim'li) doner. Gecersizse <c>null</c>.
    /// </summary>
    /// <remarks>
    /// SAKLAMA icin bunu kullan. DB'de ve API yanitinda her zaman IANA tutulmali ki frontend
    /// Intl.DateTimeFormat asla patlamasin.
    /// </remarks>
    public static string? TryNormalizeToIana(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return null;
        }

        string trimmed = timeZoneId.Trim();

        if (!TryFind(trimmed, out _))
        {
            return null;
        }

        // Zaten IANA mi? (IANA -> Windows donusumu basariliysa girdi IANA'dir.)
        if (TimeZoneInfo.TryConvertIanaIdToWindowsId(trimmed, out _))
        {
            return trimmed;
        }

        // Windows ID -> IANA'ya cevir.
        if (TimeZoneInfo.TryConvertWindowsIdToIanaId(trimmed, out string? ianaId))
        {
            return ianaId;
        }

        // Ne IANA ne Windows olarak tanindi ama TryFind cozdu (nadir platform durumu):
        // oldugu gibi don -- en azindan gecerli.
        return trimmed;
    }

    private static bool TryFindById(string? timeZoneId, out TimeZoneInfo? timeZoneInfo)
    {
        timeZoneInfo = null;

        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return false;
        }

        try
        {
            timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            return false;
        }
    }
}
