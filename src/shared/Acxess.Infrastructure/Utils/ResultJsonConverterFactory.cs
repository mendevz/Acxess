using Acxess.Shared.ResultManager;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Acxess.Infrastructure.Utils;

public class ResultJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        if (typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(Result<>))
            return true;

        return typeToConvert == typeof(Result);
    }

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        if (typeToConvert.IsGenericType)
        {
            var valueType = typeToConvert.GetGenericArguments()[0];
            var converterType = typeof(ResultConverter<>).MakeGenericType(valueType);
            return (JsonConverter)Activator.CreateInstance(converterType)!;
        }

        return new ResultConverter();
    }
}

public class ResultConverter<T> : JsonConverter<Result<T>>
{
    public override Result<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        bool isSuccess = root.GetProperty("IsSuccess").GetBoolean();

        if (isSuccess)
        {
            var valueRaw = root.GetProperty("Value").GetRawText();
            var value = JsonSerializer.Deserialize<T>(valueRaw, options);
            return Result<T>.Success(value!);
        }
        else
        {
            var errorRaw = root.GetProperty("Error").GetRawText();
            var error = JsonSerializer.Deserialize<Error>(errorRaw, options);
            return Result<T>.Failure(error!);
        }
    }

    public override void Write(Utf8JsonWriter writer, Result<T> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteBoolean("IsSuccess", value.IsSuccess);
        writer.WriteBoolean("IsFailure", value.IsFailure);

        writer.WritePropertyName("Error");
        JsonSerializer.Serialize(writer, value.Error, options);

        if (value.IsSuccess)
        {
            writer.WritePropertyName("Value");
            JsonSerializer.Serialize(writer, value.Value, options);
        }

        writer.WriteEndObject();
    }
}

public class ResultConverter : JsonConverter<Result>
{
    public override Result Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        bool isSuccess = root.GetProperty("IsSuccess").GetBoolean();

        if (isSuccess) return Result.Success();

        var errorRaw = root.GetProperty("Error").GetRawText();
        var error = JsonSerializer.Deserialize<Error>(errorRaw, options);
        return Result.Failure(error!);
    }

    public override void Write(Utf8JsonWriter writer, Result value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteBoolean("IsSuccess", value.IsSuccess);
        writer.WriteBoolean("IsFailure", value.IsFailure);

        writer.WritePropertyName("Error");
        JsonSerializer.Serialize(writer, value.Error, options);

        writer.WriteEndObject();
    }
}