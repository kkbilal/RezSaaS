namespace RezSaaS.Api.Session;

public sealed record SessionBootstrapResponse(
    SessionAccountResponse Account,
    IReadOnlyCollection<string> PlatformRoles,
    SessionStepUpResponse StepUp,
    IReadOnlyCollection<SessionTenantMembershipResponse> TenantMemberships);

public sealed record SessionAccountResponse(
    Guid UserAccountId,
    string? Email,
    bool EmailConfirmed,
    string Status);

public sealed record SessionStepUpResponse(
    bool IsSatisfied,
    IReadOnlyCollection<string> Methods,
    DateTimeOffset? ExpiresAtUtc);

public sealed record SessionTenantMembershipResponse(
    Guid MembershipId,
    Guid TenantId,
    string TenantSlug,
    string TenantDisplayName,
    string Role,
    Guid? BranchId);

public sealed record SessionStepUpRequest(
    string Password,
    string? TwoFactorCode,
    string? RecoveryCode);

public sealed record SessionStepUpCompletedResponse(
    bool IsSatisfied,
    string Method,
    DateTimeOffset ExpiresAtUtc);

public sealed record SessionStepUpErrorResponse(string ErrorCode);
