using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.CardStatement;
using CreditCardsSystem.Domain.Models.Reports;
using Refit;

namespace CreditCardsSystem.Domain.Interfaces;

public interface ICardStatementReportAppService : IRefitClient
{
    const string Controller = "/api/CardStatementReport/";


    [Post($"{Controller}{nameof(GetCreditCardStatement)}")]
    Task<ApiResponseModel<TableDataSource>> GetCreditCardStatement(CreditCardStatementReportRequest? cardStatementReportDataSourceDto, CancellationToken cancellationToken = default);

    [Post($"{Controller}{nameof(PrepareReport)}")]
    Task<ApiResponseModel<EFormResponse>> PrepareReport(CreditCardStatementReportRequest? cardStatementReportDataSourceDto, string format = "PDF", CancellationToken cancellationToken = default);

    [Post($"{Controller}{nameof(GetCashBack)}")]
    Task<decimal> GetCashBack(decimal ReqId);


}