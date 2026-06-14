using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace RezSaaS.Api.Configuration;

public static class LocalDevelopmentConfiguration
{
    private const string DatabaseNameKey = "REZSAAS_POSTGRES_DB";
    private const string HostKey = "REZSAAS_POSTGRES_HOST";
    private const string PasswordKey = "REZSAAS_POSTGRES_PASSWORD";
    private const string PortKey = "REZSAAS_POSTGRES_PORT";
    private const string UserKey = "REZSAAS_POSTGRES_USER";

    public static void AddLocalDevelopmentEnvironment(
        this ConfigurationManager configuration,
        IHostEnvironment environment)
    {
        if (!environment.IsDevelopment()
            || !string.IsNullOrWhiteSpace(configuration.GetConnectionString("IdentityDatabase")))
        {
            return;
        }

        string? environmentPath = FindEnvironmentPath(environment);

        if (environmentPath is null)
        {
            throw new InvalidOperationException(
                "Development run requires either 'ConnectionStrings__IdentityDatabase' or a local '.env' file. "
                + "Copy '.env.example' to '.env' and replace the PostgreSQL password.");
        }

        Dictionary<string, string> values = ReadEnvironmentFile(environmentPath);
        Dictionary<string, string?> localConfiguration = new(StringComparer.OrdinalIgnoreCase)
        {
            ["ConnectionStrings:AdminDatabase"] = CreatePostgresConnectionString(values),
            ["ConnectionStrings:AvailabilityDatabase"] = CreatePostgresConnectionString(values),
            ["ConnectionStrings:BookingDatabase"] = CreatePostgresConnectionString(values),
            ["ConnectionStrings:CatalogDatabase"] = CreatePostgresConnectionString(values),
            ["ConnectionStrings:IdentityDatabase"] = CreateIdentityConnectionString(values),
            ["ConnectionStrings:IntegrationsDatabase"] = CreatePostgresConnectionString(values),
            ["ConnectionStrings:MessagingDatabase"] = CreatePostgresConnectionString(values),
            ["ConnectionStrings:OrganizationDatabase"] = CreatePostgresConnectionString(values),
            ["ConnectionStrings:PaymentsDatabase"] = CreatePostgresConnectionString(values),
            ["ConnectionStrings:ResourcesDatabase"] = CreatePostgresConnectionString(values),
            ["ConnectionStrings:TenantManagementDatabase"] = CreateIdentityConnectionString(values),
        };

        configuration.AddInMemoryCollection(localConfiguration);
    }

    private static string CreateIdentityConnectionString(Dictionary<string, string> values)
    {
        return CreatePostgresConnectionString(values);
    }

    private static string CreatePostgresConnectionString(Dictionary<string, string> values)
    {
        return "Host=" + Quote(GetRequiredValue(values, HostKey))
            + ";Port=" + Quote(GetRequiredValue(values, PortKey))
            + ";Database=" + Quote(GetRequiredValue(values, DatabaseNameKey))
            + ";Username=" + Quote(GetRequiredValue(values, UserKey))
            + ";Password=" + Quote(GetRequiredValue(values, PasswordKey));
    }

    private static string? FindEnvironmentPath(IHostEnvironment environment)
    {
        string?[] candidates =
        [
            Directory.GetCurrentDirectory(),
            environment.ContentRootPath,
            AppContext.BaseDirectory,
        ];

        foreach (string? candidate in candidates)
        {
            string? path = FindEnvironmentPathFrom(candidate);

            if (path is not null)
            {
                return path;
            }
        }

        return null;
    }

    private static string? FindEnvironmentPathFrom(string? startPath)
    {
        if (string.IsNullOrWhiteSpace(startPath))
        {
            return null;
        }

        DirectoryInfo? directory = new(startPath);

        while (directory is not null)
        {
            string environmentPath = Path.Combine(directory.FullName, ".env");

            if (File.Exists(environmentPath))
            {
                return environmentPath;
            }

            directory = directory.Parent;
        }

        return null;
    }

    private static string GetRequiredValue(
        Dictionary<string, string> values,
        string key)
    {
        if (!values.TryGetValue(key, out string? value) || string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Local '.env' value '{key}' is required for Development run.");
        }

        return value;
    }

    private static string Quote(string value)
    {
        return "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }

    private static Dictionary<string, string> ReadEnvironmentFile(string path)
    {
        Dictionary<string, string> values = new(StringComparer.OrdinalIgnoreCase);

        foreach (string rawLine in File.ReadAllLines(path))
        {
            string line = rawLine.Trim();

            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            string[] parts = line.Split('=', 2);

            if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]))
            {
                throw new InvalidOperationException($"Invalid local '.env' entry: '{line}'.");
            }

            values[parts[0].Trim()] = TrimOptionalQuotes(parts[1].Trim());
        }

        return values;
    }

    private static string TrimOptionalQuotes(string value)
    {
        if (value.Length >= 2
            && ((value[0] == '"' && value[^1] == '"')
                || (value[0] == '\'' && value[^1] == '\'')))
        {
            return value[1..^1];
        }

        return value;
    }
}
