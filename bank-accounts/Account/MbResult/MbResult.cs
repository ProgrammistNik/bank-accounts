using JetBrains.Annotations;

namespace bank_accounts.Account.MbResult;

// Resharper жаловался на не использование isSuccess, Value и Error, поэтому добавил атрибуты [UsedImplicitly] к методам.
public class MbResult<T>
{
    // Resharper жаловался на не использование isSuccess, Value и Error, но они используются через рефлексию
    [UsedImplicitly]
    public bool IsSuccess { get; }
    [UsedImplicitly]
    public T? Value { get; }
    [UsedImplicitly]
    public string? Error { get; }

    private MbResult(T value)
    {
        IsSuccess = true;
        Value = value;
    }

    private MbResult(string error)
    {
        IsSuccess = false;
        Error = error;
    }

    public static MbResult<T> Success(T value) => new(value);
    [UsedImplicitly]
    public static MbResult<T> Failure(string error) => new(error); 
}
