using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.CardPayment;
using CreditCardsSystem.Domain.Models.Reports;
using Refit;

namespace CreditCardsSystem.Domain.Interfaces;

public interface IReportAppService : IRefitClient
{
    const string Controller = "/api/Report/";

    [Get($"{Controller}{nameof(GenerateDeclartationForm)}")]
    Task<ApiResponseModel<EFormResponse>> GenerateDeclartationForm(decimal RequestId);

    [Post($"{Controller}{nameof(GenerateDebitVoucher)}")]
    Task<ApiResponseModel<EFormResponse>> GenerateDebitVoucher(DebitVoucher voucherData);

    [Post($"{Controller}{nameof(GenerateDepositVoucher)}")]
    Task<ApiResponseModel<EFormResponse>> GenerateDepositVoucher(DepositVoucher voucherData);


    [Get($"{Controller}{nameof(GenerateCardIssuanceEForm)}")]
    Task<ApiResponseModel<EFormResponse>> GenerateCardIssuanceEForm(decimal RequestId);

    [Get($"{Controller}{nameof(GenerateAfterSalesForm)}")]
    Task<ApiResponseModel<EFormResponse>> GenerateAfterSalesForm(AfterSalesForm afterSalesForm);

    Task<ApiResponseModel<EFormResponse>> PrintDynamicReport<T>(T reportData, FileExtension fileExtension = FileExtension.pdf) where T : class;


    [Post($"{Controller}{nameof(GenerateCardPaymentVoucher)}")]
    Task<ApiResponseModel<EFormResponse>> GenerateCardPaymentVoucher(PaymentVoucher voucherData);
}