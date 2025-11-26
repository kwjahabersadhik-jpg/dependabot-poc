using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.Migs;
using Refit;

namespace CreditCardsSystem.Domain.Interfaces;

public interface IBlackListCardAppService : IRefitClient
{
    const string Controller = "/api/migs-black-list/";


    [Get($"{Controller}{nameof(GetBlackCards)}")]
    Task<ApiResponseModel<IEnumerable<BlackListCardDto>>> GetBlackCards();

    [Post($"{Controller}{nameof(CreateBlackCards)}/{{cardNo}}/{{isSuspicious:bool}}")]
    Task<ApiResponseModel<BlackListCardResponse>> CreateBlackCards(string cardNo, bool isSuspicious);

    [Post($"{Controller}{nameof(UpdateBlackCards)}/{{cardNo}}/{{isSuspicious:bool}}")]
    Task<ApiResponseModel<BlackListCardResponse>> UpdateBlackCards(string cardNo, bool isSuspicious);

    [Post($"{Controller}{nameof(DeleteBlackCards)}/{{cardNo}}")]
    Task<ApiResponseModel<BlackListCardResponse>> DeleteBlackCards(string cardNo);

    [Post($"{Controller}{nameof(SetSuspiciousCards)}")]
    Task<ApiResponseModel<BlackListCardResponse>> SetSuspiciousCards(SuspiciousCards model);
}