using Serilog;
using Acxess.Catalog;
using Acxess.Identity;
using Acxess.Infrastructure.Extensions;
using Acxess.Infrastructure.Middlewares;
using Acxess.Membership;
using Acxess.Billing;
using Acxess.Billing.Infrastructure.Services;
using Acxess.Catalog.Infrastructure.Services;
using Acxess.Marketing;
using Acxess.Membership.Application.Services;
using Acxess.Shared.IntegrationServices.Billing;
using Acxess.Shared.IntegrationServices.Catalog;
using Acxess.Web.Filters;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.StaticFiles;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Acxess Web");
    
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services));

    builder.Services
        .AddExceptionHandler<GlobalExceptionHandler>()
        .AddProblemDetails();

    builder.Services.AddScoped<PageExceptionFilter>();

    builder.Services.AddRazorPages(options =>
    {
        options.Conventions.AuthorizeFolder("/");
        options.Conventions.AllowAnonymousToPage("/Identity/Login");
        options.Conventions.AllowAnonymousToPage("/Identity/RegisterTenant");
    })
    .AddMvcOptions(options =>
    {
        options.Filters.Add<PageExceptionFilter>();
    });

    var modulesAssemblies = new[]
    {
        typeof(IdentityModuleExtensions).Assembly,
        typeof(MarketingModuleExtensions).Assembly,
        typeof(MembershipModuleExtensions).Assembly,
        typeof(BillingModuleExtensions).Assembly,
        typeof(CatalogModuleExtensions).Assembly,
        typeof(Program).Assembly
    };
    
    builder.Services.AddAcxessInfrastructure(modulesAssemblies);

    builder.Services.AddIdentityModule(builder.Configuration);
    builder.Services.AddCatalogModule(builder.Configuration);
    builder.Services.AddMembershipModule(builder.Configuration);
    builder.Services.AddBillingModule(builder.Configuration);
    builder.Services.AddMarketingModule(builder.Configuration);

    // integrations services
    builder.Services.AddScoped<ICatalogIntegrationService, CatalogIntegrationService>();
    builder.Services.AddScoped<IBillingIntegrationService, BillingIntegrationService>();

    var app = builder.Build();

    app.UseSerilogRequestLogging();

    if (args.Contains("--migrate-only"))
    {
        Log.Information("--> Starting MIGRATION mode...");
        await app.ApplyMigrationsAndSeedsAsync();
        Log.Information("--> Migration completed. Closing process.");
        return;
    }

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error");
        app.UseHsts();
    }
    else
    {
        app.UseDeveloperExceptionPage();
    }

    app.UseHttpsRedirection();
    app.UseRouting();
    
    app.MapPost("/api/membership/subscriptions/check-expiration", async (ISubscriptionService service) =>
        {
            await service.DeactivateExpiredSubscriptionsAsync(CancellationToken.None);
            return Results.Ok(new { message = "Expiration process executed manually." });
        })
        .WithTags("Maintenance");

    app.UseAuthentication();
    app.UseAuthorization();

    app.UseStaticFiles();

    app.MapRazorPages();

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException) // Ignora el error falso al correr herramientas de EF Core
{
    Log.Fatal(ex, "The Access application crashed and shut down");
}
finally
{
    Log.Information("Shutting down Acxess safely...");
    Log.CloseAndFlush(); 
}