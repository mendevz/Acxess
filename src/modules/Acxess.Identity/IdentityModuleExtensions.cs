using Acxess.Identity.Domain.Absractions;
using Acxess.Identity.Domain.Entities;
using Acxess.Identity.Infrastructure.Identity;
using Acxess.Identity.Infrastructure.Persistence;
using Acxess.Identity.Infrastructure.Persistence.Repositories;
using Acxess.Shared.Abstractions;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
            );
        });

        services.AddApplicationIdentity();

        services.AddScoped<IDataSeeder, IdentitySeeder>();
        services.AddScoped<IIdentityUnitOfWork, IdentityUnitOfWork>();
        services.AddScoped<ITenantRepository, TenantRepository>();

        return services;
    }

    private static IServiceCollection AddApplicationIdentity(this IServiceCollection services)
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

        services.AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        }).AddCookie(options =>
        {
            options.LoginPath = "/Identity/Login"; 
            options.LogoutPath = "/Identity/Logout";
            options.ExpireTimeSpan = TimeSpan.FromDays(30);
            options.SlidingExpiration = true;
            options.Cookie.HttpOnly = true;
            options.AccessDeniedPath = "/Identity/AccessDenied";
        });


        return services;
    }
}
