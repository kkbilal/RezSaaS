namespace RezSaaS.Modules.Admin.Domain;

public enum AccountClosureCaseStatus
{
    PendingApproval,
    Approved,
    Rejected,
    Executing,
    Executed,
    CancelledByAppeal,
}
