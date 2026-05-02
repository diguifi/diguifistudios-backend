namespace Diguifi.Application.Common;

public sealed class Result<T>
{
    private Result(bool isSuccess, T? value, ErrorContract? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public bool IsSuccess { get; }
    public T? Value { get; }
    public ErrorContract? Error { get; }

    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(string code, string message, params string[] details)
        => new(false, default, new ErrorContract(code, message, details));
}
