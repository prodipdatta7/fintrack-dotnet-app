namespace FinTrack.BuildingBlocks;

/// <summary>
/// Generic result pattern for operations that can succeed or fail.
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string? Error { get; }

    protected Result(bool isSuccess, string? error)
    {
        if (isSuccess && error is not null)
            throw new InvalidOperationException("A successful result cannot have an error.");
        if (!isSuccess && error is null)
            throw new InvalidOperationException("A failed result must have an error.");

        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, null);
    public static Result Failure(string error) => new(false, error);
    public static Result<T> Success<T>(T value) => Result<T>.Success(value);
    public static Result<T> Failure<T>(string error) => Result<T>.Failure(error);
}

public class Result<T> : Result
{
    public T? Value { get; }

    internal Result(T value) : base(true, null) => Value = value;
    internal Result(string error) : base(false, error) => Value = default;

    public static Result<T> Success(T value) => new(value);
    public new static Result<T> Failure(string error) => new(error);
}
