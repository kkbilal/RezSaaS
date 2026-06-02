namespace RezSaaS.Api.Configuration;

public sealed class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";

    private readonly RequestDelegate next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        string correlationId = context.Request.Headers.TryGetValue(HeaderName, out var values)
            && !string.IsNullOrWhiteSpace(values.FirstOrDefault())
                ? values.First()!
                : Guid.CreateVersion7().ToString("N");

        context.TraceIdentifier = correlationId;
        context.Response.Headers.TryAdd(HeaderName, correlationId);

        await next(context);
    }
}
