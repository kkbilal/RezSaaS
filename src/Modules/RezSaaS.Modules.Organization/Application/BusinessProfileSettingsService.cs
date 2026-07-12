using Microsoft.EntityFrameworkCore;
using RezSaaS.BuildingBlocks.Auditing;
using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.Organization.Domain;
using RezSaaS.Modules.Organization.Infrastructure.Persistence;

namespace RezSaaS.Modules.Organization.Application;

public sealed class BusinessProfileSettingsService
{
    public const string InvalidRequest = "BUSINESS_PROFILE_SETTINGS_INVALID_REQUEST";
    public const string MissingTenantContext = "MISSING_TENANT_CONTEXT";
    public const string MultipleBusinessesUnsupported = "MULTIPLE_BUSINESSES_UNSUPPORTED";
    public const string NotFound = "BUSINESS_PROFILE_NOT_FOUND";

    private readonly IAuditLogRecorder auditLogRecorder;
    private readonly OrganizationDbContext dbContext;
    private readonly ITenantContextAccessor tenantContextAccessor;
    private readonly TimeProvider timeProvider;

    public BusinessProfileSettingsService(
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

    public async Task<BusinessProfileSettingsResult> GetCurrentAsync(
        CancellationToken cancellationToken = default)
    {
        if (tenantContextAccessor.TenantId is null)
        {
            return BusinessProfileSettingsResult.Failure(MissingTenantContext);
        }

        List<BusinessProfileSettingsView> businesses = await dbContext.Businesses
            .AsNoTracking()
            .Where(entity => entity.Status == BusinessStatus.Active)
            .OrderBy(entity => entity.CreatedAtUtc)
            .Take(2)
            .Select(entity => ToView(entity))
            .ToListAsync(cancellationToken);

        return businesses.Count switch
        {
            0 => BusinessProfileSettingsResult.Failure(NotFound),
            1 => BusinessProfileSettingsResult.Success(businesses[0]),
            _ => BusinessProfileSettingsResult.Failure(MultipleBusinessesUnsupported),
        };
    }

    public async Task<BusinessProfileSettingsResult> UpdateAsync(
        UpdateBusinessProfileSettingsCommand command,
        CancellationToken cancellationToken = default)
    {
        if (tenantContextAccessor.TenantId is not { } tenantId)
        {
            return BusinessProfileSettingsResult.Failure(MissingTenantContext);
        }

        if (!IsValid(command)
            || !Enum.TryParse(
                command.StaffDisplayPolicy,
                ignoreCase: true,
                out PublicStaffDisplayPolicy staffDisplayPolicy))
        {
            return BusinessProfileSettingsResult.Failure(InvalidRequest);
        }

        List<Business> businesses = await dbContext.Businesses
            .Where(entity => entity.Status == BusinessStatus.Active)
            .OrderBy(entity => entity.CreatedAtUtc)
            .Take(2)
            .ToListAsync(cancellationToken);

        if (businesses.Count == 0)
        {
            return BusinessProfileSettingsResult.Failure(NotFound);
        }

        if (businesses.Count > 1)
        {
            return BusinessProfileSettingsResult.Failure(MultipleBusinessesUnsupported);
        }

        Business business = businesses[0];
        string[] changedFields = GetChangedFields(
            business,
            command,
            staffDisplayPolicy);

        business.Rename(command.DisplayName);
        business.UpdateDescription(command.Description);
        business.UpdatePublicProfile(
            command.PublicRules,
            command.SeoTitle,
            command.SeoDescription,
            staffDisplayPolicy);
        // null = "bu alana dokunma". Mevcut politika korunur.
        if (command.CancellationCutoffHours is { } cutoffHours)
        {
            business.UpdateCancellationPolicy(cutoffHours);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        if (changedFields.Length > 0)
        {
            DateTimeOffset now = timeProvider.GetUtcNow();
            await auditLogRecorder.RecordAsync(
                new AuditLogRecord(
                    tenantId,
                    command.ActorUserAccountId,
                    "organization.business.profile.updated",
                    CreateAuditDetailsJson(tenantId, business.Id, changedFields),
                    now),
                cancellationToken);
        }

        return BusinessProfileSettingsResult.Success(ToView(business));
    }

    private static bool IsValid(UpdateBusinessProfileSettingsCommand command)
    {
        return command.ActorUserAccountId != Guid.Empty
            && IsLength(command.DisplayName, minLength: 2, maxLength: 200)
            && IsLength(command.Description, minLength: 0, maxLength: 600)
            && IsLength(command.PublicRules, minLength: 0, maxLength: 1000)
            && IsLength(command.SeoTitle, minLength: 0, maxLength: 120)
            && IsLength(command.SeoDescription, minLength: 0, maxLength: 180)
            && (command.CancellationCutoffHours is null
                || (command.CancellationCutoffHours >= 0
                    && command.CancellationCutoffHours <= Business.MaxCancellationCutoffHours));
    }

    private static bool IsLength(string? value, int minLength, int maxLength)
    {
        int length = value?.Trim().Length ?? 0;

        return length >= minLength && length <= maxLength;
    }

    private static string[] GetChangedFields(
        Business business,
        UpdateBusinessProfileSettingsCommand command,
        PublicStaffDisplayPolicy staffDisplayPolicy)
    {
        List<string> changedFields = [];

        AddIfChanged(changedFields, "displayName", business.DisplayName, command.DisplayName);
        AddIfChanged(changedFields, "description", business.Description, command.Description);
        AddIfChanged(changedFields, "publicRules", business.PublicRules, command.PublicRules);
        AddIfChanged(changedFields, "seoTitle", business.SeoTitle, command.SeoTitle);
        AddIfChanged(changedFields, "seoDescription", business.SeoDescription, command.SeoDescription);

        if (business.PublicStaffDisplayPolicy != staffDisplayPolicy)
        {
            changedFields.Add("staffDisplayPolicy");
        }

        return changedFields.ToArray();
    }

    private static void AddIfChanged(
        List<string> changedFields,
        string field,
        string currentValue,
        string nextValue)
    {
        if (!string.Equals(
            currentValue.Trim(),
            nextValue.Trim(),
            StringComparison.Ordinal))
        {
            changedFields.Add(field);
        }
    }

    private static string CreateAuditDetailsJson(
        Guid tenantId,
        Guid businessId,
        IReadOnlyCollection<string> changedFields)
    {
        string fields = string.Join(
            ",",
            changedFields.Select(field => $"\"{field}\""));

        return $$"""{"tenantId":"{{tenantId}}","businessId":"{{businessId}}","changedFields":[{{fields}}]}""";
    }

    private static BusinessProfileSettingsView ToView(Business business)
    {
        return new BusinessProfileSettingsView(
            business.Id,
            business.Slug,
            business.DisplayName,
            business.CategoryKey,
            business.Description,
            business.PublicRules,
            business.SeoTitle,
            business.SeoDescription,
            business.PublicStaffDisplayPolicy.ToString(),
            business.CancellationCutoffHours);
    }
}
