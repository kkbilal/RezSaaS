using Microsoft.EntityFrameworkCore;
using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.Booking.Domain;
using RezSaaS.Modules.Booking.Infrastructure.Persistence;

namespace RezSaaS.Modules.Booking.Application;

public sealed class ConfirmedAppointmentQueryService
{
    private readonly BookingDbContext dbContext;
    private readonly ITenantContextAccessor tenantContextAccessor;

    public ConfirmedAppointmentQueryService(
        BookingDbContext dbContext,
        ITenantContextAccessor tenantContextAccessor)
    {
        this.dbContext = dbContext;
        this.tenantContextAccessor = tenantContextAccessor;
    }

    public async Task<IReadOnlyCollection<ConfirmedAppointmentBusyTimeView>> GetBusyTimesAsync(
        Guid branchId,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken = default)
    {
        if (tenantContextAccessor.TenantId is null)
        {
            return [];
        }

        return await dbContext.Appointments
            .AsNoTracking()
            .Where(entity => entity.BranchId == branchId
                && entity.Status == AppointmentStatus.Confirmed
                && entity.StartUtc < toUtc
                && entity.EndUtc > fromUtc)
            .OrderBy(entity => entity.StartUtc)
            .Select(entity => new ConfirmedAppointmentBusyTimeView(
                entity.StaffMemberId,
                entity.ResourceId,
                entity.StartUtc,
                entity.EndUtc))
            .ToListAsync(cancellationToken);
    }

    /// <param name="status">
    /// Randevu statusu filtresi (Confirmed | Cancelled | Completed | NoShow | Rebooked).
    /// Bos/null ise filtre uygulanmaz. Taninmayan bir deger gelirse BOS liste doner --
    /// "taninmadi, o halde hepsini gonder" demek fail-open olurdu.
    /// </param>
    /// <remarks>
    /// BUG FIX: Bu parametre EKSIKTI. CustomerAppointmentHistoryComposer status'u yalnizca
    /// TALEPLERE uyguluyordu; randevulara HIC uygulanmiyordu. Yani ?status=PendingApproval
    /// gonderilse bile TUM randevular donuyordu ve /hesabim sekmeleri YANLIS VERI gosteriyordu.
    /// </remarks>
    public async Task<IReadOnlyCollection<CustomerConfirmedAppointmentView>> GetOwnAsync(
        Guid customerUserAccountId,
        Guid[] branchIds,
        string? status = null,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        if (tenantContextAccessor.TenantId is null
            || customerUserAccountId == Guid.Empty
            || branchIds.Length == 0)
        {
            return [];
        }

        AppointmentStatus? statusFilter = null;

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse(status, ignoreCase: true, out AppointmentStatus parsed)
                || !Enum.IsDefined(parsed))
            {
                // Taninmayan status -> BOS liste. Filtreyi sessizce yok saymak, kullaniciya
                // istemedigi kayitlari gostermek demek olurdu.
                return [];
            }

            statusFilter = parsed;
        }

        List<Appointment> appointments = await dbContext.Appointments
            .AsNoTracking()
            .Include(entity => entity.Lines)
            .Where(entity => entity.CustomerUserAccountId == customerUserAccountId
                && branchIds.Contains(entity.BranchId)
                && (statusFilter == null || entity.Status == statusFilter))
            .OrderByDescending(entity => entity.StartUtc)
            .Take(Math.Clamp(take, 1, 100))
            .ToListAsync(cancellationToken);

        return appointments
            .Select(entity => new CustomerConfirmedAppointmentView(
                entity.Id,
                entity.AppointmentRequestId,
                entity.BranchId,
                entity.StaffMemberId,
                entity.ResourceId,
                entity.StartUtc,
                entity.EndUtc,
                entity.Status.ToString(),
                entity.Lines
                    .Select(line => new CustomerConfirmedAppointmentLineView(
                        line.ServiceVariantId,
                        line.ServiceNameSnapshot,
                        line.DurationMinutes,
                        line.PriceAmount,
                        line.CurrencyCode))
                    .ToArray()))
            .ToArray();
    }
}
