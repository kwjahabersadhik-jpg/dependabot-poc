using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.CardIssuance;
using CreditCardsSystem.Domain.Shared.Models.Card;
using Refit;

namespace CreditCardsSystem.Domain.Interfaces;

public interface ICurrencyAppService : IRefitClient
{
    const string Controller = "/api/Currency/";


    [Get($"{Controller}{nameof(ValidateCurrencyRate)}")]
    Task<ApiResponseModel<ValidateCurrencyResponse>> ValidateCurrencyRate(ValidateCurrencyRequest request);

    [Get($"{Controller}{nameof(GetCardCurrency)}")]
    Task<CardCurrencyDto> GetCardCurrency(int cardType);

    [Get($"{Controller}{nameof(GetCardCurrencyByRequestId)}")]
    Task<CardCurrencyDto?> GetCardCurrencyByRequestId(decimal requestId);

    [Get($"{Controller}{nameof(GetCurrencyRate)}")]
    Task<ValidateCurrencyResponse?> GetCurrencyRate(string currencyIsoCode);

    [Post($"{Controller}{nameof(ValidateSufficientFundForForeignCurrencyCards)}")]
    Task<ApiResponseModel<ValidateCurrencyResponse>> ValidateSufficientFundForForeignCurrencyCards(int cardType, string accountNumber);

    Task<ForeignCurrencyResponse?> GetBuyRate(string currencyIsoCode);
}


