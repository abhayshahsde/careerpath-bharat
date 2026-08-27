using CareerPath.Application.Abstractions;
using CareerPath.Application.Abstractions.Repositories;
using CareerPath.Application.Abstractions.Services;
using CareerPath.Infrastructure.Auth;
using CareerPath.Infrastructure.Data;
using CareerPath.Infrastructure.Repositories;
using CareerPath.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CareerPath.Infrastructure;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? configuration["ConnectionStrings:DefaultConnection"]
            ?? configuration["SQLAZURECONNSTR_DefaultConnection"]
            ?? configuration["CUSTOMCONNSTR_DefaultConnection"]
            ?? configuration["SQLCONNSTR_DefaultConnection"]
            ?? Environment.GetEnvironmentVariable("SQLAZURECONNSTR_DefaultConnection")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? Environment.GetEnvironmentVariable("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured in Azure App Service Configuration.");

        services.AddSingleton<ISqlConnectionFactory>(_ => new SqlConnectionFactory(connectionString));

        // Migration runner — loads embedded SQL scripts from Migrations assembly.
        // Using typeof(MigrationAssemblyMarker) avoids Assembly.Load() string lookup failures.
        services.AddSingleton<MigrationRunner>(sp =>
            new MigrationRunner(
                sp.GetRequiredService<ISqlConnectionFactory>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<MigrationRunner>>(),
                typeof(CareerPath.Migrations.MigrationAssemblyMarker).Assembly));

        // ── Auth services ─────────────────────────────────────────────────────
        var jwtSection = configuration.GetSection("Jwt");
        var jwtSettings = new JwtSettings
        {
            Key                = jwtSection["Key"]       ?? throw new InvalidOperationException("Jwt:Key is required."),
            Issuer             = jwtSection["Issuer"]    ?? "CareerPathBharat",
            Audience           = jwtSection["Audience"]  ?? "CareerPathBharatClients",
            AccessTokenMinutes = int.TryParse(jwtSection["AccessTokenMinutes"], out var atm) ? atm : 15,
            RefreshTokenDays   = int.TryParse(jwtSection["RefreshTokenDays"],   out var rtd) ? rtd : 30
        };
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(jwtSettings));
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();

        // ── Repositories ──────────────────────────────────────────────────────
        services.AddScoped<ICareerRepository, CareerRepository>();
        services.AddScoped<IStudentProfileRepository, StudentProfileRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ISessionRepository, SessionRepository>();
        services.AddScoped<ICatalogRepository, CatalogRepository>();
        services.AddScoped<IEditorialRepository, EditorialRepository>();
        services.AddScoped<IRecommendationRepository, RecommendationRepository>();
        services.AddScoped<IImportRepository, ImportRepository>();
        services.AddScoped<IExportRepository, ExportRepository>();
        services.AddScoped<IKnowledgeRepository, KnowledgeRepository>();
        services.AddScoped<IAiRepository, AiRepository>();
        services.AddScoped<IAiService, GeminiService>();
        services.AddScoped<IBillingRepository, BillingRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IOtpRepository, OtpRepository>();
        services.AddScoped<IOtpDeliveryService, OtpDeliveryService>();

        return services;
    }
}
