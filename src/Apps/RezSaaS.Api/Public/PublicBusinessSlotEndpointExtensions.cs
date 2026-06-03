using RezSaaS.Modules.Organization.Application;

namespace RezSaaS.Api.PublicApi;

public static class PublicBusinessSlotEndpointExtensions
{
    public static IEndpointRouteBuilder MapPublicBusinessSlotEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder publicBusinesses = endpoints
            .MapGroup("/api/public/businesses")
            .WithTags("Public Businesses")
            .RequireRateLimiting(OrganizationRateLimitPolicyNames.PublicDiscovery);

        publicBusinesses.MapGet(
            "/{slug}/slots",
            async (
                string slug,
                string? branchSlug,
                string? serviceVariantIds,
                DateOnly? date,
                Guid? staffMemberId,
                PublicSlotSearchComposer composer,
                CancellationToken cancellationToken) =>
            {
                if (!TryCreateRequest(
                    branchSlug,
                    serviceVariantIds,
                    date,
                    staffMemberId,
                    out PublicSlotSearchRequest? request,
                    out PublicSlotSearchValidationResponse? validationResponse))
                {
                    return Results.BadRequest(validationResponse);
                }

                PublicSlotSearchResponse? response = await composer.SearchAsync(
                    slug,
                    request!,
                    cancellationToken);

                return response is null
                    ? Results.NotFound()
                    : Results.Ok(response);
            });

        return endpoints;
    }

    private static bool TryCreateRequest(
        string? branchSlug,
        string? serviceVariantIds,
        DateOnly? date,
        Guid? staffMemberId,
        out PublicSlotSearchRequest? request,
        out PublicSlotSearchValidationResponse? validationResponse)
    {
        List<string> errors = [];
        Guid[] parsedServiceVariantIds = [];

        if (string.IsNullOrWhiteSpace(branchSlug))
        {
            errors.Add("branchSlug is required.");
        }

        if (date is null)
        {
            errors.Add("date is required.");
        }

        if (!TryParseServiceVariantIds(serviceVariantIds, out parsedServiceVariantIds))
        {
            errors.Add("serviceVariantIds must contain at least one valid GUID.");
        }

        if (errors.Count > 0)
        {
            request = null;
            validationResponse = new PublicSlotSearchValidationResponse(errors);
            return false;
        }

        request = new PublicSlotSearchRequest(
            branchSlug!.Trim(),
            date!.Value,
            parsedServiceVariantIds,
            staffMemberId);
        validationResponse = null;
        return true;
    }

    private static bool TryParseServiceVariantIds(
        string? rawValue,
        out Guid[] serviceVariantIds)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            serviceVariantIds = [];
            return false;
        }

        List<Guid> parsedIds = [];
        string[] parts = rawValue.Split(
            ',',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (string part in parts)
        {
            if (!Guid.TryParse(part, out Guid parsedId))
            {
                serviceVariantIds = [];
                return false;
            }

            parsedIds.Add(parsedId);
        }

        serviceVariantIds = parsedIds
            .Distinct()
            .ToArray();
        return serviceVariantIds.Length > 0;
    }
}
