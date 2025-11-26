using CreditCardsSystem.Domain.Enums;

namespace CreditCardsSystem.Domain.Common;

public class DataItem<T>
{
    public T? Data { get; private set; }

    public DataStatus Status { get; private set; } = DataStatus.Uninitialized;
    public DataStatus SubStatus { get; set; } = DataStatus.Uninitialized;


    public Exception? Exception { get; private set; }

    public void SetData(T item)
    {
        Data = item;
        Status = DataStatus.Success;
    }

    public void Error(Exception? exception = null)
    {
        Exception = exception;
        Status = DataStatus.Error;
    }

    public void Loading()
    {
        Exception = null;
        Status = DataStatus.Loading;
        Data = default;
    }

    public void Reset()
    {
        Status = DataStatus.Uninitialized;
        SubStatus = DataStatus.Uninitialized;
    }


    public void ResetSubStatus()
    {
        SubStatus = DataStatus.Uninitialized;
    }
}