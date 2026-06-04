using Acxess.Infrastructure.Utils;
using Acxess.Shared.Abstractions;
using MediatR;
using StackExchange.Redis;
using System.Text.Json;

namespace Acxess.Infrastructure.BehaviorsMediatR;

public class IdempotencyBehavior<TRequest, TResponse> (IConnectionMultiplexer redis)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : IIdempotentCommand
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var db = redis.GetDatabase();
        var cacheIdempotencyKey = $"idempotency:{request.IdempotencyToken}";

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new ResultJsonConverterFactory() }
        };

        var cachedResult = await db.StringGetAsync(cacheIdempotencyKey);

        if(cachedResult.HasValue && cachedResult != "PROCESSING")
        {
            return JsonSerializer.Deserialize<TResponse>(cachedResult!, jsonOptions)!;
        }

        bool isFirstToProcess = await db.StringSetAsync(
            cacheIdempotencyKey,
            "PROCESSING",
            TimeSpan.FromMinutes(2),
            When.NotExists);


        if (!isFirstToProcess)
        {
            throw new InvalidOperationException("Esta transacción ya está siendo procesada. Por favor, espera.");
        }

        var response = await next();

        await db.StringSetAsync(
            cacheIdempotencyKey,
            JsonSerializer.Serialize(response),
            TimeSpan.FromHours(3));

        return response;
    }
}
