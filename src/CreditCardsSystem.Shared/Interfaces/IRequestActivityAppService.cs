using CreditCardsSystem.Data.Models;
using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.CardOperation;
using CreditCardsSystem.Domain.Models.Request;
using CreditCardsSystem.Domain.Models.RequestActivity;
using CreditCardsSystem.Domain.Shared.Models.RequestActivity;
using Refit;

namespace CreditCardsSystem.Domain.Interfaces;

public interface IRequestActivityAppService : IRefitClient
{
    const string Controller = "/api/RequestActivity/";

    [Get($"{Controller}{nameof(GetNonEnigmaRequestActivities)}")]
    Task<ApiResponseModel<List<RequestActivityResult>>> GetNonEnigmaRequestActivities();

    [Get($"{Controller}{nameof(GetRequestActivityById)}")]
    Task<ApiResponseModel<RequestActivityDto>> GetRequestActivityById(decimal requestActivityId, bool validateChecker = false);
    Task<decimal> LogRequestActivity(RequestActivityDto requestActivity, bool searchExist = true, bool isNeedWorkflow = false, bool onlyWorkflow = false);
    Task UpdateRequestActivityDetails(decimal requestActivityId, Dictionary<string, string> Details);
    Task UpdateRequestActivityStatus(RequestActivityDto request, decimal? kfhId = null);

    [Post($"{Controller}{nameof(GetAllRequestActivity)}")]
    Task<ApiResponseModel<List<RequestActivityResult>>> GetAllRequestActivity(RequestActivityFilter filter);

    [Post($"{Controller}{nameof(SearchActivity)}")]
    Task<ApiResponseModel<List<RequestActivity>>> SearchActivity(RequestActivityDto request);

    [Post($"{Controller}{nameof(GetPendingActivities)}")]
    Task<ApiResponseModel<List<RequestActivityDto>>> GetPendingActivities(PendingActivityRequest request);

    //Task CompleteActivity(CompleteActivityRequest request);
    Task CompleteActivity(ActivityProcessRequest request, bool isFromSSO = false);

    [Post($"{Controller}{nameof(InitiateWorkFlow)}")]
    Task InitiateWorkFlow(RequestActivityDto requestActivity);
    Task UpdateSingleRequestActivityDetail(decimal requestActivityId, string parameter, string value);
    Task ValidateActivityWithWorkflow(ActivityProcessRequest request);
}
