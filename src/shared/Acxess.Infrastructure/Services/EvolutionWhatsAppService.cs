using Acxess.Shared.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Acxess.Infrastructure.Services;

public record WhatsAppTextMessageRequest
{
    [JsonPropertyName("number")]
    public string Number { get; init; } = string.Empty;

    [JsonPropertyName("text")]
    public string Text { get; init; } = string.Empty;
}

public class EvolutionWhatsAppService(
    HttpClient httpClient, 
    IConfiguration configuration, 
    ILogger<EvolutionWhatsAppService> logger) : IWhatsAppService
{
    private readonly string _instanceName = configuration["WhatsApp:InstanceName"] ?? "AcxessBotDev";

    public async Task<bool> SendTextMessageAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
    {
        try
        {
            var cleanNumber = phoneNumber.Replace("+", "").Replace(" ", "").Trim();

            var payload = new WhatsAppTextMessageRequest
            {
                Number = cleanNumber,
                Text = message
            };

            var endpoint = $"/message/sendText/{_instanceName}";
            var response = await httpClient.PostAsJsonAsync(endpoint, payload, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation("WhatsApp message successfully sent to {Number} through instance {Instance}", cleanNumber, _instanceName);
                return true;
            }

            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError("Failed to send WhatsApp to {Number}. Status: {StatusCode}. Response: {Response}",
                cleanNumber, response.StatusCode, errorContent);

            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An unexpected error occurred while sending WhatsApp to {PhoneNumber}", phoneNumber);
            return false;
        }
    }
}
