using CreditCardsSystem.Data.Models;
using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.Corporate;
using Refit;

namespace CreditCardsSystem.Domain.Interfaces;

public interface ICorporateProfileAppService : IRefitClient
{
    const string Controller = "/api/CorporateProfile/";

    [Get($"{Controller}{nameof(GetProfileForEdit)}")]
    Task<ApiResponseModel<CorporateProfileDto>> GetProfileForEdit(string civilId, bool includeValidation = true);

    [Get($"{Controller}{nameof(GetProfile)}")]
    Task<ApiResponseModel<CorporateProfileDto>> GetProfile(string civilId);


    [Get($"{Controller}{nameof(DeleteProfileInFdR)}")]
    Task<ApiResponseModel<CorporateProfileDto>> DeleteProfileInFdR(string civilId);

    Task IsExpired(GlobalLimitDto card);
    Task<GlobalLimitDto> GetAndValidateGlobalLimit(string corporateCivilId);
}
