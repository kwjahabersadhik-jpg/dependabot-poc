using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.DirectDebit;
using Refit;

namespace CreditCardsSystem.Domain.Interfaces;

public interface IDirectDebitAppService : IRefitClient
{
    const string Controller = "/api/DirectDebit/";

    [Get($"{Controller}{nameof(Get)}")]
    Task<ApiResponseModel<DirectDebitOptionDto>> Get();

    [Post($"{Controller}{nameof(Create)}")]
    Task<ApiResponseModel<string>> Create([Body] DirectDebitOptionDto request);

}
