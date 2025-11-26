using CreditCardsSystem.Domain.Interfaces;
using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Shared.Models.BCDPromotions.Beneficiaries;
using Refit;

namespace CreditCardsSystem.Domain.Shared.Interfaces;

public interface IBeneficiaryAppService : IRefitClient
{
    const string Controller = "/api/Beneficiary/";

    [Get($"{Controller}{nameof(Get)}")]
    Task<ApiResponseModel<List<BeneficiaryDto>>> Get(string civilId);

    [Post($"{Controller}{nameof(Delete)}")]
    Task<ApiResponseModel<object>> Delete([Body] BeneficiaryDto beneficiaryDto);
}