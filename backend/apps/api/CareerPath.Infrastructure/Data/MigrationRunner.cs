using System.Reflection;
using Dapper;
using Microsoft.Extensions.Logging;

namespace CareerPath.Infrastructure.Data;

/// <summary>
/// Runs numbered SQL migration scripts from the CareerPath.Migrations project.
/// Records applied migrations in system.MigrationHistory to prevent re-application.
/// NEVER run destructive migrations automatically on production startup.
/// </summary>
public sealed class MigrationRunner
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly ILogger<MigrationRunner> _logger;
    private readonly Assembly _migrationsAssembly;

    public MigrationRunner(
        ISqlConnectionFactory connectionFactory,
        ILogger<MigrationRunner> logger,
        Assembly migrationsAssembly)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
        _migrationsAssembly = migrationsAssembly;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        // Ensure MigrationHistory table exists before querying it
        await EnsureMigrationHistoryTableAsync(connection, cancellationToken);

        var applied = (await connection.QueryAsync<string>(
            "SELECT ScriptName FROM [system].[MigrationHistory] ORDER BY AppliedAt")).ToHashSet();

        var scripts = GetMigrationScripts();

        foreach (var (name, sql) in scripts)
        {
            if (applied.Contains(name))
            {
                _logger.LogDebug("Migration already applied: {ScriptName}", name);
                continue;
            }

            _logger.LogInformation("Applying migration: {ScriptName}", name);

            await using var transaction = connection.BeginTransaction();
            try
            {
                // Split on GO statements for multi-batch scripts
                var batches = sql.Split("\nGO", StringSplitOptions.RemoveEmptyEntries);
                foreach (var batch in batches)
                {
                    var trimmed = batch.Trim();
                    if (!string.IsNullOrWhiteSpace(trimmed))
                        await connection.ExecuteAsync(trimmed, transaction: transaction, commandTimeout: 120);
                }

                await connection.ExecuteAsync(
                    "INSERT INTO [system].[MigrationHistory] (ScriptName, AppliedAt) VALUES (@ScriptName, @AppliedAt)",
                    new { ScriptName = name, AppliedAt = DateTimeOffset.UtcNow },
                    transaction: transaction);

                transaction.Commit();
                applied.Add(name);
                _logger.LogInformation("Migration applied successfully: {ScriptName}", name);
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Migration failed: {ScriptName}", name);
                throw;
            }
        }

        // Ensure default admin user exists
        await EnsureDefaultAdminAsync(connection, cancellationToken);
    }

    private async Task EnsureDefaultAdminAsync(
        Microsoft.Data.SqlClient.SqlConnection connection,
        CancellationToken cancellationToken)
    {
        try
        {
            const string adminEmail = "admin@careerpathbharat.com";
            var existingUserId = await connection.ExecuteScalarAsync<Guid?>(
                "SELECT Id FROM [identity].[Users] WHERE Email = @Email",
                new { Email = adminEmail });

            var hasher = new CareerPath.Infrastructure.Auth.BcryptPasswordHasher();
            var defaultHash = hasher.Hash("Admin@12345");

            Guid adminId;
            if (!existingUserId.HasValue)
            {
                adminId = Guid.NewGuid();
                await connection.ExecuteAsync(
                    """
                    INSERT INTO [identity].[Users] 
                        (Id, Email, PasswordHash, DisplayName, IsEmailVerified, IsActive, CreatedAt, UpdatedAt)
                    VALUES 
                        (@Id, @Email, @PasswordHash, 'System Administrator', 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME())
                    """,
                    new { Id = adminId, Email = adminEmail, PasswordHash = defaultHash });
                _logger.LogInformation("Seeded default admin user: {Email}", adminEmail);
            }
            else
            {
                adminId = existingUserId.Value;
            }

            // Ensure Admin and SuperAdmin role assignments
            await connection.ExecuteAsync(
                """
                INSERT INTO [identity].[UserRoles] (UserId, RoleId, AssignedAt)
                SELECT @UserId, r.Id, SYSUTCDATETIME()
                FROM [identity].[Roles] r
                WHERE r.Name IN ('Admin', 'SuperAdmin')
                  AND NOT EXISTS (
                      SELECT 1 FROM [identity].[UserRoles] ur 
                      WHERE ur.UserId = @UserId AND ur.RoleId = r.Id
                  )
                """,
                new { UserId = adminId });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not ensure default admin user during startup.");
        }
    }

    private async Task EnsureMigrationHistoryTableAsync(
        Microsoft.Data.SqlClient.SqlConnection connection,
        CancellationToken cancellationToken)
    {
        const string sql = """
            IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'system')
                EXEC('CREATE SCHEMA [system]');
            IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id 
                           WHERE s.name = 'system' AND t.name = 'MigrationHistory')
            BEGIN
                CREATE TABLE [system].[MigrationHistory] (
                    Id          INT IDENTITY(1,1) PRIMARY KEY,
                    ScriptName  NVARCHAR(500) NOT NULL UNIQUE,
                    AppliedAt   DATETIMEOFFSET(7) NOT NULL
                );
            END
            """;
        await connection.ExecuteAsync(sql);
    }

    private IEnumerable<(string Name, string Sql)> GetMigrationScripts()
    {
        var resourceNames = _migrationsAssembly
            .GetManifestResourceNames()
            .Where(n => n.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            .OrderBy(n => n)
            .ToList();

        foreach (var resourceName in resourceNames)
        {
            using var stream = _migrationsAssembly.GetManifestResourceStream(resourceName)!;
            using var reader = new StreamReader(stream);
            var sql = reader.ReadToEnd();
            // Use just the filename portion as the script name
            var scriptName = resourceName.Split('.').TakeLast(2).First() + ".sql";
            // Actually use full resource name for uniqueness
            yield return (resourceName, sql);
        }
    }
}
