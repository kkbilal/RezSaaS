using System.Security.Cryptography;
using System.Text;
using RezSaaS.Modules.Booking.Application;

namespace RezSaaS.Api.Idempotency;

public static class BookingIdempotencyContextFactory
{
    private const int MaxKeyLength = 128;
    private const int MinKeyLength = 8;

    public static bool TryCreate(
        string? rawKey,
        string requestMaterial,
        out BookingIdempotencyContext? context,
        out string? errorCode)
    {
        context = null;
        errorCode = null;

        if (string.IsNullOrWhiteSpace(rawKey))
        {
            return true;
        }

        string normalizedKey = rawKey.Trim();

        if (normalizedKey.Length is < MinKeyLength or > MaxKeyLength
            || normalizedKey.Any(char.IsControl)
            || normalizedKey.Any(char.IsWhiteSpace))
        {
            errorCode = "IDEMPOTENCY_KEY_INVALID";
            return false;
        }

        context = new BookingIdempotencyContext(
            ComputeSha256Hex(normalizedKey),
            ComputeSha256Hex(requestMaterial));

        return true;
    }

    private static string ComputeSha256Hex(string value)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }
}
