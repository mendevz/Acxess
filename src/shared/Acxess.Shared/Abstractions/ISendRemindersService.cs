namespace Acxess.Shared.Abstractions;

public interface ISendRemindersService
{
    Task<bool> SendTextMessageAsync(string phoneNumber, string message, CancellationToken cancellationToken = default);
}
