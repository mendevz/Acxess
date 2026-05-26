using Acxess.Shared.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Respawn;
using System.Data.Common;
using Testcontainers.MsSql;

namespace Acxess.IntegrationTests.Setup;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _dbContainer;
    private Respawner _respawner = default!;
    private DbConnection _dbConnection = default!;

    public CustomWebApplicationFactory()
    {
        _dbContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("Acxess_Test_Pass_123!") 
            .Build();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        var sqlBuilder = new SqlConnectionStringBuilder(_dbContainer.GetConnectionString())
        {
            TrustServerCertificate = true
        };

        builder.UseSetting("ConnectionStrings:Default", sqlBuilder.ConnectionString);
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ICurrentTenant>();
            services.AddSingleton<TestCurrentTenant>();
            services.AddSingleton<ICurrentTenant>(sp => sp.GetRequiredService<TestCurrentTenant>());
        });
    }
    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        using var scope = Services.CreateScope();
        foreach (var seeder in scope.ServiceProvider.GetServices<IDataSeeder>())
        {
            await seeder.SeedAsync();
        }

        _dbConnection = new SqlConnection(_dbContainer.GetConnectionString());
        await _dbConnection.OpenAsync();

        _respawner = await Respawner.CreateAsync(_dbConnection, new RespawnerOptions
        {
            // Evita que Respawn borre los registros de que las tablas ya han sido migradas,
            // lo que es útil para mantener el estado de la base de datos consistente durante las pruebas.
            TablesToIgnore = 
            [
                "__MembershipMigrationsHistory",
                "__IdentityMigrationsHistory",
                "__CatalogMigrationsHistory",
                "__BillingMigrationsHistory",
                "__MarketingMigrationsHistory"
            ],
            DbAdapter = DbAdapter.SqlServer
        });
    }

    public async Task ResetDatabaseAsync()
    {
        await _respawner.ResetAsync(_dbConnection);
    }
    async Task IAsyncLifetime.DisposeAsync()
    {
        if (_dbConnection is not null)
            await _dbConnection.DisposeAsync();

        await _dbContainer.DisposeAsync();
    }
}
