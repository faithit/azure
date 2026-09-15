using Microsoft.Extensions.Configuration;
using Npgsql;

namespace CloudOps.Infrastructure.Persistence;

/// <summary>Resolves the connection-string formats commonly supplied by hosts such as Supabase and Render.</summary>
public static class DatabaseConnectionString
{
    public static string Resolve(IConfiguration configuration)
    {
        var value = configuration["DATABASE_URL"] ?? configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException("DATABASE_URL or ConnectionStrings__DefaultConnection must be configured.");
        }

        return ToNpgsqlConnectionString(value);
    }

    public static string ResolveFromEnvironment(string? fallback = null)
    {
        var value = Environment.GetEnvironmentVariable("DATABASE_URL")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? fallback;
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException("DATABASE_URL or ConnectionStrings__DefaultConnection must be configured.");
        }

        return ToNpgsqlConnectionString(value);
    }

    private static string ToNpgsqlConnectionString(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)
            || (uri.Scheme is not "postgres" and not "postgresql"))
        {
            return value;
        }

        var userInfo = uri.UserInfo.Split(':', 2);
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort ? 5432 : uri.Port,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.Trim('/')),
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty,
            SslMode = SslMode.Require
        };

        return builder.ConnectionString;
    }
}
