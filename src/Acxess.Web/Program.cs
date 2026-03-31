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

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Acxess Web");
    
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
        .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.Hosting.Lifetime", Serilog.Events.LogEventLevel.Information)
    );

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

    app.UseSerilogRequestLogging(options =>
    {
        options.GetLevel = (httpContext, elapsed, ex) =>
        {
            if (ex != null || httpContext.Response.StatusCode > 499)
            {
                return Serilog.Events.LogEventLevel.Error;
            }

            var path = httpContext.Request.Path.Value?.ToLower();

            if (path != null && (
                    path.EndsWith(".css") || 
                    path.EndsWith(".js") || 
                    path.EndsWith(".png") || 
                    path.EndsWith(".jpg") ||     
                    path.EndsWith(".jpeg") ||    
                    path.EndsWith(".ico") || 
                    path.EndsWith(".map") || 
                    path.EndsWith(".json") ||    
                    path.StartsWith("/uploads/") || 
                    (path.StartsWith("/identity/login") && httpContext.Request.Method == "GET") ||
                    path == "/sw.js") || 
                (path == "/" && httpContext.Response.StatusCode < 400))
            {
                return Serilog.Events.LogEventLevel.Debug; 
            }

            
            return Serilog.Events.LogEventLevel.Information;
        };
    });

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