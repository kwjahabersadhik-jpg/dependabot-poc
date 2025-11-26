using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.CardOperation;
using Refit;

namespace CreditCardsSystem.Domain.Interfaces;

public interface IChangeStatusAppService : IRefitClient
{
    const string Controller = "/api/ChangeStatus/";

    [Post($"{Controller}{nameof(ChangeStatus)}")]
    Task<ApiResponseModel<ChangeStatusResponse>> ChangeStatus(ChangeStatusRequest updateCardRequest);



}
public interface ICardRequestApprovalAppService : IRefitClient
{
    const string Controller = "/api/CardRequestApproval/";

    [Post($"{Controller}{nameof(GetCustomerProfile)}")]
    Task<ApiResponseModel<CreditCardCustomerProfileResponse>> GetCustomerProfile(string civilId);

    [Post($"{Controller}{nameof(ProcessCardRequest)}")]
    Task<ApiResponseModel<ProcessResponse>> ProcessCardRequest(ProcessCardRequest request);

}