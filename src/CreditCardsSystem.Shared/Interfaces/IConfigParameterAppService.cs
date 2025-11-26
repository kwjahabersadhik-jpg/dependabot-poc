using CreditCardsSystem.Domain.Interfaces;
using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Shared.Models.BCDPromotions.ConfigParameter;
using Refit;

namespace CreditCardsSystem.Domain.Shared.Interfaces;

public interface IConfigParameterAppService : IRefitClient
{
    const string Controller = "/api/ConfigParameter/";

    [Get($"{Controller}{nameof(Get)}")]
    Task<ApiResponseModel<List<ConfigParameterDto>>> Get();


    [Get($"{Controller}{nameof(GetByKey)}")]
    Task<ApiResponseModel<List<ConfigParameterDto>>> GetByKey(string key);


    [Get($"{Controller}{nameof(GetByStartsWith)}")]
    Task<ApiResponseModel<List<ConfigParameterDto>>> GetByStartsWith(string key);


    [Post($"{Controller}{nameof(Update)}")]
    Task<ApiResponseModel<ConfigParameterDto>> Update([Body] ConfigParameterDto parameterDto);


}