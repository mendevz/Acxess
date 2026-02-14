using Acxess.Catalog;
using Acxess.Identity;
using Acxess.Infrastructure.Extensions;
using Acxess.Infrastructure.Middlewares;
using Acxess.Membership;
using Acxess.Billing;
using Acxess.Billing.Infrastructure.Services;
using Acxess.Catalog.Infrastructure.Services;
using Acxess.Marketing;
using Acxess.Shared.IntegrationEvents.Billing;
using Acxess.Shared.IntegrationEvents.Catalog;
using Acxess.Web.Filters;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.StaticFiles;

var builder = WebApplication.CreateBuilder(args);

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
    });;


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
    Console.WriteLine("--> Iniciando modo MIGRACIÓN...");
    await app.ApplyMigrationsAndSeedsAsync();
    Console.WriteLine("--> Migración finalizada. Cerrando proceso.");
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

app.UseAuthentication();
app.UseAuthorization();

app.UseStaticFiles();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(builder.Environment.ContentRootPath, "Pages")),
    RequestPath = "/js-pages",
    ContentTypeProvider = new FileExtensionContentTypeProvider
    {
        Mappings = { [".js"] = "application/javascript" }
    }
});

app.MapRazorPages();

app.Run();
