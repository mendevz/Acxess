using Acxess.Shared.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Acxess.Infrastructure.Extensions;

public static class MigrationExtensions
{
    public static async Task ApplyMigrationsAndSeedsAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        
        var seeders = services.GetServices<IDataSeeder>();

        foreach (var seeder in seeders)
        {
            try 
            {
                await seeder.SeedAsync();
                Console.WriteLine($"--> Seed aplicado: {seeder.GetType().Name}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"--> Error en Seed {seeder.GetType().Name}: {ex.Message}");
                throw;
            }
        }
    }
}