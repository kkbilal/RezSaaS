using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RezSaaS.Modules.TenantManagement.Domain;
using RezSaaS.Modules.TenantManagement.Infrastructure.Persistence;

namespace RezSaaS.Modules.TenantManagement.Application;

public sealed class ChangeTenantLifecycleService
{
    private const string InvalidRequest = "TENANT_LIFECYCLE_INVALID";
    private const int MaxReasonLength = 300;
    private const string TenantClosed = "TENANT_CLOSED";
    private const string TenantNotFound = "TENANT_NOT_FOUND";

    private readonly TenantManagementDbContext dbContext;
    private readonly TimeProvider timeProvider;

    public ChangeTenantLifecycleService(
        TenantManagementDbContext dbContext,
        TimeProvider timeProvider)
    {
        this.dbContext = dbContext;
        this.timeProvider = timeProvider;
    }

    public async Task<TenantLifecycleCommandResult> CloseAsync(
        ChangeTenantLifecycleCommand command,
        CancellationToken cancellationToken = default)
    {
        return await ChangeAsync(
            command,
            TenantStatus.Closed,
            "TenantClosed",
            cancellationToken);
    }

    public async Task<TenantLifecycleCommandResult> ReactivateAsync(
        ChangeTenantLifecycleCommand command,
        CancellationToken cancellationToken = default)
    {
        return await ChangeAsync(
            command,
            TenantStatus.Active,
            "TenantReactivated",
            cancellationToken);
    }

    public async Task<TenantLifecycleCommandResult> SuspendAsync(
        ChangeTenantLifecycleCommand command,
        CancellationToken cancellationToken = default)
    {
        return await ChangeAsync(
            command,
            TenantStatus.Suspended,
            "TenantSuspended",
            cancellationToken);
    }

    private async Task<TenantLifecycleCommandResult> ChangeAsync(
        ChangeTenantLifecycleCommand command,
        TenantStatus targetStatus,
        string auditAction,
        CancellationToken cancellationToken)
    {
        if (command.TenantId == Guid.Empty
            || command.ActorUserAccountId == Guid.Empty
            || string.IsNullOrWhiteSpace(command.Reason)
            || command.Reason.Trim().Length > MaxReasonLength)
        {
            return TenantLifecycleCommandResult.Failure(InvalidRequest);
        }

        await using IDbContextTransaction transaction =
            await dbContext.Database.BeginTransactionAsync(cancellationToken);
        Tenant? tenant = await LockTenantAsync(command.TenantId, cancellationToken);

        if (tenant is null)
        {
            return TenantLifecycleCommandResult.Failure(TenantNotFound);
        }

        if (tenant.Status == targetStatus)
        {
            return TenantLifecycleCommandResult.Success(tenant.Id);
        }

        if (tenant.Status == TenantStatus.Closed)
        {
            return TenantLifecycleCommandResult.Failure(TenantClosed);
        }

        DateTimeOffset now = timeProvider.GetUtcNow();

        switch (targetStatus)
        {
            case TenantStatus.Active:
                tenant.Reactivate();
                break;
            case TenantStatus.Suspended:
                tenant.Suspend(now);
                break;
            case TenantStatus.Closed:
                tenant.Close(now);
                break;
        }

        dbContext.AuditLogEntries.Add(
            TenantAuditLogEntry.Create(
                tenant.Id,
                command.ActorUserAccountId,
                auditAction,
                JsonSerializer.Serialize(
                    new
                    {
                        tenantId = tenant.Id,
                        reason = command.Reason.Trim(),
                    }),
                now));
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return TenantLifecycleCommandResult.Success(tenant.Id);
    }

    private async Task<Tenant?> LockTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Tenants
            .FromSqlInterpolated(
                $"""
                SELECT *
                FROM tenant_management."Tenants"
                WHERE "Id" = {tenantId}
                FOR UPDATE
                """)
            .SingleOrDefaultAsync(cancellationToken);
    }
}
