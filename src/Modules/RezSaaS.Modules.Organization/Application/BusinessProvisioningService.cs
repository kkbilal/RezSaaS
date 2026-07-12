using Microsoft.EntityFrameworkCore;
using RezSaaS.BuildingBlocks.Auditing;
using RezSaaS.Modules.Organization.Domain;
using RezSaaS.Modules.Organization.Infrastructure.Persistence;

namespace RezSaaS.Modules.Organization.Application;

/// <summary>
/// Bir tenant icin <see cref="Business"/> kaydini olusturur.
/// </summary>
/// <remarks>
/// NEDEN VAR (LANSMAN BLOKAJIYDI):
/// Business -- salonun public kimligi: slug, ad, kategori, SEO, puan -- HICBIR uretim kodu
/// yolundan olusturulmuyordu. `Business.Create(...)` yalnizca TESTLERDEN cagriliyordu;
/// ne bir API ucu, ne seeder, ne tenant provisioning onu yaratiyordu.
///
/// Sonuc: platform admin tenant acar, owner giris yapar, ama:
///   - `BranchManagementService.CreateAsync` aktif bir Business arar, bulamaz -> BUSINESS_NOT_FOUND
///     => owner SUBE BILE ACAMAZ, yani kurulum daha ilk adimda olur.
///   - Salon /kesfet'te (public discovery Businesses'i sorgular) HIC GORUNMEZ.
/// Yani urun TEK BIR SALONU BILE onboard edemiyordu.
///
/// Entegrasyon testleri Business'i dogrudan seed ettigi icin bu bosluk hic fark edilmemisti;
/// uctan uca duman testi (scripts/e2e-smoke.py) ortaya cikardi.
///
/// IDEMPOTENT: Tenant provisioning yarida kalirsa (tenant yaratildi, Business yaratilamadi)
/// islem tekrar denenebilmeli. Zaten varsa mevcut kaydi doner, hata vermez.
/// </remarks>
public sealed class BusinessProvisioningService
{
    public const string InvalidRequest = "BUSINESS_PROVISIONING_INVALID";
    public const string SlugConflict = "BUSINESS_SLUG_ALREADY_EXISTS";

    private readonly IAuditLogRecorder auditLogRecorder;
    private readonly OrganizationDbContext dbContext;
    private readonly TimeProvider timeProvider;

    public BusinessProvisioningService(
        OrganizationDbContext dbContext,
        IAuditLogRecorder auditLogRecorder,
        TimeProvider timeProvider)
    {
        this.dbContext = dbContext;
        this.auditLogRecorder = auditLogRecorder;
        this.timeProvider = timeProvider;
    }

    public async Task<BusinessProvisioningResult> CreateAsync(
        CreateBusinessCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.TenantId == Guid.Empty
            || command.ActorUserAccountId == Guid.Empty
            || !IsLength(command.Slug, 2, 100)
            || !IsLength(command.DisplayName, 2, 200)
            || !IsLength(command.CategoryKey, 2, 64))
        {
            return BusinessProvisioningResult.Failure(InvalidRequest);
        }

        string slug = command.Slug.Trim();
        string normalizedSlug = slug.ToUpperInvariant();

        // IgnoreQueryFilters: bu servis PLATFORM ADMIN baglaminda calisir; tenant context'i
        // hedef tenant'a set EDILMEMISTIR. Global tenant query filter'i devrede birakirsak
        // hicbir satiri goremeyiz ve her cagriyi "yok" sanip mukerrer kayit uretiriz.
        Business? existing = await dbContext.Businesses
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                entity => entity.TenantId == command.TenantId,
                cancellationToken);

        if (existing is not null)
        {
            // IDEMPOTENT: tenant'in zaten bir isletmesi var. Yeniden yaratmiyoruz.
            return BusinessProvisioningResult.Success(existing.Id, existing.Slug);
        }

        bool slugTaken = await dbContext.Businesses
            .IgnoreQueryFilters()
            .AnyAsync(
                entity => entity.NormalizedSlug == normalizedSlug,
                cancellationToken);

        if (slugTaken)
        {
            return BusinessProvisioningResult.Failure(SlugConflict);
        }

        DateTimeOffset now = timeProvider.GetUtcNow();

        Business business = Business.Create(
            command.TenantId,
            slug,
            command.DisplayName.Trim(),
            command.CategoryKey.Trim(),
            now,
            command.Description ?? string.Empty);

        dbContext.Businesses.Add(business);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLogRecorder.RecordAsync(
            new AuditLogRecord(
                command.TenantId,
                command.ActorUserAccountId,
                "organization.business.provisioned",
                $$"""{"tenantId":"{{command.TenantId}}","businessId":"{{business.Id}}","slug":"{{business.Slug}}"}""",
                now),
            cancellationToken);

        return BusinessProvisioningResult.Success(business.Id, business.Slug);
    }

    private static bool IsLength(string? value, int minLength, int maxLength)
    {
        int length = value?.Trim().Length ?? 0;
        return length >= minLength && length <= maxLength;
    }
}

public sealed record CreateBusinessCommand(
    Guid ActorUserAccountId,
    Guid TenantId,
    string Slug,
    string DisplayName,
    string CategoryKey,
    string? Description = null);

public sealed record BusinessProvisioningResult(
    bool Succeeded,
    string? ErrorCode,
    Guid? BusinessId,
    string? Slug)
{
    public static BusinessProvisioningResult Success(Guid businessId, string slug)
    {
        return new BusinessProvisioningResult(true, null, businessId, slug);
    }

    public static BusinessProvisioningResult Failure(string errorCode)
    {
        return new BusinessProvisioningResult(false, errorCode, null, null);
    }
}
