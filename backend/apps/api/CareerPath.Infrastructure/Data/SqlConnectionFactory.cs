using Microsoft.Data.SqlClient;

namespace CareerPath.Infrastructure.Data;

/// <summary>
/// Creates and opens SQL connections. Callers are responsible for disposal.
/// Connection string is injected via typed options; no secrets stored in code.
/// </summary>
public interface ISqlConnectionFactory
{
    Task<SqlConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default);
}

public sealed class SqlConnectionFactory : ISqlConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _connectionString = connectionString;
    }

    public async Task<SqlConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
