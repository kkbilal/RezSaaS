using RezSaaS.BuildingBlocks.Time;
using Xunit;

namespace RezSaaS.ArchitectureTests;

/// <summary>
/// <see cref="TimeZoneResolution"/> icin saf birim testleri (Postgres gerektirmez).
/// </summary>
/// <remarks>
/// Bu mantik bir LANSMAN BLOKAJINI kapatiyor: sube TimeZoneId'si eskiden yalnizca uzunlukla
/// dogrulaniyordu. Gecersiz deger ("Istanbul") o subeyi sonsuza kadar 0 slot dondurmeye,
/// Windows ID ("Turkey Standard Time") ise frontend Intl'i RangeError'a itiyordu.
/// </remarks>
public sealed class TimeZoneResolutionTests
{
    [Theory]
    [InlineData("Europe/Istanbul")]
    [InlineData("Turkey Standard Time")] // Windows ID -- yine de cozulmeli
    [InlineData("  Europe/Istanbul  ")]  // trim'lenmeli
    public void IsValidAcceptsResolvableZones(string timeZoneId)
    {
        Assert.True(TimeZoneResolution.IsValid(timeZoneId));
    }

    [Theory]
    [InlineData("Istanbul")]   // sehir adi, zaman dilimi degil
    [InlineData("GMT+3")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void IsValidRejectsGarbage(string? timeZoneId)
    {
        Assert.False(TimeZoneResolution.IsValid(timeZoneId));
    }

    [Fact]
    public void NormalizeConvertsWindowsIdToIana()
    {
        // Saklama icin her zaman IANA: frontend Intl yalnizca IANA'yi anlar.
        Assert.Equal("Europe/Istanbul", TimeZoneResolution.TryNormalizeToIana("Turkey Standard Time"));
    }

    [Fact]
    public void NormalizeKeepsIanaAsIs()
    {
        Assert.Equal("Europe/Istanbul", TimeZoneResolution.TryNormalizeToIana("Europe/Istanbul"));
        Assert.Equal("Europe/Istanbul", TimeZoneResolution.TryNormalizeToIana("  Europe/Istanbul "));
    }

    [Fact]
    public void NormalizeReturnsNullForGarbage()
    {
        Assert.Null(TimeZoneResolution.TryNormalizeToIana("Istanbul"));
        Assert.Null(TimeZoneResolution.TryNormalizeToIana(null));
    }
}
