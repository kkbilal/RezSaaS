using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.Availability.Application;
using RezSaaS.Modules.Catalog.Application;
using RezSaaS.Modules.Organization.Application;
using RezSaaS.Modules.TenantManagement.Application;

namespace RezSaaS.Api.PublicApi;

public sealed class PublicBusinessProfileComposer
{
    private readonly AvailabilityQueryService availabilityQueryService;
    private readonly PublicBusinessDirectoryService businessDirectoryService;
    private readonly PublicCatalogMenuService catalogMenuService;
    private readonly TenantLifecycleQueryService tenantLifecycleQueryService;
    private readonly ITenantContextAccessor tenantContextAccessor;

    public PublicBusinessProfileComposer(
        PublicBusinessDirectoryService businessDirectoryService,
        PublicCatalogMenuService catalogMenuService,
        AvailabilityQueryService availabilityQueryService,
        TenantLifecycleQueryService tenantLifecycleQueryService,
        ITenantContextAccessor tenantContextAccessor)
    {
        this.businessDirectoryService = businessDirectoryService;
        this.catalogMenuService = catalogMenuService;
        this.availabilityQueryService = availabilityQueryService;
        this.tenantLifecycleQueryService = tenantLifecycleQueryService;
        this.tenantContextAccessor = tenantContextAccessor;
    }

    public async Task<PublicBusinessProfileResponse?> GetProfileAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        PublicBusinessCompositionContext? business =
            await businessDirectoryService.GetCompositionContextBySlugAsync(
                slug,
                cancellationToken);

        if (business is null
            || !await tenantLifecycleQueryService.IsActiveAsync(
                business.TenantId,
                cancellationToken))
        {
            return null;
        }

        Guid? previousTenantId = tenantContextAccessor.TenantId;
        tenantContextAccessor.TenantId = business.TenantId;

        try
        {
            IReadOnlyCollection<PublicServiceMenuView> services =
                await catalogMenuService.GetMenuAsync(cancellationToken);
            List<PublicBusinessBranchProfileResponse> branches = [];

            foreach (PublicBusinessBranchContext branch in business.Branches)
            {
                IReadOnlyCollection<BranchWorkingHoursView> workingHours =
                    await availabilityQueryService.GetBranchWorkingHoursAsync(
                        branch.Id,
                        cancellationToken);
                bool showStaffNames = string.Equals(
                    business.StaffDisplayPolicy,
                    "ShowNames",
                    StringComparison.OrdinalIgnoreCase);

                branches.Add(new PublicBusinessBranchProfileResponse(
                    branch.Slug,
                    branch.DisplayName,
                    branch.TimeZoneId,
                        branch.City,
                        branch.District,
                        branch.AddressLine,
                        showStaffNames
                            ? branch.StaffMembers
                                .Select(staffMember => new PublicStaffMemberProfileResponse(
                                    staffMember.Id,
                                    staffMember.DisplayName,
                                    staffMember.SkillIds))
                                .ToArray()
                            : [],
                        workingHours
                            .Select(hours => new PublicBranchWorkingHoursProfileResponse(
                                hours.DayOfWeek.ToString(),
                                hours.OpensAt,
                                hours.ClosesAt,
                                hours.IsClosed))
                            .ToArray()));
            }

            return new PublicBusinessProfileResponse(
                business.Slug,
                business.DisplayName,
                business.CategoryKey,
                business.Description,
                new PublicBusinessProfileMetadataResponse(
                    business.PublicRules,
                    business.SeoTitle,
                    business.SeoDescription,
                    business.StaffDisplayPolicy,
                    business.RatingAverage,
                    business.ReviewCount,
                    business.GalleryImages
                        .Select(image => new PublicBusinessGalleryImageProfileResponse(
                            image.ImageUrl,
                            image.AltText,
                            image.SortOrder))
                        .ToArray()),
                branches,
                services
                    .Select(service => new PublicServiceProfileResponse(
                        service.Id,
                        service.Name,
                        service.CategoryKey,
                        service.Variants
                            .Select(variant => new PublicServiceVariantProfileResponse(
                                variant.Id,
                                variant.Name,
                                variant.DurationMinutes,
                                variant.PriceAmount,
                                variant.CurrencyCode,
                                variant.RequiredResourceTypeId))
                            .ToArray()))
                    .ToArray());
        }
        finally
        {
            tenantContextAccessor.TenantId = previousTenantId;
        }
    }
}
