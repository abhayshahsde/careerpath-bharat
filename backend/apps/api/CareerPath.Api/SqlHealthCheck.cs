using CareerPath.Infrastructure.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CareerPath.Api;

/// <summary>
/// Health check that verifies SQL Server connectivity.
/// </summary>
public sealed class SqlHealthCheck : IHealthCheck
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public SqlHealthCheck(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
            return HealthCheckResult.Healthy("SQL Server is reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("SQL Server is not reachable.", ex);
        }
    }
}
