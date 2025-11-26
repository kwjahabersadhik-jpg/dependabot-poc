using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.CardIssuance;
using Refit;

namespace CreditCardsSystem.Domain.Interfaces;

public interface ICardIssuanceAppService : IRefitClient
{
    const string Controller = "/api/CardIssuance/";

    [Post($"{Controller}{nameof(GetEligibleCards)}")]
    Task<ApiResponseModel<EligibleCardResponse>> GetEligibleCards(EligibleCardRequest civilId);

    [Get($"{Controller}{nameof(GetEligibleCardDetail)}")]
    Task<ApiResponseModel<CardDefinitionDto>> GetEligibleCardDetail(int productId, string civilId);

    //[Post($"{Controller}{nameof(IssueNewCard)}")]
    //Task<Models.ApiResponseModel<IssueNewCardResponse>> IssueNewCard(IssueNewCardRequest request);

    [Post($"{Controller}{nameof(IssueAlousraCard)}")]
    Task<ApiResponseModel<CardIssueResponse>> IssueAlousraCard(CardIssueRequest request);

    [Post($"{Controller}{nameof(IssueSupplementaryCards)}")]
    Task<ApiResponseModel<SupplementaryCardIssueResponse>> IssueSupplementaryCards(SupplementaryCardIssueRequest request);

    [Post($"{Controller}{nameof(IssuePrepaidCard)}")]
    Task<ApiResponseModel<CardIssueResponse>> IssuePrepaidCard(PrepaidCardRequest request);

    [Post($"{Controller}{nameof(IssueChargeCard)}")]
    Task<ApiResponseModel<CardIssueResponse>> IssueChargeCard(ChargeCardRequest chargeCardRequest);

    [Post($"{Controller}{nameof(IssueTayseerCard)}")]
    Task<ApiResponseModel<CardIssueResponse>> IssueTayseerCard(TayseerCardRequest tayseerCardRequest);

    [Post($"{Controller}{nameof(IssueCorporateCard)}")]
    Task<ApiResponseModel<CardIssueResponse>> IssueCorporateCard(CorporateCardRequest corporateCardRequest);

    [Get($"{Controller}{nameof(GetAllProducts)}")]
    Task<ApiResponseModel<List<CardEligiblityMatrixDto>>> GetAllProducts(ProductTypes type);
}


