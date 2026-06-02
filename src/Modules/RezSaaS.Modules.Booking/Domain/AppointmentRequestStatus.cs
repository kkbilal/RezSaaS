namespace RezSaaS.Modules.Booking.Domain;

public enum AppointmentRequestStatus
{
    PendingApproval,
    Declined,
    Expired,
    Superseded,
    CancelledByCustomer,
}
