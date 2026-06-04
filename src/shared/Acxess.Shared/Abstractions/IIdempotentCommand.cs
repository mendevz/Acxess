namespace Acxess.Shared.Abstractions;

public interface IIdempotentCommand
{
    Guid IdempotencyToken { get; }
}
