using CreditCardsSystem.Data.Models;
using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.Card;
using CreditCardsSystem.Domain.Models.CardIssuance;
using CreditCardsSystem.Domain.Models.SupplementaryCard;
using Refit;

namespace CreditCardsSystem.Domain.Interfaces;

public interface ICardDetailsAppService : IRefitClient
{
    const string Controller = "/api/CardDetails/";

    [Get($"{Controller}{nameof(GetCardInfoMinimal)}")]
    Task<ApiResponseModel<CardDetailsMinimal>> GetCardInfoMinimal(decimal? requestId, string cardNumber = "");

    [Get($"{Controller}{nameof(GetCardInfo)}")]
    Task<ApiResponseModel<CardDetailsResponse>> GetCardInfo(decimal? requestId, string cardNumber = "", bool includeCardBalance = false, string kfhId = "", CancellationToken cancellationToken = default);

    [Get($"{Controller}{nameof(GetSupplementaryCardsByRequestId)}")]
    Task<ApiResponseModel<List<SupplementaryCardDetail>>> GetSupplementaryCardsByRequestId(decimal primaryCardRequestId, CancellationToken cancellationToken = default);

    [Get($"{Controller}{nameof(GetMasterSecondaryCardDetails)}")]
    Task<ApiResponseModel<SecondaryCardDetail>> GetMasterSecondaryCardDetails(decimal requestId);

    [Get($"{Controller}{nameof(GetSupplementaryCardsByCivilId)}")]
    Task<ApiResponseModel<List<SupplementaryCardDetail>>> GetSupplementaryCardsByCivilId(string civilId, int? payeeTypeId = null);

    [Get($"{Controller}{nameof(GetCardDefinitionExtensionsByProductId)}")]
    Task<ApiResponseModel<CardDefinitionExtentionDto>> GetCardDefinitionExtensionsByProductId(int productId);

    //[Get($"{Controller}{nameof(GetCardInfo)}")]
    //Task<ApiResponseModel<CardDetailsResponse>> GetCardInfo(decimal? requestId, string cardNumber = "");

    Task<RequestParameterDto> GetRequestParameter(Request cardRequest);
    Task<int> GetPayeeProductType(int? cardType);

    [Get($"{Controller}{nameof(GetIssuedSupplementaryCardCounts)}")]
    Task<IssuedCardCounts> GetIssuedSupplementaryCardCounts(string CivilID, int productId);

    [Get($"{Controller}{nameof(GetCardWithExtension)}")]
    Task<CardDefinitionDto> GetCardWithExtension(int productId);
}


