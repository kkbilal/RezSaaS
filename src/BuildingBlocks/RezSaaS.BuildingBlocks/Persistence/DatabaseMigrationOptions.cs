namespace RezSaaS.BuildingBlocks.Persistence;

public sealed class DatabaseMigrationOptions
{
    public const string SectionName = "Database";

    /// <summary>
    /// Uygulama acilirken bekleyen migration'lari otomatik uygular.
    /// </summary>
    /// <remarks>
    /// Neden var: migration'lar elle uygulaniyordu ve bu, "sema eksik/gerideki DB" durumunu
    /// sessizce uretiyordu. Gelistiricinin makinesindeki DB ile bir baskasininki ayrisiyordu.
    ///
    /// DIKKAT: Bunu production'da acik birakmadan once oku.
    /// - Coklu instance guvenli: migration'lar Postgres SESSION-LEVEL advisory lock altinda
    ///   calisir, ikinci instance kilidi bekler. Yarisma yok.
    /// - Ama yikici bir migration (kolon/tablo dusuren) otomatik uygulanir ve GERI ALINMAZ.
    ///   Gercek musteri verisi olan bir ortamda migration'i CI/CD adiminda, yedek sonrasi,
    ///   bilincli calistirmak daha guvenlidir. O zaman burayi false yap.
    /// </remarks>
    public bool AutoMigrateOnStartup { get; set; } = true;

    /// <summary>
    /// Advisory lock'i beklerken en fazla bu kadar saniye beklenir.
    /// Suresi dolarsa uygulama HATA ile acilmaz (fail-fast) -- yarim migrate edilmis
    /// bir semanin uzerine trafik almaktansa acilmamak yeglenir.
    /// </summary>
    public int LockTimeoutSeconds { get; set; } = 180;
}
