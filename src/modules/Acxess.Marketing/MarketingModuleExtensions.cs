using Acxess.Marketing.Domain.Abstractions;
using Acxess.Marketing.Infrastructure.Persistence;
using Acxess.Shared.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Acxess.Marketing;

public static class MarketingModuleExtensions
{
     public static IServiceCollection AddMarketingModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default");

        services.AddDbContext<MarketingModuleContext>(options =>
        {
            options.UseSqlServer(connectionString, sqlOptions =>
                sqlOptions.MigrationsHistoryTable("__MarketingMigrationsHistory", "Marketing")
            );
        });

        services.AddScoped<IDataSeeder, MarketingSeeders>();
        services.AddScoped<IMarketingUnitOfWork, MarketingUnitOfWork>();

        return services;
    }
}
