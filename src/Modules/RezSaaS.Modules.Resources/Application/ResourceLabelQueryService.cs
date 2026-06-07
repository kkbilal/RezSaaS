using Microsoft.EntityFrameworkCore;
using RezSaaS.Modules.Resources.Infrastructure.Persistence;

namespace RezSaaS.Modules.Resources.Application;

public sealed class ResourceLabelQueryService
{
    private readonly ResourcesDbContext dbContext;

    public ResourceLabelQueryService(ResourcesDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<IReadOnlyDictionary<Guid, ResourceLabelView>> GetResourceLabelsAsync(
        IReadOnlyCollection<Guid> resourceIds,
        CancellationToken cancellationToken = default)
    {
        if (resourceIds.Count == 0)
        {
            return new Dictionary<Guid, ResourceLabelView>();
        }

        Guid[] distinctResourceIds = resourceIds
            .Where(entity => entity != Guid.Empty)
            .Distinct()
            .ToArray();

        return await dbContext.Resources
            .AsNoTracking()
            .Where(entity => distinctResourceIds.Contains(entity.Id))
            .Select(entity => new ResourceLabelView(
                entity.Id,
                entity.DisplayName))
            .ToDictionaryAsync(entity => entity.Id, cancellationToken);
    }
}
