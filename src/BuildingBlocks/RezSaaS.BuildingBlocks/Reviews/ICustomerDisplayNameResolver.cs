namespace RezSaaS.BuildingBlocks.Reviews;

/// <summary>
/// Cross-module contract implemented by the Identity module.
/// Used by the Reviews module to resolve a masked customer display name
/// for public review display (PII hygiene).
/// </summary>
public interface ICustomerDisplayNameResolver
{
    /// <summary>
    /// Returns a display-safe customer name for the given user account id.
    /// Returns null if the user does not exist.
    /// </summary>
    Task<string?> ResolveAsync(
        Guid userAccountId,
        CancellationToken cancellationToken = default);
}