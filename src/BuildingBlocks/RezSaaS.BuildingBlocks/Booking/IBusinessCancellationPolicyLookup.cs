namespace RezSaaS.BuildingBlocks.Booking;

/// <summary>
/// Organization modulu tarafindan uygulanan, salt-okunur modul-arasi sozlesme.
/// Booking modulu, musteri iptalinde isletmenin iptal politikasini bunun uzerinden okur.
/// </summary>
/// <remarks>
/// NEDEN BOYLE: Booking modulu Organization modulune DOGRUDAN referans VEREMEZ
/// (ModuleDependencyTests bunu zorluyor: moduller birbirini tanimaz). Sozlesme
/// BuildingBlocks'ta durur, adapter'i composition root (RezSaaS.Api) baglar --
/// Reviews modulunun ICompletedAppointmentLookup deseniyle ayni.
///
/// Yalnizca Booking'in ihtiyaci olan veriyi tasir; is kurali sizmaz.
/// </remarks>
public interface IBusinessCancellationPolicyLookup
{
    /// <summary>
    /// Tenant'in iptal politikasini doner. Isletme bulunamazsa <c>null</c>.
    /// </summary>
    Task<BusinessCancellationPolicy?> GetAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);
}

/// <param name="CancellationCutoffHours">
/// Randevu saatine bu kadar saatten az kaldiysa musteri iptal EDEMEZ.
/// 0 = her zaman iptal edilebilir.
/// </param>
public sealed record BusinessCancellationPolicy(int CancellationCutoffHours);
