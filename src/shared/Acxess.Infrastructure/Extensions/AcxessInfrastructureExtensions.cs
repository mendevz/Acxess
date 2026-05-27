using Acxess.Infrastructure.Services;
using Acxess.Shared.Abstractions;
using Amazon.S3;
using Microsoft.Extensions.Configuration;
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
        services.AddScoped<IImageStorageService, CloudflareR2StorageService>();

        services.AddMediatR(cfg => 
        {
            cfg.RegisterServicesFromAssemblies(moduleAssemblies);
            cfg.RegisterServicesFromAssembly(typeof(AcxessInfrastructureExtensions).Assembly);
            cfg.AddOpenBehavior(typeof(BehaviorsMediatR.LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(BehaviorsMediatR.TracingBehavior<,>));
            cfg.AddOpenBehavior(typeof(BehaviorsMediatR.DatabaseExceptionBehavior<,>));
            cfg.AddOpenBehavior(typeof(BehaviorsMediatR.TransactionalBehavior<,>));
        });

        return services;
    }

    public static IServiceCollection AddS3Client(this IServiceCollection services, IConfigurationSection r2Config)
    {
        var accessKey = r2Config["AccessKey"];
        var secretKey = r2Config["SecretKey"];
        var serviceUrl = r2Config["ServiceURL"];

        var credentials = new Amazon.Runtime.BasicAWSCredentials(accessKey, secretKey);
        var s3Config = new Amazon.S3.AmazonS3Config
        {
            ServiceURL = serviceUrl,
            ForcePathStyle = true
        };
        services.AddSingleton<IAmazonS3>(new Amazon.S3.AmazonS3Client(credentials, s3Config));
        return services;
    }
}
