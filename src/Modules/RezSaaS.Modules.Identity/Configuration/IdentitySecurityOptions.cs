namespace RezSaaS.Modules.Identity.Configuration;

public sealed class IdentitySecurityOptions
{
    public const string SectionName = "Identity";

    public int AuthenticationPermitLimit { get; init; }

    public int AuthenticationWindowMinutes { get; init; }

    public EmailDeliveryMode DeliveryMode { get; init; }

    public int LockoutMinutes { get; init; }

    public int MaxFailedAccessAttempts { get; init; }

    public int PasswordRequiredLength { get; init; }

    public int PasswordRequiredUniqueChars { get; init; }

    public bool RequireConfirmedEmail { get; init; }

    public void Validate()
    {
        RequirePositiveValue(AuthenticationPermitLimit, nameof(AuthenticationPermitLimit));
        RequirePositiveValue(AuthenticationWindowMinutes, nameof(AuthenticationWindowMinutes));
        RequirePositiveValue(LockoutMinutes, nameof(LockoutMinutes));
        RequirePositiveValue(MaxFailedAccessAttempts, nameof(MaxFailedAccessAttempts));
        RequirePositiveValue(PasswordRequiredLength, nameof(PasswordRequiredLength));
        RequirePositiveValue(PasswordRequiredUniqueChars, nameof(PasswordRequiredUniqueChars));

        if (RequireConfirmedEmail && DeliveryMode == EmailDeliveryMode.Unconfigured)
        {
            throw new InvalidOperationException(
                "An email provider must be configured when confirmed email is required.");
        }
    }

    private static void RequirePositiveValue(int value, string propertyName)
    {
        if (value <= 0)
        {
            throw new InvalidOperationException(
                $"Identity configuration value '{propertyName}' must be greater than zero.");
        }
    }
}
