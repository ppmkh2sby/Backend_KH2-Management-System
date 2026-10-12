using Npgsql;

namespace KH2.ManagementSystem.Infrastructure.Persistence;

internal static class PostgreSqlConnectionString
{
    public static string Normalize(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        if (!Uri.TryCreate(connectionString, UriKind.Absolute, out var uri) ||
            uri.Scheme is not "postgres" and not "postgresql")
        {
            return connectionString;
        }

        var userInfo = uri.GetComponents(UriComponents.UserInfo, UriFormat.Unescaped);
        var passwordSeparator = userInfo.IndexOf(':', StringComparison.Ordinal);
        if (passwordSeparator <= 0 || string.IsNullOrWhiteSpace(uri.Host))
        {
            throw new InvalidOperationException(
                "PostgreSQL URI must include a host, username, and password.");
        }

        var database = uri.AbsolutePath.Trim('/');
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort ? 5432 : uri.Port,
            Database = string.IsNullOrWhiteSpace(database) ? "postgres" : database,
            Username = userInfo[..passwordSeparator],
            Password = userInfo[(passwordSeparator + 1)..],
            SslMode = SslMode.Require,
            Pooling = true
        };

        return builder.ConnectionString;
    }
}
