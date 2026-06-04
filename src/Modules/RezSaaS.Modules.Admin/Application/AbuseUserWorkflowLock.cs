using Microsoft.EntityFrameworkCore;
using RezSaaS.Modules.Admin.Infrastructure.Persistence;

namespace RezSaaS.Modules.Admin.Application;

internal static class AbuseUserWorkflowLock
{
    public static async Task AcquireAsync(
        AdminDbContext dbContext,
        Guid userAccountId,
        CancellationToken cancellationToken)
    {
        string lockKey = $"abuse-user-workflow:{userAccountId:D}";
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0))",
            cancellationToken);
    }
}
