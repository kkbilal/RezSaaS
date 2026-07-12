using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RezSaaS.Modules.Organization.Domain;
using RezSaaS.Modules.Organization.Infrastructure.Persistence;

namespace RezSaaS.Modules.Organization.Application;

public sealed class PublicBusinessDirectoryService
{
    private readonly OrganizationDbContext dbContext;
    private readonly IOptions<PublicBusinessDirectoryOptions> options;

    public PublicBusinessDirectoryService(
        OrganizationDbContext dbContext,
        IOptions<PublicBusinessDirectoryOptions> options)
    {
        this.dbContext = dbContext;
        this.options = options;
    }

    public async Task<IReadOnlyCollection<PublicBusinessSummaryView>> SearchAsync(
        PublicBusinessSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        PublicBusinessDirectoryOptions directoryOptions = options.Value;
        int take = Math.Clamp(
            query.Take ?? directoryOptions.DefaultTake,
            1,
            directoryOptions.MaxTake);
        string? searchText = NormalizeOptional(query.SearchText);
        string? categoryKey = NormalizeOptional(query.CategoryKey);
        string? normalizedCity = NormalizeOptional(query.City)?.ToUpperInvariant();
        string? normalizedDistrict = NormalizeOptional(query.District)?.ToUpperInvariant();

        IQueryable<Business> businesses = dbContext.Businesses
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(entity => entity.Status == BusinessStatus.Active);

        if (categoryKey is not null)
        {
            businesses = businesses.Where(entity => entity.CategoryKey == categoryKey);
        }

        if (searchText is not null)
        {
            businesses = businesses.Where(entity =>
                EF.Functions.ILike(entity.DisplayName, $"%{searchText}%")
                || EF.Functions.ILike(entity.CategoryKey, $"%{searchText}%"));
        }

        if (normalizedCity is not null)
        {
            businesses = businesses.Where(entity => dbContext.Branches
                .IgnoreQueryFilters()
                .Any(branch =>
                    branch.TenantId == entity.TenantId
                    && branch.BusinessId == entity.Id
                    && branch.NormalizedCity == normalizedCity));
        }

        if (normalizedDistrict is not null)
        {
            businesses = businesses.Where(entity => dbContext.Branches
                .IgnoreQueryFilters()
                .Any(branch =>
                    branch.TenantId == entity.TenantId
                    && branch.BusinessId == entity.Id
                    && branch.NormalizedDistrict == normalizedDistrict));
        }

        return await businesses
            .OrderBy(entity => entity.DisplayName)
            .Take(take)
            .Select(entity => new PublicBusinessSummaryView(
                entity.TenantId,
                entity.Slug,
                entity.DisplayName,
                entity.CategoryKey,
                dbContext.Branches
                    .IgnoreQueryFilters()
                    .Where(branch => branch.TenantId == entity.TenantId && branch.BusinessId == entity.Id)
                    .OrderBy(branch => branch.DisplayName)
                    .Select(branch => branch.City)
                    .FirstOrDefault() ?? string.Empty,
                dbContext.Branches
                    .IgnoreQueryFilters()
                    .Where(branch => branch.TenantId == entity.TenantId && branch.BusinessId == entity.Id)
                    .OrderBy(branch => branch.DisplayName)
                    .Select(branch => branch.District)
                    .FirstOrDefault() ?? string.Empty))
            .ToListAsync(cancellationToken);
    }

    public async Task<PublicBusinessProfileView?> GetBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        string normalizedSlug = NormalizeRequired(slug, nameof(slug)).ToUpperInvariant();

        Business? business = await dbContext.Businesses
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                entity => entity.NormalizedSlug == normalizedSlug
                    && entity.Status == BusinessStatus.Active,
                cancellationToken);

        if (business is null)
        {
            return null;
        }

        List<PublicBusinessBranchView> branches = await dbContext.Branches
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(entity => entity.TenantId == business.TenantId && entity.BusinessId == business.Id)
            .OrderBy(entity => entity.DisplayName)
            .Select(entity => new PublicBusinessBranchView(
                entity.Slug,
                entity.DisplayName,
                entity.TimeZoneId,
                entity.City,
                entity.District,
                entity.AddressLine))
            .ToListAsync(cancellationToken);

        return new PublicBusinessProfileView(
            business.Slug,
            business.DisplayName,
            business.CategoryKey,
            business.Description,
            branches);
    }

    public async Task<PublicBusinessCompositionContext?> GetCompositionContextBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        string normalizedSlug = NormalizeRequired(slug, nameof(slug)).ToUpperInvariant();

        Business? business = await dbContext.Businesses
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                entity => entity.NormalizedSlug == normalizedSlug
                    && entity.Status == BusinessStatus.Active,
                cancellationToken);

        if (business is null)
        {
            return null;
        }

        List<PublicBusinessBranchContextSeed> branchSeeds = await dbContext.Branches
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(entity => entity.TenantId == business.TenantId && entity.BusinessId == business.Id)
            .OrderBy(entity => entity.DisplayName)
            .Select(entity => new PublicBusinessBranchContextSeed(
                entity.Id,
                entity.Slug,
                entity.DisplayName,
                entity.TimeZoneId,
                entity.City,
                entity.District,
                entity.AddressLine,
                entity.SlotIntervalMinutes,
                entity.MaxPublicSlots))
            .ToListAsync(cancellationToken);
        Guid[] branchIds = branchSeeds
            .Select(entity => entity.Id)
            .ToArray();
        List<PublicStaffMemberContextSeed> staffMembers = await dbContext.StaffMembers
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(entity => entity.TenantId == business.TenantId
                && branchIds.Contains(entity.BranchId)
                && entity.Status == StaffMemberStatus.Active)
            .OrderBy(entity => entity.DisplayName)
            .Select(entity => new PublicStaffMemberContextSeed(
                entity.BranchId,
                entity.Id,
                entity.DisplayName))
            .ToListAsync(cancellationToken);
        Guid[] staffMemberIds = staffMembers
            .Select(entity => entity.Id)
            .ToArray();
        List<PublicStaffSkillContextSeed> staffSkills = await dbContext.StaffSkills
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(entity => entity.TenantId == business.TenantId
                && staffMemberIds.Contains(entity.StaffMemberId))
            .Select(entity => new PublicStaffSkillContextSeed(
                entity.StaffMemberId,
                entity.SkillId))
            .ToListAsync(cancellationToken);
        ILookup<Guid, Guid> skillIdsByStaffMemberId = staffSkills
            .ToLookup(
                entity => entity.StaffMemberId,
                entity => entity.SkillId);
        ILookup<Guid, PublicStaffMemberView> staffMembersByBranchId = staffMembers
            .ToLookup(
                entity => entity.BranchId,
                entity => new PublicStaffMemberView(
                    entity.Id,
                    entity.DisplayName,
                    skillIdsByStaffMemberId[entity.Id].ToArray()));
        List<PublicBusinessGalleryImageView> galleryImages = await dbContext.BusinessGalleryImages
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(entity => entity.TenantId == business.TenantId
                && entity.BusinessId == business.Id
                && entity.IsPublished)
            .OrderBy(entity => entity.SortOrder)
            .Select(entity => new PublicBusinessGalleryImageView(
                entity.ImageUrl,
                entity.AltText,
                entity.SortOrder))
            .ToListAsync(cancellationToken);
        PublicBusinessBranchContext[] branches = branchSeeds
            .Select(branch => new PublicBusinessBranchContext(
                branch.Id,
                branch.Slug,
                branch.DisplayName,
                branch.TimeZoneId,
                branch.City,
                branch.District,
                branch.AddressLine,
                branch.SlotIntervalMinutes,
                branch.MaxPublicSlots,
                staffMembersByBranchId[branch.Id].ToArray()))
            .ToArray();

        return new PublicBusinessCompositionContext(
            business.TenantId,
            business.Id,
            business.Slug,
            business.DisplayName,
            business.CategoryKey,
            business.Description,
            business.PublicRules,
            business.SeoTitle,
            business.SeoDescription,
            business.PublicStaffDisplayPolicy.ToString(),
            business.RatingAverage,
            business.ReviewCount,
            galleryImages,
            branches);
    }

    private sealed record PublicBusinessBranchContextSeed(
        Guid Id,
        string Slug,
        string DisplayName,
        string TimeZoneId,
        string City,
        string District,
        string AddressLine,
        int? SlotIntervalMinutes,
        int? MaxPublicSlots);

    private sealed record PublicStaffMemberContextSeed(
        Guid BranchId,
        Guid Id,
        string DisplayName);

    private sealed record PublicStaffSkillContextSeed(
        Guid StaffMemberId,
        Guid SkillId);

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string NormalizeRequired(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        return value.Trim();
    }
}
