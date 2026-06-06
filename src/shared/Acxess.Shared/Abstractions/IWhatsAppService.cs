namespace Acxess.Shared.Abstractions;

public interface IWhatsAppService
{
    Task<bool> SendTextMessageAsync(string phoneNumber, string message, CancellationToken cancellationToken = default);
}
