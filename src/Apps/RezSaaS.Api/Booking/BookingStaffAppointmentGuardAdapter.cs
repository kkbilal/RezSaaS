using Microsoft.EntityFrameworkCore;
using RezSaaS.BuildingBlocks.Booking;
using RezSaaS.Modules.Booking.Domain;
using RezSaaS.Modules.Booking.Infrastructure.Persistence;

namespace RezSaaS.Api.Booking;

/// <summary>
/// Composition-root adapter: Organization'in <see cref="IStaffAppointmentGuard"/> sozlesmesini
/// Booking modulunun Appointments tablosundan (salt-okunur) besler.
/// </summary>
/// <remarks>
/// Organization, Booking'e dogrudan referans veremez (ModuleDependencyTests). Sozlesme
/// BuildingBlocks'ta, uygulamasi burada -- Reviews ve iptal-politikasi adapter'leriyle ayni kalip.
/// </remarks>
public sealed class BookingStaffAppointmentGuardAdapter : IStaffAppointmentGuard
{
    private readonly BookingDbContext bookingDbContext;

    public BookingStaffAppointmentGuardAdapter(BookingDbContext bookingDbContext)
    {
        this.bookingDbContext = bookingDbContext;
    }

    public async Task<bool> HasUpcomingActiveAppointmentsAsync(
        Guid tenantId,
        Guid staffMemberId,
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken = default)
    {
        // IgnoreQueryFilters: bu cagri isletme (owner) baglaminda yapiliyor ama Booking'in
        // tenant context'i bu akista set edilmemis olabilir. Tenant'i ACIKCA filtreliyoruz.
        //
        // Sadece Confirmed randevular engel. Cancelled/Completed/NoShow/Rebooked bir personelin
        // arsivlenmesini engellememeli -- onlar gecmis/kapali. StartUtc >= now: sadece
        // GELECEKTEKI randevular. (Devam eden bir randevu icin de StartUtc gecmiste olabilir;
        // ama pratik esik "bugunden sonrasi" -- gecmisteki confirmed randevular zaten yasandi.)
        return await bookingDbContext.Appointments
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(
                appointment => appointment.TenantId == tenantId
                    && appointment.StaffMemberId == staffMemberId
                    && appointment.Status == AppointmentStatus.Confirmed
                    && appointment.StartUtc >= asOfUtc,
                cancellationToken);
    }
}
