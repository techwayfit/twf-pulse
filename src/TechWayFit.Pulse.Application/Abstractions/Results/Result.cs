namespace TechWayFit.Pulse.Application.Abstractions.Results;

public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
    Unauthorized,
    Forbidden,
    RateLimited,
    Unexpected
}

public sealed record Error(string Code, string Message, ErrorType Type = ErrorType.Validation);

public class Result
{
    public bool IsSuccess { get; }

    public Error? Error { get; }

    protected Result(bool isSuccess, Error? error)
    {
        if (isSuccess && error is not null)
        {
            throw new ArgumentException("Successful result cannot contain an error.", nameof(error));
        }

        if (!isSuccess && error is null)
        {
            throw new ArgumentException("Failed result must contain an error.", nameof(error));
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, null);

    public static Result Failure(Error error) => new(false, error);
}

public sealed class Result<T> : Result
{
    public T? Value { get; }

    private Result(bool isSuccess, T? value, Error? error)
        : base(isSuccess, error)
    {
        Value = value;
    }

    public static Result<T> Success(T value) => new(true, value, null);

    public new static Result<T> Failure(Error error) => new(false, default, error);
}

public static class ResultErrors
{
    public static Error Validation(string message, string code = "validation_error")
        => new(code, message, ErrorType.Validation);

    public static Error Unauthorized(string message, string code = "unauthorized")
        => new(code, message, ErrorType.Unauthorized);

    public static Error RateLimited(string message, string code = "rate_limited")
        => new(code, message, ErrorType.RateLimited);

    public static Error Unexpected(string message, string code = "unexpected_error")
        => new(code, message, ErrorType.Unexpected);
}
