using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.CardOperation;
using Refit;

namespace CreditCardsSystem.Domain.Interfaces;

public interface IChangeOfAddressAppService : IRefitClient
{
    const string Controller = "/api/ChangeOfAddress/";

    [Post($"{Controller}{nameof(RequestChangeOfAddress)}")]
    Task<ApiResponseModel<ChangeOfDetailResponse>> RequestChangeOfAddress(ChangeOfAddressRequest request);

    [Post($"{Controller}{nameof(ProcessChangeOfAddressRequest)}")]
    Task<ApiResponseModel<ProcessResponse>> ProcessChangeOfAddressRequest(ProcessChangeOfAddressRequest request);

    [Post($"{Controller}{nameof(RequestChangeCardHolderName)}")]
    Task<ApiResponseModel<ChangeOfDetailResponse>> RequestChangeCardHolderName(ChangeHolderNameRequest request);

    [Post($"{Controller}{nameof(RequestChangeLinkedAccount)}")]
    Task<ApiResponseModel<ChangeOfDetailResponse>> RequestChangeLinkedAccount(ChangeLinkedAccountRequest request);

}
