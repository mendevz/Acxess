using Acxess.Shared.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace Acxess.Infrastructure.Services;

public class SmsService : ISendRemindersService
{
    private readonly ILogger<SmsService> _logger;
    private readonly string _fromPhoneNumber = "";

    public SmsService(IConfiguration configuration, ILogger<SmsService> logger)
    {
        _logger = logger;

        var accountSid = configuration["Twilio:AccountSid"];
        var authToken = configuration["Twilio:AuthToken"];
        _fromPhoneNumber = configuration["Twilio:FromPhoneNumber"] ?? "";

        if (!string.IsNullOrEmpty(accountSid) && !string.IsNullOrEmpty(authToken))
        {
            TwilioClient.Init(accountSid, authToken);
        }
        else
        {
            _logger.LogWarning("Twilio credentials are not fully configured in appsettings.");
        }
    }

    public async Task<bool> SendTextMessageAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(_fromPhoneNumber))
            {
                _logger.LogError("Twilio FromPhoneNumber is not configured.");
                return false;
            }

            var messageOptions = new CreateMessageOptions(new PhoneNumber(phoneNumber))
            {
                From = new PhoneNumber(_fromPhoneNumber),
                Body = message
            };

            _logger.LogInformation("Sending SMS to {PhoneNumber}", phoneNumber);

            var response = await MessageResource.CreateAsync(messageOptions);

            if (response.ErrorCode.HasValue)
            {
                _logger.LogError("Failed to send SMS to {PhoneNumber}. ErrorCode: {ErrorCode}, ErrorMessage: {ErrorMessage}", phoneNumber, response.ErrorCode, response.ErrorMessage);
                return false;
            }

            _logger.LogInformation("Successfully sent SMS to {PhoneNumber}. Sid: {Sid}", phoneNumber, response.Sid);
            return true;
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Exception while sending SMS to {PhoneNumber}", phoneNumber);
            return false;
        }
    }
}
