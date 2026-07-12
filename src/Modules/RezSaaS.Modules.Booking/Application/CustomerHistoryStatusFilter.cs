using RezSaaS.Modules.Booking.Domain;

namespace RezSaaS.Modules.Booking.Application;

/// <summary>
/// Musteri randevu gecmisi (<c>GET /api/customer/appointment-history</c>) icin status filtresi.
/// </summary>
/// <remarks>
/// NEDEN AYRI BIR FILTRE VAR:
/// Bu uc TEK bir listede IKI FARKLI aggregate donuyor -- AppointmentRequest ve Appointment --
/// ve bu ikisinin status enum'larinin KESISIMI BOSTUR:
///
///   AppointmentRequestStatus : PendingApproval, Approved, Declined, Expired, Superseded,
///                              CancelledByCustomer
///   AppointmentStatus        : Confirmed, Cancelled, Completed, NoShow, Rebooked
///
/// Uc, status'u yalnizca <see cref="AppointmentRequestStatusFilter"/> ile dogruluyordu.
/// Sonuc: ?status=Confirmed gibi bir RANDEVU statusu 400 ile REDDEDILIYORDU; ?status=PendingApproval
/// gibi bir TALEP statusu ise gecerli sayiliyor ama randevu sorgusu onu tanimadigi icin randevular
/// BOS donuyordu. Yani HANGI DEGERI VERIRSENIZ VERIN randevular gecmiste HIC GORUNMUYORDU.
///
/// Bu bug'i uctan uca duman testi ortaya cikardi -- tip sistemi ve derleyici goremezdi, cunku
/// iki enum da ayri ayri gecerliydi; hata BIRLESIMLERININ dusunulmemis olmasindaydi.
///
/// Dogrusu: iki enum'un BIRLESIMINI kabul et. Her sorgu servisi tanimadigi degeri zaten
/// kendi icinde fail-closed ele aliyor (bos liste doner).
/// </remarks>
public static class CustomerHistoryStatusFilter
{
    public static bool IsValidOrEmpty(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return true;
        }

        return AppointmentRequestStatusFilter.TryParse(status, out _)
            || TryParseAppointmentStatus(status, out _);
    }

    private static bool TryParseAppointmentStatus(
        string? status,
        out AppointmentStatus parsedStatus)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            parsedStatus = default;
            return false;
        }

        return Enum.TryParse(status, ignoreCase: true, out parsedStatus)
            && Enum.IsDefined(parsedStatus);
    }
}
