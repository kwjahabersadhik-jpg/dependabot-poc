using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.CardOperation;
using Refit;

namespace CreditCardsSystem.Domain.Interfaces;

public interface IChangeLimitAppService : IRefitClient
{
    const string Controller = "/api/ChangeLimit/";

    [Post($"{Controller}{nameof(InsertTayseerCreditCheckingRecord)}")]
    Task InsertTayseerCreditCheckingRecord(TayseerCreditCheckingDto tayseerObj);

    [Post($"{Controller}{nameof(CheckCBKViolationStatus)}")]
    Task<ApiResponseModel<CreditCheckCalculationResponse>> CheckCBKViolationStatus(CreditCheckCalculationRequest request);

    [Post($"{Controller}{nameof(CalculateCreditChecking)}")]
    Task<ApiResponseModel<CreditCheckCalculationResponse>> CalculateCreditChecking(CreditCheckCalculationRequest request);

    [Get($"{Controller}{nameof(GetCreditCardCheckingPreviousLog)}")]
    Task<TayseerCreditCheckingData> GetCreditCardCheckingPreviousLog(decimal requestId);

    [Get($"{Controller}{nameof(GetPreviousInstallments)}")]
    Task<PreviousInstallments> GetPreviousInstallments(string civilId);

    [Post($"{Controller}{nameof(RequestChangeLimit)}")]
    Task<ApiResponseModel<ProcessResponse>> RequestChangeLimit(ChangeLimitRequest request);

    [Get($"{Controller}{nameof(DeleteChangeLimit)}")]
    Task<ApiResponseModel<ProcessResponse>> DeleteChangeLimit(decimal id);

    [Post($"{Controller}{nameof(CancelChangeLimit)}")]
    Task<ApiResponseModel<ProcessResponse>> CancelChangeLimit(decimal id, ChangeLimitStatus status);


    [Get($"{Controller}{nameof(GetChangeLimitHistory)}")]
    Task<ApiResponseModel<List<ChangeLimitHistoryDto>>> GetChangeLimitHistory(decimal requestId);

    [Post($"{Controller}{nameof(ProcessChangeLimitRequest)}")]
    Task<ApiResponseModel<ProcessResponse>> ProcessChangeLimitRequest(ProcessChangeLimitRequest request);


}
