using System.Data.Common;
using System.Globalization;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace RezSaaS.BuildingBlocks.Persistence;

/// <summary>
/// DI'a kayitli TUM DbContext'lerin bekleyen migration'larini uygulama acilirken calistirir.
/// </summary>
/// <remarks>
/// Bu bir <see cref="IHostedService"/>'tir; StartAsync, sunucu istek kabul etmeye BASLAMADAN
/// once calisir. Yani uygulama trafige acildiginda sema her zaman gunceldir.
///
/// COKLU INSTANCE GUVENLIGI:
/// Migration'lar Postgres SESSION-LEVEL advisory lock (pg_advisory_lock) altinda calisir.
/// Ayni anda acilan ikinci instance kilidi bekler; ilk instance bitirince kilidi alir ve
/// uygulanacak bir sey kalmadigini gorup gecer.
/// Transaction-scoped kilit (pg_advisory_xact_lock -- projenin baska yerlerinde kullanilan
/// kalip) BURADA ISE YARAMAZ: her migration kendi transaction'inda calisir, kilit aralarda
/// dusordu ve iki instance ayni migration'i ayni anda uygulamaya calisabilirdi.
///
/// Devre disi birakmak icin: Database:AutoMigrateOnStartup = false
/// </remarks>
public sealed class DatabaseMigrator : IHostedService
{
    // Bu uygulamaya ozgu sabit kilit adi. Ayni Postgres'i paylasan baska bir uygulamanin
    // kilidiyle carpismasin diye isimlendirilmis bir hash kullaniyoruz.
    private const string LockName = "rezsaas:database-migrations";

    private static readonly Action<ILogger, Exception?> LogAutoMigrateDisabled =
        LoggerMessage.Define(
            LogLevel.Information,
            new EventId(1, nameof(DatabaseMigrator)),
            "Otomatik migration kapali (Database:AutoMigrateOnStartup = false). Sema dogrulanmadi.");

    private static readonly Action<ILogger, Exception?> LogNoContexts =
        LoggerMessage.Define(
            LogLevel.Warning,
            new EventId(2, nameof(DatabaseMigrator)),
            "Migrate edilecek DbContext bulunamadi.");

    private static readonly Action<ILogger, string, int, string, Exception?> LogApplying =
        LoggerMessage.Define<string, int, string>(
            LogLevel.Information,
            new EventId(3, nameof(DatabaseMigrator)),
            "{Context}: {Count} migration uygulaniyor -> {Migrations}");

    private static readonly Action<ILogger, int, int, long, Exception?> LogCompleted =
        LoggerMessage.Define<int, int, long>(
            LogLevel.Information,
            new EventId(4, nameof(DatabaseMigrator)),
            "Sema guncel. {ContextCount} DbContext kontrol edildi, {AppliedCount} migration uygulandi ({ElapsedMs} ms).");

    private static readonly Action<ILogger, Exception?> LogUnlockFailed =
        LoggerMessage.Define(
            LogLevel.Warning,
            new EventId(5, nameof(DatabaseMigrator)),
            "Migration advisory lock'i birakilirken hata olustu.");

    private readonly ILogger<DatabaseMigrator> logger;
    private readonly DatabaseMigrationOptions options;
    private readonly IServiceProvider serviceProvider;

    public DatabaseMigrator(
        IServiceProvider serviceProvider,
        IOptions<DatabaseMigrationOptions> options,
        ILogger<DatabaseMigrator> logger)
    {
        this.serviceProvider = serviceProvider;
        this.options = options.Value;
        this.logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!options.AutoMigrateOnStartup)
        {
            LogAutoMigrateDisabled(logger, null);
            return;
        }

        DbContextTypeRegistry registry = serviceProvider.GetRequiredService<DbContextTypeRegistry>();

        if (registry.ContextTypes.Count == 0)
        {
            LogNoContexts(logger, null);
            return;
        }

        await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();

        List<DbContext> contexts = registry.ContextTypes
            .Select(type => (DbContext)scope.ServiceProvider.GetRequiredService(type))
            .ToList();

        // Kilidi TEK bir baglanti uzerinden, oturum boyunca tutuyoruz.
        DbContext lockContext = contexts[0];
        DbConnection connection = lockContext.Database.GetDbConnection();

        await connection.OpenAsync(cancellationToken);

        try
        {
            await AcquireLockAsync(lockContext, cancellationToken);

            Stopwatch stopwatch = Stopwatch.StartNew();
            int appliedTotal = 0;

            foreach (DbContext context in contexts)
            {
                List<string> pending =
                    (await context.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();

                if (pending.Count == 0)
                {
                    continue;
                }

                LogApplying(
                    logger,
                    context.GetType().Name,
                    pending.Count,
                    string.Join(", ", pending),
                    null);

                await context.Database.MigrateAsync(cancellationToken);
                appliedTotal += pending.Count;
            }

            LogCompleted(logger, contexts.Count, appliedTotal, stopwatch.ElapsedMilliseconds, null);
        }
        finally
        {
            await ReleaseLockAsync(lockContext, connection);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task AcquireLockAsync(DbContext lockContext, CancellationToken cancellationToken)
    {
        // lock_timeout: kilit beklerken sonsuza kadar asili kalmayalim. Suresi dolarsa
        // saglayici exception firlatir ve uygulama ACILMAZ -- yarim uygulanmis bir semanin
        // uzerine trafik almaktansa fail-fast dogru davranistir.
        //
        // NOT: "SET lock_timeout = ..." parametre KABUL ETMEZ, bu yuzden string interpolasyonu
        // gerekirdi ve EF1002 (SQL injection) analizoru hakli olarak buna itiraz eder.
        // set_config() ise parametre alir -- ayni isi yapar, enjeksiyon yuzeyi birakmaz.
        string timeoutMilliseconds =
            (options.LockTimeoutSeconds * 1000).ToString(CultureInfo.InvariantCulture);

        await lockContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT set_config('lock_timeout', {timeoutMilliseconds}, false)",
            cancellationToken);

        await lockContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_lock(hashtextextended({LockName}, 0))",
            cancellationToken);
    }

    private async Task ReleaseLockAsync(DbContext lockContext, DbConnection connection)
    {
        try
        {
            await lockContext.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_unlock(hashtextextended({LockName}, 0))",
                CancellationToken.None);
        }
        catch (Exception exception)
        {
            // Kilit oturum kapaninca zaten dusor; burada patlamak asil migration hatasini
            // gizlemekten baska ise yaramaz.
            LogUnlockFailed(logger, exception);
        }
        finally
        {
            await connection.CloseAsync();
        }
    }
}
