using Microsoft.EntityFrameworkCore;
using RezSaaS.Modules.TenantManagement.Domain;
using RezSaaS.Modules.TenantManagement.Infrastructure.Persistence;

namespace RezSaaS.Modules.TenantManagement.Application;

public sealed class ChangeTenantMembershipStatusService
{
    private const string InvalidRequest = "TENANT_MEMBERSHIP_INVALID";
    private const string LastOwnerRequired = "TENANT_LAST_OWNER_REQUIRED";
    private const string MembershipNotFound = "TENANT_MEMBERSHIP_NOT_FOUND";
    private const string MembershipRevoked = "TENANT_MEMBERSHIP_REVOKED";

    private readonly TenantManagementDbContext dbContext;
    private readonly TimeProvider timeProvider;

    public ChangeTenantMembershipStatusService(
        TenantManagementDbContext dbContext,
        TimeProvider timeProvider)
    {
        this.dbContext = dbContext;
        this.timeProvider = timeProvider;
    }

    public async Task<TenantMembershipCommandResult> RevokeAsync(
        ChangeTenantMembershipStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        return await ChangeAsync(
            command,
            TenantMembershipStatus.Revoked,
            "TenantMembershipRevoked",
            cancellationToken);
    }

    public async Task<TenantMembershipCommandResult> SuspendAsync(
        ChangeTenantMembershipStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        return await ChangeAsync(
            command,
            TenantMembershipStatus.Suspended,
            "TenantMembershipSuspended",
            cancellationToken);
    }

    private async Task<TenantMembershipCommandResult> ChangeAsync(
        ChangeTenantMembershipStatusCommand command,
        TenantMembershipStatus targetStatus,
        string auditAction,
        CancellationToken cancellationToken)
    {
        if (command.TenantId == Guid.Empty
            || command.MembershipId == Guid.Empty
            || command.ActorUserAccountId == Guid.Empty)
        {
            return TenantMembershipCommandResult.Failure(InvalidRequest);
        }

        TenantMembership? membership = await dbContext.Memberships
            .SingleOrDefaultAsync(
                entity => entity.TenantId == command.TenantId
                    && entity.Id == command.MembershipId,
                cancellationToken);

        if (membership is null)
        {
            return TenantMembershipCommandResult.Failure(MembershipNotFound);
        }

        if (membership.Status == targetStatus)
        {
            return TenantMembershipCommandResult.Success(membership.Id);
        }

        if (membership.Status == TenantMembershipStatus.Revoked)
        {
            return TenantMembershipCommandResult.Failure(MembershipRevoked);
        }

        if (membership.Role == TenantMembershipRole.BusinessOwner
            && membership.Status == TenantMembershipStatus.Active
            && await CountActiveOwnersAsync(command.TenantId, cancellationToken) <= 1)
        {
            return TenantMembershipCommandResult.Failure(LastOwnerRequired);
        }

        DateTimeOffset now = timeProvider.GetUtcNow();

        if (targetStatus == TenantMembershipStatus.Revoked)
        {
            membership.Revoke();
        }
        else
        {
            membership.Suspend();
        }

        dbContext.AuditLogEntries.Add(
            TenantAuditLogEntry.Create(
                command.TenantId,
                command.ActorUserAccountId,
                auditAction,
                $$"""{"tenantId":"{{command.TenantId}}","membershipId":"{{membership.Id}}","userAccountId":"{{membership.UserAccountId}}"}""",
                now));
        await dbContext.SaveChangesAsync(cancellationToken);

        return TenantMembershipCommandResult.Success(membership.Id);
    }

    private async Task<int> CountActiveOwnersAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Memberships.CountAsync(
            entity => entity.TenantId == tenantId
                && entity.Role == TenantMembershipRole.BusinessOwner
                && entity.Status == TenantMembershipStatus.Active,
            cancellationToken);
    }
}
