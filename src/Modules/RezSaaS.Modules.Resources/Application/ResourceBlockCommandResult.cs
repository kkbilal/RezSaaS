namespace RezSaaS.Modules.Resources.Application;

public sealed record ResourceBlockCommandResult(
    bool Succeeded,
    ResourceBlockView? Block,
    string? ErrorCode)
{
    public static ResourceBlockCommandResult Success(ResourceBlockView block)
    {
        return new ResourceBlockCommandResult(true, block, ErrorCode: null);
    }

    public static ResourceBlockCommandResult Failure(string errorCode)
    {
        return new ResourceBlockCommandResult(false, Block: null, errorCode);
    }
}
