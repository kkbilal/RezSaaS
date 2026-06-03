using Microsoft.Extensions.Options;

namespace RezSaaS.Api.Configuration;

public sealed class UnsafeRequestOriginMiddleware
{
    private static readonly HashSet<string> UnsafeMethods =
        new(StringComparer.OrdinalIgnoreCase)
        {
            HttpMethods.Delete,
            HttpMethods.Patch,
            HttpMethods.Post,
            HttpMethods.Put,
        };

    private readonly RequestDelegate next;
    private readonly UnsafeRequestOriginOptions options;

    public UnsafeRequestOriginMiddleware(
        RequestDelegate next,
        IOptions<UnsafeRequestOriginOptions> options)
    {
        this.next = next;
        this.options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!UnsafeMethods.Contains(context.Request.Method)
            || IsAllowedOrigin(context.Request))
        {
            await next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
    }

    private bool IsAllowedOrigin(HttpRequest request)
    {
        string? origin = request.Headers.Origin.FirstOrDefault();

        if (string.IsNullOrWhiteSpace(origin))
        {
            string? referer = request.Headers.Referer.FirstOrDefault();

            return string.IsNullOrWhiteSpace(referer)
                || IsAllowedOriginValue(request, referer);
        }

        return IsAllowedOriginValue(request, origin);
    }

    private bool IsAllowedOriginValue(HttpRequest request, string origin)
    {
        if (!Uri.TryCreate(origin, UriKind.Absolute, out Uri? originUri))
        {
            return false;
        }

        string requestOrigin = $"{request.Scheme}://{request.Host}".TrimEnd('/');
        string normalizedOrigin = originUri.GetLeftPart(UriPartial.Authority).TrimEnd('/');

        return string.Equals(normalizedOrigin, requestOrigin, StringComparison.OrdinalIgnoreCase)
            || options.AllowedOrigins.Any(allowedOrigin =>
                string.Equals(
                    allowedOrigin.TrimEnd('/'),
                    normalizedOrigin,
                    StringComparison.OrdinalIgnoreCase));
    }
}
