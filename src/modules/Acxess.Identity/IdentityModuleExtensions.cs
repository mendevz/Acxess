using Acxess.Identity.Domain.Entities;
using Acxess.Identity.Infrastructure.Identity;
using Acxess.Identity.Infrastructure.Persistence;
using Acxess.Identity.Infrastructure.Services;
using Acxess.Shared.Abstractions;
using Acxess.Shared.IntegrationServices;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
namespace Acxess.Identity;

public static class IdentityModuleExtensions
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default");

        services.AddDbContext<IdentityModuleContext>(options =>
        {
            options.UseSqlServer(connectionString, sqlOptions =>
                sqlOptions.MigrationsHistoryTable("__IdentityMigrationsHistory", "Identity")
            )
            .ConfigureWarnings(w => w.Throw(RelationalEventId.MultipleCollectionIncludeWarning)); ;
        });

        services.AddApplicationIdentity(configuration);

        services.AddScoped<IDataSeeder, IdentitySeeder>();
        services.AddScoped<IIdentityIntegrationService, IdentityIntegrationService>();
        services.AddScoped<ITimeService, TimeService>();
        return services;
    }

    private static IServiceCollection AddApplicationIdentity(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.Password.RequireDigit = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false; 
            options.Password.RequiredLength = 1;            
            options.Password.RequiredUniqueChars = 0;        
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<IdentityModuleContext>()
        .AddDefaultTokenProviders()
        .AddClaimsPrincipalFactory<ApplicationUserClaimsPrincipalFactory>();

        string redisConnectionString = configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("Redis connection string is missing.");

        var redis = ConnectionMultiplexer.Connect(redisConnectionString);

        services.AddDataProtection()
                .PersistKeysToStackExchangeRedis(redis, "Acxess:DataProtection:Keys")
                .SetApplicationName("AcxessApp");

        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = "Acxess.Auth.Session"; // Un nombre limpio para tu PWA
            options.LoginPath = "/Identity/Login";
            options.LogoutPath = "/Identity/Logout";
            options.AccessDeniedPath = "/Identity/AccessDenied";

            options.ExpireTimeSpan = TimeSpan.FromDays(30);
            options.SlidingExpiration = true;
            options.Cookie.HttpOnly = true;

            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        });

        services.Configure<SecurityStampValidatorOptions>(options =>
        {
            options.ValidationInterval = TimeSpan.FromDays(7);
        });

        return services;
    }
}
