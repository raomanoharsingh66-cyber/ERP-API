using System.Text;
using BizFlow.Api.Extensions;
using BizFlow.Api.Identity;
using BizFlow.Api.Middleware;
using BizFlow.Application;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Infrastructure;
using BizFlow.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Serilog;

// Initialize early bootstrap logger
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting BizFlow ERP API host...");

    var builder = WebApplication.CreateBuilder(args);

    // Configure Serilog from appsettings
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    // Add Application & Infrastructure Clean Architecture layers
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // Add Controllers & JSON options
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        });

    // Configure JWT Authentication
    var secretKey = builder.Configuration["JwtSettings:SecretKey"] 
        ?? "BizFlow_SuperSecretKey_ProductionQuality_MustBeAtLeast32Bytes_2026!";
    var issuer = builder.Configuration["JwtSettings:Issuer"] ?? "BizFlow.Api";
    var audience = builder.Configuration["JwtSettings:Audience"] ?? "BizFlow.Client";

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

    builder.Services.AddAuthorization();
    builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();

    // Configure CORS for Angular Frontend (Localhost + Vercel)
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            policy.SetIsOriginAllowed(_ => true)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    });

    // Configure Swagger with JWT Bearer
    builder.Services.AddSwaggerDocumentation();

    var app = builder.Build();

    // Run automatic database creation and seeding on startup
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var dbContext = services.GetRequiredService<ApplicationDbContext>();
            var seeder = services.GetRequiredService<IDatabaseSeeder>();
            
            await dbContext.Database.EnsureCreatedAsync();
            await seeder.SeedAsync();
            Log.Information("Database schema ensured and seeded successfully.");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Database initialization warning: Ensure SQL Server is accessible on localhost (BizFlowErpDb).");
        }
    }

    // Custom Middlewares
    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseMiddleware<RequestLoggingMiddleware>();

    // Swagger UI in All Environments
    app.UseSwaggerDocumentation();

    if (!app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }

    app.UseCors("AllowFrontend");

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapGet("/", () => Results.Redirect("/swagger"));
    app.MapControllers();

    Log.Information("BizFlow ERP API configured and ready to handle requests.");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "BizFlow ERP API host terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}
