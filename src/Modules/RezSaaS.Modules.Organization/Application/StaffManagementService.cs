using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RezSaaS.BuildingBlocks.Auditing;
using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.Organization.Domain;
using RezSaaS.Modules.Organization.Infrastructure.Persistence;

namespace RezSaaS.Modules.Organization.Application;

public sealed class StaffManagementService
{
    public const string InvalidRequest = "STAFF_INVALID_REQUEST";
    public const string MissingTenantContext = "MISSING_TENANT_CONTEXT";
    public const string BranchNotFound = "BRANCH_NOT_FOUND";
    public const string StaffNotFound = "STAFF_NOT_FOUND";

    private readonly IAuditLogRecorder auditLogRecorder;
    private readonly OrganizationDbContext dbContext;
    private readonly ITenantContextAccessor tenantContextAccessor;
    private readonly TimeProvider timeProvider;

    public StaffManagementService(
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

    public async Task<StaffManagementResult> ListByBranchAsync(
        Guid branchId,
        CancellationToken cancellationToken = default)
    {
        if (tenantContextAccessor.TenantId is null)
        {
            return StaffManagementResult.Failure(MissingTenantContext);
        }

        List<StaffView> staffMembers = await dbContext.StaffMembers
            .AsNoTracking()
            .Where(entity => entity.BranchId == branchId)
            .OrderBy(entity => entity.DisplayName)
            .Select(entity => ToView(entity))
            .ToListAsync(cancellationToken);

        return StaffManagementResult.SuccessList(staffMembers);
    }

    public async Task<StaffManagementResult> GetByIdAsync(
        Guid staffId,
        CancellationToken cancellationToken = default)
    {
        if (tenantContextAccessor.TenantId is null)
        {
            return StaffManagementResult.Failure(MissingTenantContext);
        }

        StaffView? staff = await dbContext.StaffMembers
            .AsNoTracking()
            .Where(entity => entity.Id == staffId)
            .Select(entity => ToView(entity))
            .FirstOrDefaultAsync(cancellationToken);

        return staff is not null
            ? StaffManagementResult.Success(staff)
            : StaffManagementResult.Failure(StaffNotFound);
    }

    public async Task<StaffManagementResult> CreateAsync(
        CreateStaffCommand command,
        CancellationToken cancellationToken = default)
    {
        if (tenantContextAccessor.TenantId is not { } tenantId)
        {
            return StaffManagementResult.Failure(MissingTenantContext);
        }

        if (!IsCreateValid(command))
        {
            return StaffManagementResult.Failure(InvalidRequest);
        }

        bool branchExists = await dbContext.Branches
            .AnyAsync(entity => entity.Id == command.BranchId, cancellationToken);

        if (!branchExists)
        {
            return StaffManagementResult.Failure(BranchNotFound);
        }

        DateTimeOffset now = timeProvider.GetUtcNow();

        StaffMember staff = StaffMember.Create(
            tenantId,
            command.BranchId,
            command.DisplayName,
            now,
            command.UserAccountId.HasValue && command.UserAccountId.Value != Guid.Empty
                ? command.UserAccountId
                : null);

        dbContext.StaffMembers.Add(staff);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLogRecorder.RecordAsync(
            new AuditLogRecord(
                tenantId,
                command.ActorUserAccountId,
                "organization.staff.created",
                $$"""{"tenantId":"{{tenantId}}","staffId":"{{staff.Id}}","branchId":"{{command.BranchId}}"}""",
                now),
            cancellationToken);

        return StaffManagementResult.Success(ToView(staff));
    }

    public async Task<StaffManagementResult> UpdateAsync(
        UpdateStaffCommand command,
        CancellationToken cancellationToken = default)
    {
        if (tenantContextAccessor.TenantId is not { } tenantId)
        {
            return StaffManagementResult.Failure(MissingTenantContext);
        }

        if (!IsUpdateValid(command))
        {
            return StaffManagementResult.Failure(InvalidRequest);
        }

        StaffMember? staff = await dbContext.StaffMembers
            .FirstOrDefaultAsync(entity => entity.Id == command.StaffId
                && entity.BranchId == command.BranchId,
                cancellationToken);

        if (staff is null)
        {
            return StaffManagementResult.Failure(StaffNotFound);
        }

        // BUG FIX: burada HICBIR SEY UYGULANMIYORDU. Entity cekiliyor, SaveChangesAsync
        // cagriliyor ve "guncellendi" audit'i yaziliyordu -- ama DisplayName hic degismiyordu.
        // Istek 200 OK donuyor, isim eski kaliyordu.
        string previousDisplayName = staff.DisplayName;
        staff.Rename(command.DisplayName);

        await dbContext.SaveChangesAsync(cancellationToken);

        DateTimeOffset now = timeProvider.GetUtcNow();
        await auditLogRecorder.RecordAsync(
            new AuditLogRecord(
                tenantId,
                command.ActorUserAccountId,
                "organization.staff.updated",
                // Audit kaydi artik NE DEGISTIGINI soyluyor. Eskiden sadece "guncellendi"
                // diyordu -- ustelik hicbir sey guncellenmedigi halde.
                $$"""{"tenantId":"{{tenantId}}","staffId":"{{staff.Id}}","previousDisplayName":{{JsonSerializer.Serialize(previousDisplayName)}},"displayName":{{JsonSerializer.Serialize(staff.DisplayName)}}}""",
                now),
            cancellationToken);

        return StaffManagementResult.Success(ToView(staff));
    }

    public async Task<StaffManagementResult> ArchiveAsync(
        Guid actorUserAccountId,
        Guid staffId,
        CancellationToken cancellationToken = default)
    {
        if (tenantContextAccessor.TenantId is not { } tenantId)
        {
            return StaffManagementResult.Failure(MissingTenantContext);
        }

        StaffMember? staff = await dbContext.StaffMembers
            .FirstOrDefaultAsync(entity => entity.Id == staffId, cancellationToken);

        if (staff is null)
        {
            return StaffManagementResult.Failure(StaffNotFound);
        }

        staff.Archive();
        await dbContext.SaveChangesAsync(cancellationToken);

        DateTimeOffset now = timeProvider.GetUtcNow();
        await auditLogRecorder.RecordAsync(
            new AuditLogRecord(
                tenantId,
                actorUserAccountId,
                "organization.staff.archived",
                $$"""{"tenantId":"{{tenantId}}","staffId":"{{staff.Id}}"}""",
                now),
            cancellationToken);

        return StaffManagementResult.Success(ToView(staff));
    }

    private static bool IsCreateValid(CreateStaffCommand command)
    {
        return command.ActorUserAccountId != Guid.Empty
            && command.BranchId != Guid.Empty
            && IsLength(command.DisplayName, minLength: 2, maxLength: 200);
    }

    // UpdateAsync'in HIC validasyonu yoktu (zaten hicbir sey de uygulamiyordu).
    // Ayni kurallar: isim 2-200 karakter.
    private static bool IsUpdateValid(UpdateStaffCommand command)
    {
        return command.ActorUserAccountId != Guid.Empty
            && command.BranchId != Guid.Empty
            && command.StaffId != Guid.Empty
            && IsLength(command.DisplayName, minLength: 2, maxLength: 200);
    }

    private static bool IsLength(string? value, int minLength, int maxLength)
    {
        int length = value?.Trim().Length ?? 0;
        return length >= minLength && length <= maxLength;
    }

    private static StaffView ToView(StaffMember staff)
    {
        return new StaffView(
            staff.Id,
            staff.BranchId,
            staff.DisplayName,
            staff.UserAccountId,
            staff.Status.ToString(),
            staff.CreatedAtUtc);
    }
}
