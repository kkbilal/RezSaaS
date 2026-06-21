using Microsoft.EntityFrameworkCore;
using RezSaaS.BuildingBlocks.Auditing;
using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.Organization.Domain;
using RezSaaS.Modules.Organization.Infrastructure.Persistence;

namespace RezSaaS.Modules.Organization.Application;

public sealed class BranchManagementService
{
    public const string InvalidRequest = "BRANCH_INVALID_REQUEST";
    public const string MissingTenantContext = "MISSING_TENANT_CONTEXT";
    public const string BusinessNotFound = "BUSINESS_NOT_FOUND";
    public const string BranchNotFound = "BRANCH_NOT_FOUND";
    public const string BranchHasStaff = "BRANCH_HAS_STAFF";
    public const string SlugConflict = "BRANCH_SLUG_CONFLICT";

    private readonly IAuditLogRecorder auditLogRecorder;
    private readonly OrganizationDbContext dbContext;
    private readonly ITenantContextAccessor tenantContextAccessor;
    private readonly TimeProvider timeProvider;

    public BranchManagementService(
        OrganizationDbContext dbContext,
        ITenantContextAccessor tenantContextAccessor,
        IAuditLogRecorder auditLogRecorder,
        TimeProvider timeProvider)
    {
        this.dbContext = dbContext;
        this.tenantContextAccessor = tenantContextAccessor;
        this.auditLogRecorder = auditLogRecorder;
        this.timeProvider = timeProvider;
    }

    public async Task<BranchManagementResult> ListAsync(
        CancellationToken cancellationToken = default)
    {
        if (tenantContextAccessor.TenantId is not { } tenantId)
        {
            return BranchManagementResult.Failure(MissingTenantContext);
        }

        List<BranchView> branches = await dbContext.Branches
            .AsNoTracking()
            .Where(entity => entity.TenantId == tenantId)
            .OrderBy(entity => entity.DisplayName)
            .Select(entity => ToView(entity))
            .ToListAsync(cancellationToken);

        return BranchManagementResult.SuccessList(branches);
    }

    public async Task<BranchManagementResult> GetByIdAsync(
        Guid branchId,
        CancellationToken cancellationToken = default)
    {
        if (tenantContextAccessor.TenantId is null)
        {
            return BranchManagementResult.Failure(MissingTenantContext);
        }

        BranchView? branch = await dbContext.Branches
            .AsNoTracking()
            .Where(entity => entity.Id == branchId)
            .Select(entity => ToView(entity))
            .FirstOrDefaultAsync(cancellationToken);

        return branch is not null
            ? BranchManagementResult.Success(branch)
            : BranchManagementResult.Failure(BranchNotFound);
    }

    public async Task<BranchManagementResult> CreateAsync(
        CreateBranchCommand command,
        CancellationToken cancellationToken = default)
    {
        if (tenantContextAccessor.TenantId is not { } tenantId)
        {
            return BranchManagementResult.Failure(MissingTenantContext);
        }

        if (!IsCreateValid(command))
        {
            return BranchManagementResult.Failure(InvalidRequest);
        }

        Guid? businessId = await dbContext.Businesses
            .Where(entity => entity.TenantId == tenantId
                && entity.Status == BusinessStatus.Active)
            .Select(entity => (Guid?)entity.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (businessId is null)
        {
            return BranchManagementResult.Failure(BusinessNotFound);
        }

        string upperSlug = command.Slug.Trim().ToUpperInvariant();
        bool slugExists = await dbContext.Branches
            .AnyAsync(entity => entity.TenantId == tenantId
                && entity.NormalizedSlug == upperSlug,
                cancellationToken);

        if (slugExists)
        {
            return BranchManagementResult.Failure(SlugConflict);
        }

        DateTimeOffset now = timeProvider.GetUtcNow();

        Branch branch = Branch.Create(
            tenantId,
            businessId.Value,
            command.Slug,
            command.DisplayName,
            command.TimeZoneId,
            now,
            command.City,
            command.District,
            command.AddressLine);

        dbContext.Branches.Add(branch);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLogRecorder.RecordAsync(
            new AuditLogRecord(
                tenantId,
                command.ActorUserAccountId,
                "organization.branch.created",
                $$"""{"tenantId":"{{tenantId}}","branchId":"{{branch.Id}}","slug":"{{command.Slug}}"}""",
                now),
            cancellationToken);

        return BranchManagementResult.Success(ToView(branch));
    }

    public async Task<BranchManagementResult> UpdateAsync(
        UpdateBranchCommand command,
        CancellationToken cancellationToken = default)
    {
        if (tenantContextAccessor.TenantId is not { } tenantId)
        {
            return BranchManagementResult.Failure(MissingTenantContext);
        }

        if (!IsUpdateValid(command))
        {
            return BranchManagementResult.Failure(InvalidRequest);
        }

        Branch? branch = await dbContext.Branches
            .FirstOrDefaultAsync(entity => entity.Id == command.BranchId, cancellationToken);

        if (branch is null)
        {
            return BranchManagementResult.Failure(BranchNotFound);
        }

        string oldDisplayName = branch.DisplayName;

        branch.SetLocation(
            command.City,
            command.District,
            command.AddressLine);

        await dbContext.SaveChangesAsync(cancellationToken);

        DateTimeOffset now = timeProvider.GetUtcNow();
        await auditLogRecorder.RecordAsync(
            new AuditLogRecord(
                tenantId,
                command.ActorUserAccountId,
                "organization.branch.updated",
                $$"""{"tenantId":"{{tenantId}}","branchId":"{{branch.Id}}","oldDisplayName":"{{oldDisplayName}}","newDisplayName":"{{command.DisplayName}}"}""",
                now),
            cancellationToken);

        return BranchManagementResult.Success(ToView(branch));
    }

    public async Task<BranchManagementResult> UpdateSlotSettingsAsync(
        UpdateBranchSlotSettingsCommand command,
        CancellationToken cancellationToken = default)
    {
        if (tenantContextAccessor.TenantId is not { } tenantId)
        {
            return BranchManagementResult.Failure(MissingTenantContext);
        }

        if (command.SlotIntervalMinutes is <= 0 || command.MaxPublicSlots is <= 0)
        {
            return BranchManagementResult.Failure(InvalidRequest);
        }

        Branch? branch = await dbContext.Branches
            .FirstOrDefaultAsync(entity => entity.Id == command.BranchId, cancellationToken);

        if (branch is null)
        {
            return BranchManagementResult.Failure(BranchNotFound);
        }

        branch.SetPublicSlotSettings(command.SlotIntervalMinutes, command.MaxPublicSlots);
        await dbContext.SaveChangesAsync(cancellationToken);

        DateTimeOffset now = timeProvider.GetUtcNow();
        await auditLogRecorder.RecordAsync(
            new AuditLogRecord(
                tenantId,
                command.ActorUserAccountId,
                "organization.branch.slot-settings.updated",
                $$"""{"tenantId":"{{tenantId}}","branchId":"{{branch.Id}}","slotIntervalMinutes":{{command.SlotIntervalMinutes}},"maxPublicSlots":{{command.MaxPublicSlots}}}""",
                now),
            cancellationToken);

        return BranchManagementResult.Success(ToView(branch));
    }

    public async Task<BranchManagementResult> ArchiveAsync(
        Guid actorUserAccountId,
        Guid branchId,
        CancellationToken cancellationToken = default)
    {
        if (tenantContextAccessor.TenantId is not { } tenantId)
        {
            return BranchManagementResult.Failure(MissingTenantContext);
        }

        Branch? branch = await dbContext.Branches
            .FirstOrDefaultAsync(entity => entity.Id == branchId, cancellationToken);

        if (branch is null)
        {
            return BranchManagementResult.Failure(BranchNotFound);
        }

        bool hasStaff = await dbContext.StaffMembers
            .AnyAsync(entity => entity.BranchId == branchId, cancellationToken);

        if (hasStaff)
        {
            return BranchManagementResult.Failure(BranchHasStaff);
        }

        dbContext.Branches.Remove(branch);
        await dbContext.SaveChangesAsync(cancellationToken);

        DateTimeOffset now = timeProvider.GetUtcNow();
        await auditLogRecorder.RecordAsync(
            new AuditLogRecord(
                tenantId,
                actorUserAccountId,
                "organization.branch.archived",
                $$"""{"tenantId":"{{tenantId}}","branchId":"{{branch.Id}}"}""",
                now),
            cancellationToken);

        return BranchManagementResult.Success(ToView(branch));
    }

    private static bool IsCreateValid(CreateBranchCommand command)
    {
        return command.ActorUserAccountId != Guid.Empty
            && IsLength(command.Slug, minLength: 2, maxLength: 64)
            && IsLength(command.DisplayName, minLength: 2, maxLength: 200)
            && IsLength(command.TimeZoneId, minLength: 1, maxLength: 80);
    }

    private static bool IsUpdateValid(UpdateBranchCommand command)
    {
        return command.ActorUserAccountId != Guid.Empty
            && command.BranchId != Guid.Empty
            && IsLength(command.DisplayName, minLength: 2, maxLength: 200);
    }

    private static bool IsLength(string? value, int minLength, int maxLength)
    {
        int length = value?.Trim().Length ?? 0;
        return length >= minLength && length <= maxLength;
    }

    private static BranchView ToView(Branch branch)
    {
        return new BranchView(
            branch.Id,
            branch.Slug,
            branch.DisplayName,
            branch.TimeZoneId,
            branch.City,
            branch.District,
            branch.AddressLine,
            branch.SlotIntervalMinutes,
            branch.MaxPublicSlots,
            branch.CreatedAtUtc);
    }
}
