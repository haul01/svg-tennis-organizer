namespace TennisClub.Api.Common.Results;

public enum ResultStatus
{
    Success,
    Unauthorized,
    Invalid,
    NotFound,
    Conflict
}

public record ValidationFailure(string Code, string Message);

public class Result
{
    public ResultStatus Status { get; init; } = ResultStatus.Success;
    public string? Error { get; init; }
    public IReadOnlyList<ValidationFailure>? Failures { get; init; }

    public bool IsSuccess => Status == ResultStatus.Success;

    public static Result Success() => new() { Status = ResultStatus.Success };
    public static Result<T> Success<T>(T value) =>
        new() { Status = ResultStatus.Success, Value = value };

    public static Result Unauthorized(string? message = null) =>
        new() { Status = ResultStatus.Unauthorized, Error = message };

    public static Result NotFound(string? message = null) =>
        new() { Status = ResultStatus.NotFound, Error = message };

    public static Result Invalid(string message) =>
        new() { Status = ResultStatus.Invalid, Error = message };

    public static Result Invalid(IReadOnlyList<ValidationFailure> failures) =>
        new() { Status = ResultStatus.Invalid, Failures = failures };

    public static Result Conflict(string message) =>
        new() { Status = ResultStatus.Conflict, Error = message };
}

/// <summary>
/// Generic result carrying a value on success.
/// Intentionally not derived from Result: the implicit conversion from Result
/// lets handlers return non-generic failure helpers (e.g. Result.Unauthorized())
/// from methods declared as Task&lt;Result&lt;T&gt;&gt;.
/// </summary>
public class Result<T>
{
    public ResultStatus Status { get; init; } = ResultStatus.Success;
    public T? Value { get; init; }
    public string? Error { get; init; }
    public IReadOnlyList<ValidationFailure>? Failures { get; init; }

    public bool IsSuccess => Status == ResultStatus.Success;

    public static implicit operator Result<T>(Result r) => new()
    {
        Status = r.Status,
        Error = r.Error,
        Failures = r.Failures
    };
}
