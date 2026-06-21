using Microsoft.EntityFrameworkCore;
using RezSaaS.BuildingBlocks.Reviews;
using RezSaaS.Modules.Booking.Domain;
using RezSaaS.Modules.Booking.Infrastructure.Persistence;
using RezSaaS.Modules.Organization.Domain;
using RezSaaS.Modules.Organization.Infrastructure.Persistence;

namespace RezSaaS.Api.Reviews;

/// <summary>
/// Composition-root adapter that implements the Reviews cross-module contract
/// <see cref="ICompletedAppointmentLookup"/> by reading from the Booking module's
/// <see cref="BookingDbContext"/> (no writes) and resolving the business id via
/// the Organization module's branch mapping.
/// </summary>
public sealed class BookingCompletedAppointmentLookupAdapter : ICompletedAppointmentLookup
{
    private readonly BookingDbContext bookingDbContext;
    private readonly OrganizationDbContext organizationDbContext;

    public BookingCompletedAppointmentLookupAdapter(
        BookingDbContext bookingDbContext,
        OrganizationDbContext organizationDbContext)
    {
        this.bookingDbContext = bookingDbContext;
        this.organizationDbContext = organizationDbContext;
    }

    public async Task<CompletedAppointmentSnapshot?> GetAsync(
        Guid tenantId,
        Guid appointmentId,
        Guid customerUserAccountId,
        CancellationToken cancellationToken = default)
    {
        var appointment = await bookingDbContext.Appointments
            .AsNoTracking()
            .Where(appointment => appointment.TenantId == tenantId
                && appointment.Id == appointmentId
                && appointment.CustomerUserAccountId == customerUserAccountId)
            .Select(appointment => new
            {
                appointment.BranchId,
                appointment.Status,
                appointment.CompletedAtUtc,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (appointment is null)
        {
            return null;
        }

        if (appointment.Status != AppointmentStatus.Completed)
        {
            return new CompletedAppointmentSnapshot(
                AppointmentId: appointmentId,
                BusinessId: Guid.Empty,
                BranchId: appointment.BranchId,
                CustomerUserAccountId: customerUserAccountId,
                CompletedAtUtc: DateTimeOffset.MinValue,
                ServiceNameSnapshots: Array.Empty<string>());
        }

        Guid businessId = await organizationDbContext.Branches
            .AsNoTracking()
            .Where(branch => branch.TenantId == tenantId && branch.Id == appointment.BranchId)
            .Select(branch => branch.BusinessId)
            .FirstOrDefaultAsync(cancellationToken);

        if (businessId == Guid.Empty)
        {
            return null;
        }

        List<string> serviceNameSnapshots = await bookingDbContext.AppointmentLines
            .AsNoTracking()
            .Where(line => line.AppointmentId == appointmentId)
            .Select(line => line.ServiceNameSnapshot)
            .ToListAsync(cancellationToken);

        return new CompletedAppointmentSnapshot(
            AppointmentId: appointmentId,
            BusinessId: businessId,
            BranchId: appointment.BranchId,
            CustomerUserAccountId: customerUserAccountId,
            CompletedAtUtc: appointment.CompletedAtUtc ?? DateTimeOffset.MinValue,
            ServiceNameSnapshots: serviceNameSnapshots);
    }
}