using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RezSaaS.BuildingBlocks.Modularity;
using RezSaaS.Modules.Reviews.Application;
using RezSaaS.Modules.Reviews.Infrastructure.Persistence;

namespace RezSaaS.Modules.Reviews;

public sealed class ReviewsModule : ModuleBase
{
    public override string Name => "Reviews";

    public override void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString(ReviewsDbContext.ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{ReviewsDbContext.ConnectionStringName}' is required.");

        services.AddDbContext<ReviewsDbContext>(
            options => options.UseNpgsql(connectionString));

        services.AddScoped<CreateReviewService>();
        services.AddScoped<ModerateReviewService>();
        services.AddScoped<PublicReviewQueryService>();
        services.AddScoped<BusinessReviewQueryService>();
    }
}