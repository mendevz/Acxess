namespace Acxess.Shared.ResultManager;


public interface IResult 
{
    bool IsFailure { get; }
    Error Error { get; } 
}
public class Result : IResult
{
    protected Result(bool isSuccess, Error error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);
    public static Result Failure(string code, string error) => new(false, new Error(code, error, ErrorType.Failure));
}

public class Result<T> : Result
{
    private readonly T? _value;

    private Result(T? value, bool isSuccess, Error error) : base(isSuccess, error)
    {
        _value = value;
    }

    public T Value => IsSuccess ? _value! : throw new InvalidOperationException("No hay valor en un resultado fallido.");

    public static Result<T> Success(T value) => new(value, true, Error.None);
    public new static Result<T> Failure(Error error) => new(default, false, error);
    public new static Result<T> Failure(string code, string error) => new(default, false, new Error(code, error, ErrorType.Failure));
    
    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(Error error) => Failure(error);
}