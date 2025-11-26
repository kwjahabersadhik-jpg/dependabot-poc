using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.Migs;
using Refit;

namespace CreditCardsSystem.Domain.Interfaces;

public interface IFraudMonitorAppService : IRefitClient
{
    const string Controller = "/api/migs-frud-monitor/";

    [Post($"{Controller}{nameof(FilterTransactions)}")]
    Task<ApiResponseModel<FilterWrapper>> FilterTransactions(MigsTransactionsFilter model);

    [Get($"{Controller}{nameof(GetMerchantGroups)}")]
    Task<ApiResponseModel<List<ListItem>>> GetMerchantGroups();

    [Post($"{Controller}{nameof(PutSendToFdr)}")]
    Task<ApiResponseModel<FraudMonitorResponse>> PutSendToFdr(TransactionsStatus model);

    [Post($"{Controller}{nameof(PutTransactionsFraudStatus)}")]
    Task<ApiResponseModel<FraudMonitorResponse>> PutTransactionsFraudStatus(TransactionsStatus model);
}