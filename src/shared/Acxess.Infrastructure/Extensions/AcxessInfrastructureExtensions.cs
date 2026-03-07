using Acxess.Infrastructure.Services;
using Acxess.Shared.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Acxess.Infrastructure.Extensions;

public static class AcxessInfrastructureExtensions
{
    public static IServiceCollection AddAcxessInfrastructure(
        this IServiceCollection services,
        params System.Reflection.Assembly[] moduleAssemblies)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentTenant, CurrentTenantService>();
        services.AddScoped<IImageStorageService, LocalDiskStorageService>();

        services.AddMediatR(cfg => 
        {
            cfg.RegisterServicesFromAssemblies(moduleAssemblies);
            cfg.RegisterServicesFromAssembly(typeof(AcxessInfrastructureExtensions).Assembly);
            cfg.AddOpenBehavior(typeof(BehaviorsMediatR.DatabaseExceptionBehavior<,>));
            cfg.AddOpenBehavior(typeof(BehaviorsMediatR.TransactionalBehavior<,>));
            cfg.AddOpenBehavior(typeof(BehaviorsMediatR.LoggingBehavior<,>));
        });

        return services;
    }
}
