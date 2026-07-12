namespace RezSaaS.Modules.Organization.Domain;

public sealed class StaffMember
{
    private StaffMember()
    {
    }

    private StaffMember(
        Guid id,
        Guid tenantId,
        Guid branchId,
        Guid? userAccountId,
        string displayName,
        DateTimeOffset createdAtUtc)
    {
        RequireNonEmpty(tenantId, nameof(tenantId));
        RequireNonEmpty(branchId, nameof(branchId));

        Id = id;
        TenantId = tenantId;
        BranchId = branchId;
        UserAccountId = userAccountId;
        DisplayName = NormalizeRequiredText(displayName, nameof(displayName));
        CreatedAtUtc = createdAtUtc;
    }

    public Branch? Branch { get; private set; }

    public Guid BranchId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public string DisplayName { get; private set; } = string.Empty;

    public Guid Id { get; private set; }

    public StaffMemberStatus Status { get; private set; } = StaffMemberStatus.Active;

    public Guid TenantId { get; private set; }

    public Guid? UserAccountId { get; private set; }

    public static StaffMember Create(
        Guid tenantId,
        Guid branchId,
        string displayName,
        DateTimeOffset createdAtUtc,
        Guid? userAccountId = null)
    {
        return new StaffMember(
            Guid.CreateVersion7(),
            tenantId,
            branchId,
            userAccountId,
            displayName,
            createdAtUtc);
    }

    /// <summary>
    /// Personelin görünen adını değiştirir.
    /// </summary>
    /// <remarks>
    /// Bu metot uzun süre EKSİKTİ. StaffManagementService.UpdateAsync entity'yi çekip
    /// DisplayName'i hiç uygulamadan SaveChangesAsync çağırıyordu: istek 200 OK dönüyor,
    /// isim DEĞİŞMİYOR, üstelik "organization.staff.updated" audit kaydı da yazılıyordu.
    /// Yani hem kullanıcıya hem denetim günlüğüne yalan söyleniyordu.
    ///
    /// Yanlış yazılmış bir personel adını düzeltmek tipik bir ilk-kullanım senaryosudur;
    /// bu boşluk ürünün "elemanlarını yönetebilme" vaadini doğrudan deliyordu.
    /// </remarks>
    public void Rename(string displayName)
    {
        DisplayName = NormalizeRequiredText(displayName, nameof(displayName));
    }

    public void Archive()
    {
        Status = StaffMemberStatus.Archived;
    }

    private static string NormalizeRequiredText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        return value.Trim();
    }

    private static void RequireNonEmpty(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Value is required.", parameterName);
        }
    }
}
