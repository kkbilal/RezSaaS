using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
using RezSaaS.BuildingBlocks.Auditing;
using RezSaaS.BuildingBlocks.Booking;
using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.Booking.Domain;
using RezSaaS.Modules.Booking.Infrastructure.Persistence;

namespace RezSaaS.Modules.Booking.Application;

/// <summary>
/// Musterinin KENDI onaylanmis (Confirmed) randevusunu iptal etmesi.
/// </summary>
/// <remarks>
/// NEDEN VAR (LANSMAN BLOKAJIYDI):
/// Musteri onaylanmis randevusunu HICBIR SEKILDE iptal edemiyordu. Talep iptali yalnizca
/// PendingApproval statusunde calisiyor; salon onayladiktan sonra APPOINTMENT_REQUEST_ALREADY_CLOSED
/// donuyordu. Isletme tarafindaki iptal ucu ise tenant uyeligi ariyor -> musteri Forbidden aliyordu.
///
/// Sonuc: musterinin plani degisirse yapacak HICBIR SEYI yoktu, salonu ARAMAK zorundaydi --
/// ki bu tam olarak bu SaaS'in cozmeyi vaat ettigi problem. Ayrica isletme takvimi no-show ile
/// dolardi.
///
/// TASARIM:
/// - Sahiplik uymazsa 404 (403 DEGIL). 403 "bu kayit var ama goremiyorsun" bilgisini SIZDIRIR.
///   Mevcut CancelAppointmentRequestService de boyle davraniyor.
/// - Satir kilidi (FOR UPDATE): iki es zamanli iptal yarismasin.
/// - Idempotent: ayni Idempotency-Key ile tekrar cagrilirsa ayni sonucu doner; zaten iptal
///   edilmis randevu icin de basarili doner (cift tiklama cift etki yaratmaz).
/// - Iptal politikasi (CancellationCutoffHours) BACKEND'DE zorlanir. UI'da butonu gizlemek
///   bir yetki/kural kontrolu DEGILDIR -- dogruluk kaynagi burasidir.
/// </remarks>
public sealed class CancelAppointmentByCustomerService
{
    public const string AlreadyClosed = "APPOINTMENT_ALREADY_CLOSED";
    public const string CancelTooLate = "APPOINTMENT_CANCEL_TOO_LATE";
    public const string IdempotencyKeyReused = "IDEMPOTENCY_KEY_REUSED";
    public const string MissingTenantContext = "MISSING_TENANT_CONTEXT";
    public const string NotFound = "APPOINTMENT_NOT_FOUND";

    private const string CancelOperation = "public.appointment.cancel";
    private const string CancellationReason = "Müşteri tarafından iptal edildi.";

    private readonly IAuditLogRecorder auditLogRecorder;
    private readonly IBusinessCancellationPolicyLookup cancellationPolicyLookup;
    private readonly BookingDbContext dbContext;
    private readonly ITenantContextAccessor tenantContextAccessor;
    private readonly TimeProvider timeProvider;

    public CancelAppointmentByCustomerService(
        BookingDbContext dbContext,
        IAuditLogRecorder auditLogRecorder,
        IBusinessCancellationPolicyLookup cancellationPolicyLookup,
        ITenantContextAccessor tenantContextAccessor,
        TimeProvider timeProvider)
    {
        this.dbContext = dbContext;
        this.auditLogRecorder = auditLogRecorder;
        this.cancellationPolicyLookup = cancellationPolicyLookup;
        this.tenantContextAccessor = tenantContextAccessor;
        this.timeProvider = timeProvider;
    }

    public async Task<CustomerAppointmentCancellationResult> CancelAsync(
        Guid appointmentId,
        Guid customerUserAccountId,
        BookingIdempotencyContext? idempotency = null,
        CancellationToken cancellationToken = default)
    {
        if (tenantContextAccessor.TenantId is not { } tenantId)
        {
            return CustomerAppointmentCancellationResult.Failure(MissingTenantContext);
        }

        CustomerAppointmentCancellationResult? replay =
            await TryReplayAsync(tenantId, customerUserAccountId, idempotency, cancellationToken);

        if (replay is not null)
        {
            return replay;
        }

        DateTimeOffset now = timeProvider.GetUtcNow();

        await using IDbContextTransaction transaction =
            await dbContext.Database.BeginTransactionAsync(cancellationToken);

        Appointment? appointment =
            await LockAppointmentAsync(tenantId, appointmentId, cancellationToken);

        // SAHIPLIK: eslesmezse 404. Baskasinin randevusunun VAR OLDUGUNU bile sizdirmiyoruz.
        if (appointment is null || appointment.CustomerUserAccountId != customerUserAccountId)
        {
            return CustomerAppointmentCancellationResult.Failure(NotFound);
        }

        // IDEMPOTENT: zaten iptal edilmisse basarili don. Cift tiklama cift etki yaratmaz.
        if (appointment.Status == AppointmentStatus.Cancelled)
        {
            await AddIdempotencyRecordAsync(
                tenantId,
                customerUserAccountId,
                appointment.Id,
                idempotency,
                now,
                cancellationToken);

            return await SaveAsync(
                    transaction,
                    tenantId,
                    customerUserAccountId,
                    idempotency,
                    cancellationToken)
                ?? CustomerAppointmentCancellationResult.Success(appointment.Id);
        }

        // Tamamlanmis / gelmedi / yeniden planlanmis bir randevu iptal EDILEMEZ.
        if (appointment.Status != AppointmentStatus.Confirmed)
        {
            return CustomerAppointmentCancellationResult.Failure(AlreadyClosed);
        }

        // IPTAL POLITIKASI -- BACKEND'DE zorlanir.
        BusinessCancellationPolicy? policy =
            await cancellationPolicyLookup.GetAsync(tenantId, cancellationToken);

        // FAIL-CLOSED: politika okunamazsa varsayilana duseriz, "kural yok" SAYMAYIZ.
        int cutoffHours = policy?.CancellationCutoffHours ?? DefaultCutoffHours;

        if (cutoffHours > 0
            && now.AddHours(cutoffHours) > appointment.StartUtc)
        {
            return CustomerAppointmentCancellationResult.Failure(CancelTooLate, cutoffHours);
        }

        appointment.Cancel(customerUserAccountId, CancellationReason, now);

        await AddIdempotencyRecordAsync(
            tenantId,
            customerUserAccountId,
            appointment.Id,
            idempotency,
            now,
            cancellationToken);

        CustomerAppointmentCancellationResult? duplicate =
            await SaveAsync(transaction, tenantId, customerUserAccountId, idempotency, cancellationToken);

        if (duplicate is not null)
        {
            return duplicate;
        }

        await auditLogRecorder.RecordAsync(
            new AuditLogRecord(
                tenantId,
                customerUserAccountId,
                "booking.appointment.cancelled_by_customer",
                $$"""{"tenantId":"{{tenantId}}","appointmentId":"{{appointment.Id}}"}""",
                now),
            cancellationToken);

        return CustomerAppointmentCancellationResult.Success(appointment.Id);
    }

    /// <summary>
    /// Politika okunamadiginda kullanilan guvenli varsayilan.
    /// "Okuyamadim, o halde kural yok" demek son dakika iptallerini serbest birakirdi.
    /// </summary>
    private const int DefaultCutoffHours = 2;

    private async Task<Appointment?> LockAppointmentAsync(
        Guid tenantId,
        Guid appointmentId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Appointments
            .FromSqlInterpolated(
                $"""
                SELECT *
                FROM booking."Appointments"
                WHERE "TenantId" = {tenantId}
                    AND "Id" = {appointmentId}
                FOR UPDATE
                """)
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(cancellationToken);
    }

    private async Task<CustomerAppointmentCancellationResult?> SaveAsync(
        IDbContextTransaction transaction,
        Guid tenantId,
        Guid customerUserAccountId,
        BookingIdempotencyContext? idempotency,
        CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return null;
        }
        catch (DbUpdateException) when (idempotency is not null)
        {
            // Ayni anahtarla es zamanli ikinci istek yarisi kazandi. Onun sonucunu doneriz.
            await transaction.RollbackAsync(cancellationToken);
            DetachChangedEntities();

            CustomerAppointmentCancellationResult? replayed =
                await TryReplayAsync(tenantId, customerUserAccountId, idempotency, cancellationToken);

            if (replayed is not null)
            {
                return replayed;
            }

            throw;
        }
    }

    private async Task<CustomerAppointmentCancellationResult?> TryReplayAsync(
        Guid tenantId,
        Guid customerUserAccountId,
        BookingIdempotencyContext? idempotency,
        CancellationToken cancellationToken)
    {
        if (idempotency is null)
        {
            return null;
        }

        BookingIdempotencyRecord? existing = await dbContext.IdempotencyRecords
            .AsNoTracking()
            .SingleOrDefaultAsync(
                entity => entity.TenantId == tenantId
                    && entity.ActorUserAccountId == customerUserAccountId
                    && entity.Operation == CancelOperation
                    && entity.KeyHash == idempotency.KeyHash,
                cancellationToken);

        if (existing is null)
        {
            return null;
        }

        // Ayni anahtar, FARKLI govde ile geldiyse bu bir istemci hatasidir -- sessizce
        // eski sonucu donmek yanlis randevuyu iptal ettigimizi dusundurur.
        if (existing.RequestHash != idempotency.RequestHash
            || existing.ResponseResourceId is null)
        {
            return CustomerAppointmentCancellationResult.Failure(IdempotencyKeyReused);
        }

        return CustomerAppointmentCancellationResult.Success(existing.ResponseResourceId.Value);
    }

    private Task AddIdempotencyRecordAsync(
        Guid tenantId,
        Guid customerUserAccountId,
        Guid appointmentId,
        BookingIdempotencyContext? idempotency,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (idempotency is null)
        {
            return Task.CompletedTask;
        }

        dbContext.IdempotencyRecords.Add(
            BookingIdempotencyRecord.Create(
                tenantId,
                customerUserAccountId,
                CancelOperation,
                idempotency.KeyHash,
                idempotency.RequestHash,
                appointmentId,
                appointmentId,
                AppointmentStatus.Cancelled.ToString(),
                affectedRequests: 0,
                responseExpiresAtUtc: null,
                createdAtUtc: now));

        return Task.CompletedTask;
    }

    private void DetachChangedEntities()
    {
        foreach (EntityEntry entry in dbContext.ChangeTracker
            .Entries()
            .Where(entity => entity.State is EntityState.Added or EntityState.Modified))
        {
            entry.State = EntityState.Detached;
        }
    }
}

public sealed record CustomerAppointmentCancellationResult(
    bool Succeeded,
    string? ErrorCode,
    Guid? AppointmentId,
    int? CancellationCutoffHours)
{
    public static CustomerAppointmentCancellationResult Success(Guid appointmentId)
    {
        return new CustomerAppointmentCancellationResult(true, null, appointmentId, null);
    }

    public static CustomerAppointmentCancellationResult Failure(
        string errorCode,
        int? cancellationCutoffHours = null)
    {
        return new CustomerAppointmentCancellationResult(
            false,
            errorCode,
            null,
            cancellationCutoffHours);
    }
}
