using System.Text;
using System.Threading.RateLimiting;
using CareerPath.Api.Endpoints;
using CareerPath.Api.Identity;
using CareerPath.Api.Middleware;
using CareerPath.Application.Abstractions;
using CareerPath.Application.Careers;
using CareerPath.Infrastructure;
using CareerPath.Infrastructure.Data;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;

// ─── Bootstrap logger (before DI is built) ───────────────────────────────────
Directory.CreateDirectory("logs");
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId} {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/careerpath-.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 31,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {CorrelationId} {SourceContext} {Message:lj}{NewLine}{Exception}")
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting CareerPath Bharat API");

    var builder = WebApplication.CreateBuilder(args);

    // ─── Serilog (final config, reads from appsettings & ensures file sink) ─────
    builder.Host.UseSerilog((ctx, services, config) => config
        .ReadFrom.Configuration(ctx.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithEnvironmentName()
        .Enrich.WithMachineName()
        .Enrich.WithThreadId()
        .WriteTo.Console(outputTemplate:
            "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId} {SourceContext} {Message:lj}{NewLine}{Exception}")
        .WriteTo.File(
            path: "logs/careerpath-.txt",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 31,
            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {CorrelationId} {SourceContext} {Message:lj}{NewLine}{Exception}"));

    // ─── Configuration validation ─────────────────────────────────────────────
    builder.Services.AddOptions<JwtOptions>()
        .Bind(builder.Configuration.GetSection("Jwt"))
        .ValidateDataAnnotations()
        .ValidateOnStart();

    // ─── HTTP Accessor (for CurrentUserService) ───────────────────────────────
    builder.Services.AddHttpContextAccessor();

    // ─── Infrastructure ───────────────────────────────────────────────────────
    builder.Services.AddInfrastructure(builder.Configuration);

    // ─── Application (MediatR + FluentValidation) ─────────────────────────────
    builder.Services.AddMediatR(cfg =>
    {
        cfg.RegisterServicesFromAssemblyContaining<GetCareersQuery>();
        cfg.AddOpenBehavior(typeof(CareerPath.Application.Common.Behaviors.HtmlSanitizationBehavior<,>));
    });

    builder.Services.AddValidatorsFromAssemblyContaining<
        CareerPath.Application.Student.UpsertProfileValidator>();

    // ─── Identity ─────────────────────────────────────────────────────────────
    builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

    var jwtSection = builder.Configuration.GetSection("Jwt");
    var jwtKey = jwtSection["Key"]
        ?? throw new InvalidOperationException("Jwt:Key is not configured.");

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer           = true,
                ValidateAudience         = true,
                ValidateLifetime         = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer              = jwtSection["Issuer"],
                ValidAudience            = jwtSection["Audience"],
                IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ClockSkew                = TimeSpan.FromSeconds(30)
            };
        });

    builder.Services.AddAuthorization();

    // ─── OpenAPI / Swagger ────────────────────────────────────────────────────
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title       = "CareerPath Bharat API",
            Version     = "v1",
            Description = "Production-grade career guidance platform for Indian students. " +
                          "All salary/admission information is indicative only."
        });
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name         = "Authorization",
            Type         = SecuritySchemeType.Http,
            Scheme       = "bearer",
            BearerFormat = "JWT",
            In           = ParameterLocation.Header,
            Description  = "Enter your JWT token."
        });
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            [
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                }
            ] = Array.Empty<string>()
        });
    });

    // ─── Rate Limiting ────────────────────────────────────────────────────────
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        // Public endpoints: 60 req/min per IP
        options.AddFixedWindowLimiter("public", o =>
        {
            o.PermitLimit      = 60;
            o.Window           = TimeSpan.FromMinutes(1);
            o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            o.QueueLimit       = 5;
        });

        // Auth/write endpoints: 10 req/min per IP
        options.AddFixedWindowLimiter("strict", o =>
        {
            o.PermitLimit      = 10;
            o.Window           = TimeSpan.FromMinutes(1);
            o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            o.QueueLimit       = 0;
        });
    });

    // ─── Health Checks ────────────────────────────────────────────────────────
    builder.Services.AddHealthChecks()
        .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy())
        .AddCheck<SqlHealthCheck>("sql");

    // ─── CORS ─────────────────────────────────────────────────────────────────
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("Frontend", policy =>
        {
            var origins = builder.Configuration
                .GetSection("Cors:AllowedOrigins")
                .Get<string[]>() ?? ["http://localhost:5173"];

            policy.WithOrigins(origins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });

    // ─── Problem Details ──────────────────────────────────────────────────────
    builder.Services.AddProblemDetails(options =>
    {
        options.CustomizeProblemDetails = ctx =>
        {
            ctx.ProblemDetails.Extensions["correlationId"] =
                ctx.HttpContext.Items["CorrelationId"]?.ToString();
            ctx.ProblemDetails.Extensions["instance"] =
                ctx.HttpContext.Request.Path.Value;
        };
    });

    // ─────────────────────────────────────────────────────────────────────────
    var app = builder.Build();

    // Run migrations on startup (creates tables, catalog data, and seeds if they don't exist)
    try
    {
        using var scope = app.Services.CreateScope();
        var migrationRunner = scope.ServiceProvider.GetRequiredService<MigrationRunner>();
        await migrationRunner.RunAsync();
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Database migration on startup encountered an error or database is initializing. API will continue to start.");
    }

    // ─── Middleware pipeline ──────────────────────────────────────────────────
    app.UseExceptionHandler();
    app.UseStatusCodePages();

    app.UseCorrelationId();

    app.UseSerilogRequestLogging(opts =>
    {
        opts.EnrichDiagnosticContext = (diag, ctx) =>
        {
            diag.Set("CorrelationId", ctx.Items["CorrelationId"]?.ToString() ?? string.Empty);
            diag.Set("UserAgent", ctx.Request.Headers.UserAgent.ToString());
        };
    });

    if (!app.Environment.IsDevelopment())
    {
        app.UseHsts();
    }

    app.UseMiddleware<SecurityHeadersMiddleware>();
    app.UseHttpsRedirection();
    app.UseCors("Frontend");
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();

    // ─── Swagger UI + Scalar ───────────────────────────────────────────────────
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "CareerPath Bharat API v1"));
    app.MapScalarApiReference(options =>
    {
        options.Title = "CareerPath Bharat API";
        options.OpenApiRoutePattern = "/swagger/v1/swagger.json";
    });

    // ─── Health endpoints ─────────────────────────────────────────────────────
    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = check => check.Name == "self"
    });
    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = _ => true
    });

    // ─── API endpoints ────────────────────────────────────────────────────────
    app.MapAuth();
    app.MapCareers();
    app.MapProfile();
    app.MapCatalog();
    app.MapEditorial();
    app.MapRecommendations();
    app.MapImports();
    app.MapExports();
    app.MapKnowledge();
    app.MapAi();
    app.MapBilling();
    app.MapNotificationAnalytics();
    app.MapLogs();

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

// Needed for WebApplicationFactory in integration tests
public partial class Program { }
