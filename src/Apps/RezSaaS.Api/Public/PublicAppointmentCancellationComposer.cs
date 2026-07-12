using System.Security.Claims;
using System.Text;
using RezSaaS.Api.Idempotency;
using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.Booking.Application;
using RezSaaS.Modules.Organization.Application;

namespace RezSaaS.Api.PublicApi;

/// <summary>
/// Musterinin KENDI onaylanmis randevusunu iptal etmesi (public yuzey).
/// </summary>
/// <remarks>
/// Neden /api/public/businesses/{slug}/... altinda:
/// slug, tenant cozumunu saglar. /api/customer/* altinda slug YOK -- oradan yapmak
/// cross-tenant lookup gerektirirdi. Mevcut TALEP iptali de tam olarak bu desende
/// (bkz. PublicAppointmentRequestComposer.CancelOwnAsync).
/// </remarks>
public sealed class PublicAppointmentCancellationComposer
{
    private const string InvalidRequest = "APPOINTMENT_CANCEL_INVALID";
    private const string NotFound = "APPOINTMENT_NOT_FOUND";
    private const string Unauthorized = "APPOINTMENT_CANCEL_UNAUTHORIZED";

    private readonly CancelAppointmentByCustomerService cancelService;
    private readonly PublicBusinessDirectoryService businessDirectoryService;
    private readonly ITenantContextAccessor tenantContextAccessor;

    public PublicAppointmentCancellationComposer(
        CancelAppointmentByCustomerService cancelService,
        PublicBusinessDirectoryService businessDirectoryService,
        ITenantContextAccessor tenantContextAccessor)
    {
        this.cancelService = cancelService;
        this.businessDirectoryService = businessDirectoryService;
        this.tenantContextAccessor = tenantContextAccessor;
    }

    public async Task<PublicAppointmentCancellationResult> CancelOwnAsync(
        string businessSlug,
        Guid appointmentId,
        string? idempotencyKey,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserAccountId(user, out Guid customerUserAccountId))
        {
            return PublicAppointmentCancellationResult.Failure(
                PublicAppointmentRequestAccessOutcome.Unauthorized,
                Unauthorized);
        }

        PublicBusinessCompositionContext? business =
            await businessDirectoryService.GetCompositionContextBySlugAsync(
                businessSlug,
                cancellationToken);

        if (business is null)
        {
            return PublicAppointmentCancellationResult.Failure(
                PublicAppointmentRequestAccessOutcome.NotFound,
                NotFound);
        }

        if (!BookingIdempotencyContextFactory.TryCreate(
            idempotencyKey,
            CreateIdempotencyMaterial(businessSlug, appointmentId),
            out BookingIdempotencyContext? idempotency,
            out string? idempotencyErrorCode))
        {
            return PublicAppointmentCancellationResult.Failure(
                PublicAppointmentRequestAccessOutcome.BadRequest,
                idempotencyErrorCode!);
        }

        Guid? previousTenantId = tenantContextAccessor.TenantId;
        tenantContextAccessor.TenantId = business.TenantId;

        try
        {
            CustomerAppointmentCancellationResult result =
                await cancelService.CancelAsync(
                    appointmentId,
                    customerUserAccountId,
                    idempotency,
                    cancellationToken);

            return result.Succeeded
                ? PublicAppointmentCancellationResult.Success(
                    new PublicAppointmentCancellationResponse(
                        result.AppointmentId!.Value,
                        "Cancelled"))
                : MapFailure(result.ErrorCode ?? InvalidRequest, result.CancellationCutoffHours);
        }
        finally
        {
            tenantContextAccessor.TenantId = previousTenantId;
        }
    }

    private static PublicAppointmentCancellationResult MapFailure(
        string errorCode,
        int? cancellationCutoffHours)
    {
        PublicAppointmentRequestAccessOutcome outcome = errorCode switch
        {
            "APPOINTMENT_NOT_FOUND" => PublicAppointmentRequestAccessOutcome.NotFound,

            // 409 Conflict: kaynagin MEVCUT DURUMU islemi engelliyor.
            // APPOINTMENT_CANCEL_TOO_LATE de buraya girer -- istek gecerli, ama isletmenin
            // iptal politikasi bu randevu icin artik gecerli degil.
            "APPOINTMENT_ALREADY_CLOSED"
                or "APPOINTMENT_CANCEL_TOO_LATE"
                or "IDEMPOTENCY_KEY_REUSED" => PublicAppointmentRequestAccessOutcome.Conflict,

            "MISSING_TENANT_CONTEXT" => PublicAppointmentRequestAccessOutcome.BadRequest,
            _ => PublicAppointmentRequestAccessOutcome.Unprocessable,
        };

        return PublicAppointmentCancellationResult.Failure(
            outcome,
            errorCode,
            cancellationCutoffHours);
    }

    private static string CreateIdempotencyMaterial(string businessSlug, Guid appointmentId)
    {
        StringBuilder builder = new();
        builder.Append("appointment-cancel|");
        builder.Append(businessSlug.Trim().ToLowerInvariant());
        builder.Append('|');
        builder.Append(appointmentId);

        return builder.ToString();
    }

    private static bool TryGetUserAccountId(ClaimsPrincipal user, out Guid userAccountId)
    {
        string? rawUserId = user.FindFirstValue("sub")
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(rawUserId, out userAccountId);
    }
}

public sealed record PublicAppointmentCancellationResult(
    PublicAppointmentRequestAccessOutcome Outcome,
    string? ErrorCode,
    PublicAppointmentCancellationResponse? Response,
    int? CancellationCutoffHours)
{
    public static PublicAppointmentCancellationResult Success(
        PublicAppointmentCancellationResponse response)
    {
        return new PublicAppointmentCancellationResult(
            PublicAppointmentRequestAccessOutcome.Success,
            null,
            response,
            null);
    }

    public static PublicAppointmentCancellationResult Failure(
        PublicAppointmentRequestAccessOutcome outcome,
        string errorCode,
        int? cancellationCutoffHours = null)
    {
        return new PublicAppointmentCancellationResult(
            outcome,
            errorCode,
            null,
            cancellationCutoffHours);
    }
}

public sealed record PublicAppointmentCancellationResponse(
    Guid AppointmentId,
    string Status);

/// <param name="CancellationCutoffHours">
/// APPOINTMENT_CANCEL_TOO_LATE durumunda dolu gelir; UI kullaniciya ANLASILIR bir mesaj
/// yazabilsin diye ("Randevu saatine 2 saatten az kaldigi icin iptal edilemiyor").
/// </param>
public sealed record PublicAppointmentCancellationErrorResponse(
    string ErrorCode,
    int? CancellationCutoffHours);
