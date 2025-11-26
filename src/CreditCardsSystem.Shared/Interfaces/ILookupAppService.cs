using CreditCardsSystem.Data.Models;
using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.Card;
using CreditCardsSystem.Domain.Models.CardIssuance;
using CreditCardsSystem.Domain.Models.CoBrand;
using Kfh.Aurora.Organization;
using Refit;

namespace CreditCardsSystem.Domain.Interfaces;

public interface ILookupAppService : IRefitClient
{
    const string Controller = "/api/Lookup/";

    [Get($"{Controller}{nameof(GetAllBranches)}")]
    Task<ApiResponseModel<List<Branch>>> GetAllBranches();

    [Get($"{Controller}{nameof(GetAllCompanies)}")]
    Task<ApiResponseModel<List<CompanyDto>>> GetAllCompanies();

    [Get($"{Controller}{nameof(GetCardCurrencies)}")]
    Task<ApiResponseModel<List<CardCurrencyDto>>> GetCardCurrencies();

    [Get($"{Controller}{nameof(GetAllProducts)}")]
    Task<ApiResponseModel<List<CardDefinitionDto>>> GetAllProducts();

    [Get($"{Controller}{nameof(GetAreaCodes)}")]
    Task<ApiResponseModel<List<AreaCodesDto>>> GetAreaCodes();
    [Get($"{Controller}{nameof(GetRelationships)}")]
    Task<ApiResponseModel<List<Relationship>>> GetRelationships();

    [Get($"{Controller}{nameof(GetCardStatus)}")]
    Task<ApiResponseModel<CardStatusList>> GetCardStatus(StatusType type = StatusType.All);
}
