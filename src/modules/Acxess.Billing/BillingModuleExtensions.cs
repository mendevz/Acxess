using Acxess.Billing.Domain.Abstractions;
using Acxess.Billing.Infrastructure.Persistence;
using Acxess.Shared.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Acxess.Billing;

public static class BillingModuleExtensions
{
    public static IServiceCollection AddBillingModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default");

        services.AddDbContext<BillingModuleContext>(options =>
        {
            options.UseSqlServer(connectionString, sqlOptions =>
                sqlOptions.MigrationsHistoryTable("__BillingMigrationsHistory", "Billing")
            );
        });

        services.AddScoped<IDataSeeder, BillingSeeder>();
        services.AddScoped<IBillingUnitOfWork, BillingUnitOfWork>();

        return services;
    }

}
