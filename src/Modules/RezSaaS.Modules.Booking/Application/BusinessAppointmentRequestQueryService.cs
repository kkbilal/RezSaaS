using Microsoft.EntityFrameworkCore;
using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.Booking.Domain;
using RezSaaS.Modules.Booking.Infrastructure.Persistence;

namespace RezSaaS.Modules.Booking.Application;

public sealed class BusinessAppointmentRequestQueryService
{
    private readonly BookingDbContext dbContext;
    private readonly ITenantContextAccessor tenantContextAccessor;

    public BusinessAppointmentRequestQueryService(
        BookingDbContext dbContext,
        ITenantContextAccessor tenantContextAccessor)
    {
        this.dbContext = dbContext;
        this.tenantContextAccessor = tenantContextAccessor;
    }

    public async Task<IReadOnlyCollection<BusinessAppointmentRequestListItemView>> GetPendingAsync(
        Guid? branchId,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        return await GetAsync(
            new BusinessAppointmentRequestQuery(
                branchId,
                AppointmentRequestStatus.PendingApproval.ToString(),
                FromUtc: null,
                ToUtc: null,
                take),
            cancellationToken);
    }

    public async Task<IReadOnlyCollection<BusinessAppointmentRequestListItemView>> GetAsync(
        BusinessAppointmentRequestQuery requestQuery,
        CancellationToken cancellationToken = default)
    {
        if (tenantContextAccessor.TenantId is null)
        {
            return [];
        }

        IQueryable<AppointmentRequest> query = dbContext.AppointmentRequests
            .AsNoTracking()
            .Include(entity => entity.Lines)
            .AsQueryable();

        if (requestQuery.BranchId is not null)
        {
            query = query.Where(entity => entity.BranchId == requestQuery.BranchId);
        }

        if (AppointmentRequestStatusFilter.TryParse(
            requestQuery.Status,
            out AppointmentRequestStatus parsedStatus))
        {
            query = query.Where(entity => entity.Status == parsedStatus);
        }

        if (requestQuery.FromUtc is not null)
        {
            query = query.Where(entity => entity.RequestedStartUtc >= requestQuery.FromUtc);
        }

        if (requestQuery.ToUtc is not null)
        {
            query = query.Where(entity => entity.RequestedStartUtc < requestQuery.ToUtc);
        }

        List<AppointmentRequest> requests = await query
            .OrderBy(entity => entity.RequestedStartUtc)
            .Take(Math.Clamp(requestQuery.Take, 1, 100))
            .ToListAsync(cancellationToken);

        return requests
            .Select(ToListItemView)
            .ToArray();
    }

    public async Task<BusinessAppointmentRequestListItemView?> GetByIdAsync(
        Guid appointmentRequestId,
        CancellationToken cancellationToken = default)
    {
        if (tenantContextAccessor.TenantId is null || appointmentRequestId == Guid.Empty)
        {
            return null;
        }

        AppointmentRequest? request = await dbContext.AppointmentRequests
            .AsNoTracking()
            .Include(entity => entity.Lines)
            .SingleOrDefaultAsync(
                entity => entity.Id == appointmentRequestId,
                cancellationToken);

        return request is null ? null : ToListItemView(request);
    }

    public async Task<BusinessAppointmentRequestAuthorizationContext?> GetAuthorizationContextAsync(
        Guid appointmentRequestId,
        CancellationToken cancellationToken = default)
    {
        if (tenantContextAccessor.TenantId is null)
        {
            return null;
        }

        return await dbContext.AppointmentRequests
            .AsNoTracking()
            .Where(entity => entity.Id == appointmentRequestId)
            .Select(entity => new BusinessAppointmentRequestAuthorizationContext(
                entity.Id,
                entity.BranchId,
                entity.CustomerUserAccountId,
                entity.Status.ToString()))
            .SingleOrDefaultAsync(cancellationToken);
    }

    private static BusinessAppointmentRequestListItemView ToListItemView(AppointmentRequest request)
    {
        return new BusinessAppointmentRequestListItemView(
            request.Id,
            request.CustomerUserAccountId,
            request.BranchId,
            request.StaffMemberId,
            request.ResourceId,
            request.RequestedStartUtc,
            request.RequestedEndUtc,
            request.ExpiresAtUtc,
            request.Status.ToString(),
            request.Lines
                .Select(line => new BusinessAppointmentRequestLineView(
                    line.ServiceVariantId,
                    line.ServiceNameSnapshot,
                    line.DurationMinutes,
                    line.PriceAmount,
                    line.CurrencyCode))
                .ToArray());
    }
}
