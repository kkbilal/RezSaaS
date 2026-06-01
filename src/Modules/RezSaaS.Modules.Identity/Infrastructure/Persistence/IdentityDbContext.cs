using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RezSaaS.Modules.Identity.Domain;

namespace RezSaaS.Modules.Identity.Infrastructure.Persistence;

public sealed class IdentityDbContext
    : IdentityDbContext<UserAccount, IdentityRole<Guid>, Guid>
{
    public const string ConnectionStringName = "IdentityDatabase";

    public IdentityDbContext(DbContextOptions<IdentityDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.HasDefaultSchema("identity");

        builder.Entity<UserAccount>(user =>
        {
            user.Property(account => account.Status)
                .HasConversion<string>()
                .HasMaxLength(32);
        });
    }
}
