namespace RezSaaS.BuildingBlocks.Booking;

/// <summary>
/// Booking modulu tarafindan uygulanan, salt-okunur modul-arasi sozlesme.
/// Organization modulu, bir personeli arsivlemeden ONCE onun gelecekte AKTIF (Confirmed)
/// randevusu olup olmadigini bunun uzerinden sorar.
/// </summary>
/// <remarks>
/// NEDEN BOYLE: Organization modulu Booking modulune DOGRUDAN referans VEREMEZ
/// (ModuleDependencyTests moduller arasi referansi yasakliyor). Sozlesme BuildingBlocks'ta,
/// adapter'i composition root'ta baglanir -- iptal politikasi (IBusinessCancellationPolicyLookup)
/// ve Reviews adapter'leriyle ayni kalip.
///
/// Yalnizca Organization'in ihtiyaci olan tek bilgiyi tasir (var/yok); is kurali sizmaz.
/// </remarks>
public interface IStaffAppointmentGuard
{
    /// <summary>
    /// Personelin, verilen ANDAN sonra baslayan AKTIF (Confirmed) bir randevusu var mi?
    /// </summary>
    Task<bool> HasUpcomingActiveAppointmentsAsync(
        Guid tenantId,
        Guid staffMemberId,
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken = default);
}
