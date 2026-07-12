using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RezSaaS.Modules.Integrations.Infrastructure.Persistence;

/// <summary>
/// EF araclari (dotnet ef migrations ...) icin tasarim zamani DbContext fabrikasi.
/// </summary>
/// <remarks>
/// Integrations modulu de Payments gibi Program.cs'te yorum satirinda (Faz 4/5'e kadar kapali),
/// bu yuzden DI'a kaydedilmiyor ve EF araclari context'i kuramiyordu -- yani bu modul icin de
/// migration uretilemiyordu.
///
/// Bkz. <see cref="RezSaaS.Modules.Payments.Infrastructure.Persistence.PaymentsDbContextFactory"/>:
/// ayni sessiz kayma tuzagi. Kapali modulun modeli sapinca entegrasyon testleri toptan duser.
///
/// Yalnizca EF ARACLARI kullanir; calisma zamani davranisini etkilemez.
/// </remarks>
public sealed class IntegrationsDbContextFactory : IDesignTimeDbContextFactory<IntegrationsDbContext>
{
    public IntegrationsDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<IntegrationsDbContext> optionsBuilder = new();
        optionsBuilder.UseNpgsql();

        return new IntegrationsDbContext(optionsBuilder.Options);
    }
}
