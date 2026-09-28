using BizFlow.Application.Common.Interfaces;
using BizFlow.Infrastructure.Data;
using BizFlow.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BizFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? configuration.GetConnectionString("BizFlowErpDb")
            ?? "Server=localhost;Database=BizFlowErpDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString, b =>
                b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        services.AddScoped<IApplicationDbContext>(provider => 
            provider.GetRequiredService<ApplicationDbContext>());

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        // Phase 2 Identity, RBAC & Tenant Services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IDatabaseSeeder, DatabaseSeeder>();
        services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, BizFlow.Infrastructure.Identity.PermissionAuthorizationHandler>();

        // Phase 3 Inventory & Merchandise Services
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IInventoryService, InventoryService>();

        // Phase 4 Commercial & Sales Services
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<ISalesService, SalesService>();

        // Phase 5 Procurement & Vendor Payables Services
        services.AddScoped<ISupplierService, SupplierService>();
        services.AddScoped<IPurchaseService, PurchaseService>();

        // Phase 6 Accounting & General Ledger Services
        services.AddScoped<IAccountingService, AccountingService>();

        // Phase 7 Analytics & Audit Services
        services.AddScoped<IDashboardService, DashboardService>();

        // AI Smart Purchase Assistant
        services.AddScoped<IAiAssistantService, AiAssistantService>();

        return services;
    }
}
