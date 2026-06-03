using Microsoft.EntityFrameworkCore;
using RezSaaS.Modules.TenantManagement.Domain;
using RezSaaS.Modules.TenantManagement.Infrastructure.Persistence;

namespace RezSaaS.Modules.TenantManagement.Application;

public sealed class AddTenantMembershipService
{
    private const string InvalidRequest = "TENANT_MEMBERSHIP_INVALID";
    private const string MembershipAlreadyExists = "TENANT_MEMBERSHIP_ALREADY_EXISTS";
    private const string TenantNotFound = "TENANT_NOT_FOUND";

    private readonly TenantManagementDbContext dbContext;
    private readonly TimeProvider timeProvider;

    public AddTenantMembershipService(
        TenantManagementDbContext dbContext,
        TimeProvider timeProvider)
    {
        this.dbContext = dbContext;
        this.timeProvider = timeProvider;
    }

    public async Task<TenantMembershipCommandResult> AddAsync(
        AddTenantMembershipCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.TenantId == Guid.Empty
            || command.ActorUserAccountId == Guid.Empty
            || command.UserAccountId == Guid.Empty)
        {
            return TenantMembershipCommandResult.Failure(InvalidRequest);
        }

        bool tenantExists = await dbContext.Tenants
            .AsNoTracking()
            .AnyAsync(entity => entity.Id == command.TenantId, cancellationToken);

        if (!tenantExists)
        {
            return TenantMembershipCommandResult.Failure(TenantNotFound);
        }

        bool membershipExists = await dbContext.Memberships
            .AsNoTracking()
            .AnyAsync(
                entity => entity.TenantId == command.TenantId
                    && entity.UserAccountId == command.UserAccountId,
                cancellationToken);

        if (membershipExists)
        {
            return TenantMembershipCommandResult.Failure(MembershipAlreadyExists);
        }

        DateTimeOffset now = timeProvider.GetUtcNow();
        TenantMembership membership;

        try
        {
            membership = TenantMembership.Create(
                command.TenantId,
                command.UserAccountId,
                command.Role,
                now,
                command.BranchId);
        }
        catch (ArgumentException)
        {
            return TenantMembershipCommandResult.Failure(InvalidRequest);
        }

        dbContext.Memberships.Add(membership);
        dbContext.AuditLogEntries.Add(
            TenantAuditLogEntry.Create(
                command.TenantId,
                command.ActorUserAccountId,
                "TenantMembershipAdded",
                $$"""{"tenantId":"{{command.TenantId}}","membershipId":"{{membership.Id}}","userAccountId":"{{command.UserAccountId}}","role":"{{command.Role}}"}""",
                now));

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return TenantMembershipCommandResult.Failure(MembershipAlreadyExists);
        }

        return TenantMembershipCommandResult.Success(membership.Id);
    }
}
