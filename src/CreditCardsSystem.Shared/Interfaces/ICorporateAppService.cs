using CreditCardsSystem.Data.Models;
using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.CardOperation;
using Refit;

namespace CreditCardsSystem.Domain.Interfaces;
public interface ICorporateAppService : IRefitClient
{
    const string Controller = "/api/Corporate/";

    [Post($"{Controller}{nameof(ProcessProfileRequest)}")]
    Task<ApiResponseModel<ProcessResponse>> ProcessProfileRequest(ProcessCorporateProfileRequest request);

    [Post($"{Controller}{nameof(RequestAddProfile)}")]
    Task<ApiResponseModel<CorporateProfileDto>> RequestAddProfile(CorporateProfileDto profile);

    [Post($"{Controller}{nameof(RequestUpdateProfile)}")]
    Task<ApiResponseModel<CorporateProfileDto>> RequestUpdateProfile(CorporateProfileDto profile);
}
