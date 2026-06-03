using Microsoft.EntityFrameworkCore;
using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.Booking.Domain;
using RezSaaS.Modules.Booking.Infrastructure.Persistence;

namespace RezSaaS.Modules.Booking.Application;

public sealed class CustomerAppointmentRequestQueryService
{
    private readonly BookingDbContext dbContext;
    private readonly ITenantContextAccessor tenantContextAccessor;

    public CustomerAppointmentRequestQueryService(
        BookingDbContext dbContext,
        ITenantContextAccessor tenantContextAccessor)
    {
        this.dbContext = dbContext;
        this.tenantContextAccessor = tenantContextAccessor;
    }

    public async Task<IReadOnlyCollection<CustomerAppointmentRequestView>> GetOwnAsync(
        Guid customerUserAccountId,
        Guid[] branchIds,
        string? status,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        if (tenantContextAccessor.TenantId is null
            || customerUserAccountId == Guid.Empty
            || branchIds.Length == 0)
        {
            return [];
        }

        IQueryable<AppointmentRequest> query = dbContext.AppointmentRequests
            .AsNoTracking()
            .Include(entity => entity.Lines)
            .Where(entity => entity.CustomerUserAccountId == customerUserAccountId
                && branchIds.Contains(entity.BranchId));

        if (AppointmentRequestStatusFilter.TryParse(status, out AppointmentRequestStatus parsedStatus))
        {
            query = query.Where(entity => entity.Status == parsedStatus);
        }

        List<AppointmentRequest> requests = await query
            .OrderByDescending(entity => entity.CreatedAtUtc)
            .Take(Math.Clamp(take, 1, 100))
            .ToListAsync(cancellationToken);

        return requests
            .Select(ToView)
            .ToArray();
    }

    public async Task<CustomerAppointmentRequestView?> GetOwnByIdAsync(
        Guid customerUserAccountId,
        Guid appointmentRequestId,
        Guid[] branchIds,
        CancellationToken cancellationToken = default)
    {
        if (tenantContextAccessor.TenantId is null
            || customerUserAccountId == Guid.Empty
            || appointmentRequestId == Guid.Empty
            || branchIds.Length == 0)
        {
            return null;
        }

        AppointmentRequest? request = await dbContext.AppointmentRequests
            .AsNoTracking()
            .Include(entity => entity.Lines)
            .Where(entity => entity.CustomerUserAccountId == customerUserAccountId
                && entity.Id == appointmentRequestId
                && branchIds.Contains(entity.BranchId))
            .SingleOrDefaultAsync(cancellationToken);

        return request is null ? null : ToView(request);
    }

    private static CustomerAppointmentRequestView ToView(AppointmentRequest request)
    {
        return new CustomerAppointmentRequestView(
            request.Id,
            request.BranchId,
            request.StaffMemberId,
            request.ResourceId,
            request.RequestedStartUtc,
            request.RequestedEndUtc,
            request.ExpiresAtUtc,
            request.Status.ToString(),
            request.Lines
                .Select(line => new CustomerAppointmentRequestLineView(
                    line.ServiceVariantId,
                    line.ServiceNameSnapshot,
                    line.DurationMinutes,
                    line.PriceAmount,
                    line.CurrencyCode))
                .ToArray());
    }
}
