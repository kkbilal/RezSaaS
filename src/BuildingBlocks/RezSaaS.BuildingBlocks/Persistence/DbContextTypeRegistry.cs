using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace RezSaaS.BuildingBlocks.Persistence;

/// <summary>
/// DI'a kayitli DbContext tiplerinin listesi.
/// </summary>
/// <remarks>
/// Bu liste ELLE TUTULMAZ. <see cref="DatabaseMigrationServiceCollectionExtensions.AddDatabaseMigration"/>
/// servis koleksiyonunu tarayarak doldurur.
///
/// Neden onemli: elle tutulan bir liste olsaydi, yeni bir modul ekleyen kisi buraya yazmayi
/// unuttugunda o modulun migration'lari SESSIZCE uygulanmazdi -- ve bunu ancak uretimde,
/// "tablo yok" hatasiyla fark ederdik. Tarama, unutmayi imkansiz kilar.
/// </remarks>
public sealed class DbContextTypeRegistry
{
    public DbContextTypeRegistry(IReadOnlyList<Type> contextTypes)
    {
        ContextTypes = contextTypes;
    }

    public IReadOnlyList<Type> ContextTypes { get; }
}

public static class DatabaseMigrationServiceCollectionExtensions
{
    /// <summary>
    /// Acilista otomatik migration'i devreye alir.
    /// </summary>
    /// <remarks>
    /// MODULLER KAYDEDILDIKTEN SONRA cagrilmalidir (AddModules'tan sonra) -- aksi halde
    /// servis koleksiyonunda taranacak DbContext bulunamaz.
    /// </remarks>
    public static IServiceCollection AddDatabaseMigration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<DatabaseMigrationOptions>()
            .Bind(configuration.GetSection(DatabaseMigrationOptions.SectionName))
            .Validate(
                options => options.LockTimeoutSeconds > 0,
                "Database:LockTimeoutSeconds pozitif olmalidir.")
            .ValidateOnStart();

        List<Type> contextTypes = services
            .Select(descriptor => descriptor.ServiceType)
            .Where(type => type.IsClass
                && !type.IsAbstract
                && type.IsSubclassOf(typeof(DbContext)))
            .Distinct()
            .OrderBy(type => type.Name, StringComparer.Ordinal)
            .ToList();

        services.AddSingleton(new DbContextTypeRegistry(contextTypes));
        services.AddHostedService<DatabaseMigrator>();

        return services;
    }
}
