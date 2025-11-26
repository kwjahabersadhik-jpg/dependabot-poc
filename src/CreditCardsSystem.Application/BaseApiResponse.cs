using CreditCardsSystem.Domain.Common;
using Kfh.Aurora.Logging;
using Serilog;
using System.Runtime.CompilerServices;
namespace CreditCardsSystem.Application;

public abstract class BaseApiResponse
{

    public static ApiResponseModel<TModel> Success<TModel>(TModel data, string message = "") where TModel : class
    {
        return new ApiResponseModel<TModel>().Success(data, message);
    }

    public static ApiResponseModel<TModel> Failure<TModel>(string message = "", List<ValidationError> validationErrors = null!) where TModel : class
    {
        return new ApiResponseModel<TModel>().Fail(message, validationErrors);
    }

    public static ApiResponseModel Success(string message = "")
    {
        return new ApiResponseModel().Success(message);
    }

    public static ApiResponseModel Failure(string message = "", List<ValidationError> validationErrors = null!)
    {
        return new ApiResponseModel().Fail(message, validationErrors);
    }


    //_auditLogger.Log.Error(GlobalResources.LogTemplate, requestDetail.CivilId, "Card Activation", message);

    //_auditLogger.Log.Information(GlobalResources.LogTemplate, requestDetail.CivilId, "Card Activation", message);
}


public abstract class BaseRequestActivity : BaseApiResponse
{
    private readonly IRequestActivityAppService requestActivityAppService;

    public BaseRequestActivity(IRequestActivityAppService requestActivityAppService)
    {
        this.requestActivityAppService = requestActivityAppService;
    }


}
