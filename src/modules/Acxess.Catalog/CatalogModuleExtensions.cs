using Acxess.Catalog.Infrastructure.Persistence;
using Acxess.Shared.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Acxess.Catalog;

public static class CatalogModuleExtensions
{
    public static IServiceCollection AddCatalogModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default");

        services.AddDbContext<CatalogModuleContext>(options =>
        {
            options.UseSqlServer(connectionString, sqlOptions =>
                sqlOptions.MigrationsHistoryTable("__CatalogMigrationsHistory", "Catalog")
            );
        });

        services.AddScoped<IDataSeeder, CatalogSeeder>();

        return services;
    }
}
