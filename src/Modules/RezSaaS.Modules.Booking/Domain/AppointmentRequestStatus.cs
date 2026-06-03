namespace RezSaaS.Modules.Booking.Domain;

public enum AppointmentRequestStatus
{
    PendingApproval,
    Approved,
    Declined,
    Expired,
    Superseded,
    CancelledByCustomer,
}
