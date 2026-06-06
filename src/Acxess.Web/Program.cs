using Acxess.Billing;
using Acxess.Billing.Infrastructure.Services;
using Acxess.Catalog;
using Acxess.Catalog.Infrastructure.Services;
using Acxess.Identity;
using Acxess.Infrastructure.Extensions;
using Acxess.Infrastructure.Middlewares;
using Acxess.Marketing;
using Acxess.Membership;
using Acxess.Membership.Application.Features.Subscriptions.Commands.SendDailyExpirationReminders;
using Acxess.Membership.Application.Services;
using Acxess.Shared.IntegrationServices.Billing;
using Acxess.Shared.IntegrationServices.Catalog;
using Acxess.Web;
using Acxess.Web.Filters;
using Destructurama;
using MediatR;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();
try
{
    Log.Information("Starting Acxess Web");
    var builder = WebApplication.CreateBuilder(args);

    var r2Config = builder.Configuration.GetSection("CloudflareR2");
    builder.Services.AddS3Client(r2Config);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Destructure.UsingAttributes()
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
        .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.Hosting.Lifetime", Serilog.Events.LogEventLevel.Information)
    );
    
    builder.Services.AddAcxessTelemetry(builder.Configuration);
    builder.Services.AddDistributedCacheRedis(builder.Configuration);
    builder.Services.AddWhatsAppInfrastructure(builder.Configuration);

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
    app.UseStaticFiles();
    app.UseRouting();
    
    app.UseSerilogRequestLogging(options =>
    {
        options.GetLevel = (httpContext, elapsed, ex) =>
        {
            if (ex != null || httpContext.Response.StatusCode > 499) return Serilog.Events.LogEventLevel.Error;
            if (httpContext.Response.StatusCode == 404) return Serilog.Events.LogEventLevel.Debug;
            
            var path = httpContext.Request.Path.Value?.ToLower();
            if (path == "/" || (path?.StartsWith("/identity/login") == true && httpContext.Request.Method == "GET"))
            {
                return Serilog.Events.LogEventLevel.Debug; 
            }
            return Serilog.Events.LogEventLevel.Information;
        };
    });
    
    app.UseAuthentication();
    app.UseAuthorization();
    
    app.MapRazorPages();

    app.MapPost("/api/membership/subscriptions/check-expiration", async (ISubscriptionService service) =>
    {
        await service.DeactivateExpiredSubscriptionsAsync(CancellationToken.None);
        return Results.Ok(new { message = "Expiration process executed manually." });
    })
    .WithTags("Maintenance");

    app.MapPost("/api/membership/subscriptions/send-report-subscriptions", async (IMediator mediator) =>
    {
        var result = await mediator.Send(new SendDailyExpirationRemindersCommand(), CancellationToken.None);
        return Results.Ok(new { message = "Send report of expired subscriptions executed manually." });
    })
    .WithTags("Maintenance");
    
    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException) 
{
    Log.Fatal(ex, "The Access application crashed and shut down");
    throw;
}
finally
{
    Log.Information("Shutting down Acxess safely...");
    Log.CloseAndFlush(); 
}

public partial class Program { }