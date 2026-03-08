namespace KH2.ManagementSystem.BuildingBlocks.Results;

public class Result
{
    protected Result(bool isSuccess, AppError error)
    {
        if (isSuccess && error != AppError.None)
        {
            throw new ArgumentException("A successful result cannot contain an error.", nameof(error));
        }

        if (!isSuccess && error == AppError.None)
        {
            throw new ArgumentException("A failed result must contain an error.", nameof(error));
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public AppError Error { get; }

    public static Result Success()
    {
        return new Result(true, AppError.None);
    }

    public static Result Failure(AppError error)
    {
        return new Result(false, error);
    }

    public static Result<TValue> Success<TValue>(TValue value)
    {
        return new Result<TValue>(value, true, AppError.None);
    }

    public static Result<TValue> Failure<TValue>(AppError error)
    {
        return new Result<TValue>(default, false, error);
    }
}

public sealed class Result<TValue> : Result
{
    internal Result(TValue? value, bool isSuccess, AppError error)
        : base(isSuccess, error)
    {
        Value = value;
    }

    public TValue? Value { get; }
}
