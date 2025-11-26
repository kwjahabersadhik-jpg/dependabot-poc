using CreditCardsSystem.Domain.Common;

namespace CreditCardsSystem.Domain.Models;

public class ApiResponseModel<TModel> where TModel : class?
{
    public bool IsSuccess { get; set; } = true;
    public bool IsInternalException { get; set; } = false;
    public string Message { get; set; } = string.Empty;
    public TModel? Data { get; set; } = null;
    public List<ValidationError> ValidationErrors { get; set; } = new();

    public (bool IsSuccess, string Message) GetResult()
    {
        return (IsSuccess, Message);
    }

    public bool IsSuccessWithData
    {
        get
        {
            return IsSuccess && Data is not null;
        }
    }
    public ApiResponseModel()
    {
    }

    public ApiResponseModel<TModel> Success(TModel? data, string message = "")
    {
        return new ApiResponseModel<TModel>
        {
            Data = data,
            Message = message
        };
    }

    public ApiResponseModel<TModel> Fail(string message = "", List<ValidationError> validationErrors = null!)
    {
        return new ApiResponseModel<TModel>
        {
            Data = null,
            IsSuccess = false,
            Message = string.IsNullOrEmpty(message) ? "Failed" : message,
            ValidationErrors = validationErrors
        };
    }
}


public class ApiResponseModel
{
    public bool IsSuccess { get; set; } = true;
    public bool IsInternalException { get; set; } = false;
    public string Message { get; set; } = string.Empty;
    public List<ValidationError> ValidationErrors { get; set; } = new();

    public (bool IsSuccess, string Message) GetResult()
    {
        return (IsSuccess, Message);
    }


    public ApiResponseModel()
    {
    }

    public ApiResponseModel Success(string message = "")
    {
        return new ApiResponseModel
        {
            Message = message
        };
    }

    public ApiResponseModel Fail(string message = "", List<ValidationError> validationErrors = null!)
    {
        return new ApiResponseModel
        {
            IsSuccess = false,
            Message = string.IsNullOrEmpty(message) ? "Failed" : message,
            ValidationErrors = validationErrors
        };
    }
}
