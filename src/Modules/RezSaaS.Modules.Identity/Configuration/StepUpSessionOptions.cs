namespace RezSaaS.Modules.Identity.Configuration;

public sealed class StepUpSessionOptions
{
    public const string SectionName = "Identity:StepUp";

    public string CookieName { get; init; } = "RezSaaS.StepUp";

    public int DurationMinutes { get; init; } = 30;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(CookieName))
        {
            throw new InvalidOperationException("Step-up cookie name is required.");
        }

        if (DurationMinutes <= 0 || DurationMinutes > 480)
        {
            throw new InvalidOperationException(
                "Step-up session duration must be between 1 and 480 minutes.");
        }
    }
}
