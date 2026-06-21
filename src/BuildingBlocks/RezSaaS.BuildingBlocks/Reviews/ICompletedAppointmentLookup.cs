namespace RezSaaS.BuildingBlocks.Reviews;

/// <summary>
/// Read-only cross-module contract implemented by the Booking module.
/// Used by the Reviews module to verify that an appointment is completed
/// and belongs to the given customer before allowing a review.
/// </summary>
public interface ICompletedAppointmentLookup
{
    /// <summary>
    /// Returns completed appointment details for the given tenant/appointment/customer,
    /// or <c>null</c> if the appointment does not exist, is not completed, or belongs to
    /// another tenant/customer.
    /// </summary>
    Task<CompletedAppointmentSnapshot?> GetAsync(
        Guid tenantId,
        Guid appointmentId,
        Guid customerUserAccountId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Snapshot returned by <see cref="ICompletedAppointmentLookup.GetAsync"/>.
/// Contains only the data Reviews needs; no business rule leakage.
/// </summary>
public sealed record CompletedAppointmentSnapshot(
    Guid AppointmentId,
    Guid BusinessId,
    Guid BranchId,
    Guid CustomerUserAccountId,
    DateTimeOffset CompletedAtUtc,
    IReadOnlyCollection<string> ServiceNameSnapshots);