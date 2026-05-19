using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Acxess.Web;

public static class ObservabilityExtensions
{
    public static IServiceCollection AddAcxessTelemetry(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(
                    serviceName: "AcxessWeb", 
                    serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString() ?? "1.0.0"
                )
            )
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.Filter = context => 
                        {
                            var path = context.Request.Path.Value?.ToLower();
                            return path != null && !path.Contains('.') && path != "/" && !path.Contains("health");
                        };
                    })
                    .AddEntityFrameworkCoreInstrumentation(options => 
                    {
                        options.SetDbStatementForText = true; 
                    })
                    .AddSource("Acxess.Application")
                    .AddOtlpExporter(options =>
                    {
                        options.Protocol = OtlpExportProtocol.HttpProtobuf;
                        var otelEndpoint = configuration["OpenTelemetry:Endpoint"];
                        var otelToken = configuration["OpenTelemetry:Token"];

                        if (!string.IsNullOrWhiteSpace(otelEndpoint))
                            options.Endpoint = new Uri(otelEndpoint);
                            
                        if (!string.IsNullOrWhiteSpace(otelToken))
                            options.Headers = $"Authorization=Bearer {otelToken}";
                    });
            });

        return services;
    }
}