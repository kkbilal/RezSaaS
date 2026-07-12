using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RezSaaS.Modules.Payments.Infrastructure.Persistence;

/// <summary>
/// EF araclari (dotnet ef migrations ...) icin tasarim zamani DbContext fabrikasi.
/// </summary>
/// <remarks>
/// NEDEN GEREKLI: Payments modulu Program.cs'te YORUM SATIRINDA (Faz 4/5'e kadar kapali),
/// dolayisiyla PaymentsDbContext DI'a hic kaydedilmiyor. EF araclari da context'i uygulamanin
/// servis saglayicisindan cozmeye calistigi icin "Unable to create a DbContext" diyordu.
///
/// Sonuc: kapali modulun modeli degistiginde migration URETILEMIYORDU. Model snapshot'tan
/// sapti, kimse fark etmedi -- ta ki entegrasyon testleri (bu context'i dogrudan kuruyorlar)
/// "PaymentsDbContext has pending changes" ile TOPTAN dusene kadar. Yani kapali bir modulun
/// kaymasi, TUM test paketini kilitlemisti.
///
/// Bu fabrika yalnizca EF ARACLARI tarafindan kullanilir; calisma zamani davranisini
/// etkilemez. Modul yeniden acildiginda da zararsizdir.
/// </remarks>
public sealed class PaymentsDbContextFactory : IDesignTimeDbContextFactory<PaymentsDbContext>
{
    public PaymentsDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<PaymentsDbContext> optionsBuilder = new();

        // Tasarim zamaninda gercek bir baglanti KURULMAZ; EF yalnizca saglayicinin
        // (Npgsql) tip eslemelerini bilmek ister. Migration uretmek icin bu yeterlidir.
        optionsBuilder.UseNpgsql();

        return new PaymentsDbContext(optionsBuilder.Options);
    }
}
