namespace RezSaaS.BuildingBlocks.Security;

public static class PiiMasker
{
    public static string MaskEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return string.Empty;
        }

        string normalized = email.Trim();
        int atIndex = normalized.IndexOf('@', StringComparison.Ordinal);

        if (atIndex <= 1)
        {
            return "***";
        }

        return normalized[0] + "***" + normalized[atIndex..];
    }

    public static string MaskPhone(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return string.Empty;
        }

        string digits = new(phone.Where(char.IsDigit).ToArray());

        if (digits.Length <= 4)
        {
            return "***";
        }

        return "***" + digits[^4..];
    }
}
