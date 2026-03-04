using Acxess.Membership.Application.Services;
using Acxess.Membership.Infrastructure.BackgroundJobs;
using Acxess.Membership.Infrastructure.Persistence;
using Acxess.Shared.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Acxess.Membership;

public static class MembershipModuleExtensions
{
    public static IServiceCollection AddMembershipModule(this IServiceCollection services, IConfiguration configuration)
    {
        
        var connectionString = configuration.GetConnectionString("Default");

        services.AddDbContext<MembershipModuleContext>(options =>
        {
            options.UseSqlServer(connectionString, sqlOptions =>
                sqlOptions.MigrationsHistoryTable("__MembershipMigrationsHistory", "Membership")
            );
        });

        services.AddScoped<IDataSeeder, MembershipSeeder>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        services.AddHostedService<SubscriptionExpirationWorker>();

        return services;
    }

}
