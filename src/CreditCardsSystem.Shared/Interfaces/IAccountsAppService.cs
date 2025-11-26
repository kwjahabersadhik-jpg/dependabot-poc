using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.Account;
using CreditCardsSystem.Domain.Models.CardIssuance;
using CreditCardsSystem.Domain.Shared.Models.Account;
using Refit;

namespace CreditCardsSystem.Domain.Interfaces;

public interface IAccountsAppService : IRefitClient
{
    const string Controller = "/api/Accounts/";


    [Get($"{Controller}{nameof(GetAccountNumberByRequestId)}")]
    Task<ApiResponseModel<string>> GetAccountNumberByRequestId(string requestId);


    [Get($"{Controller}{nameof(GetCorporateAccounts)}")]
    Task<ApiResponseModel<List<AccountDetailsDto>>> GetCorporateAccounts(string civilId);

    [Get($"{Controller}{nameof(GetHoldList)}")]
    Task<ApiResponseModel<HoldDetailsDTO>> GetHoldList(string accountNumber);

    [Get($"{Controller}{nameof(GetDebitAccounts)}")]
    Task<ApiResponseModel<List<AccountDetailsDto>>> GetDebitAccounts(string civilId, string accountNumber = "");

    [Get($"{Controller}{nameof(GetDebitAccountsByAccountNumber)}")]
    Task<ApiResponseModel<List<AccountDetailsDto>>> GetDebitAccountsByAccountNumber(string accountNumber);

    [Get($"{Controller}{nameof(GetDebitAccountsForUSDCard)}")]
    Task<ApiResponseModel<List<AccountDetailsDto>>> GetDebitAccountsForUSDCard(string civilId);

    [Get($"{Controller}{nameof(GetSalaryAccountsForUSDCard)}")]
    Task<ApiResponseModel<List<AccountDetailsDto>>> GetSalaryAccountsForUSDCard(string civilId);

    [Get($"{Controller}{nameof(VerifySalary)}")]
    Task<ApiResponseModel<VerifySalaryResponse>> VerifySalary(string accountNumber, string civilId, int tolerance = ConfigurationBase.Tolerance);

    [Get($"{Controller}{nameof(GetHoldStatus)}")]
    Task<Dictionary<long, string>> GetHoldStatus(int[] holdIds);

    [Get($"{Controller}{nameof(GetFinancialPosition)}")]
    Task<ApiResponseModel<FinancialPositionResponse>> GetFinancialPosition(string civilId, Collateral collateral, string accountNumber);

    [Get($"{Controller}{nameof(GetDepositAccounts)}")]
    Task<ApiResponseModel<List<AccountDetailsDto>>> GetDepositAccounts(string civilId, string accountNumber = "");

    [Get($"{Controller}{nameof(GetMarginAccounts)}")]
    Task<ApiResponseModel<List<AccountDetailsDto>>> GetMarginAccounts(string civilId, string accountNumber = "");

    [Get($"{Controller}{nameof(GetDepositAccountsForUSDCard)}")]
    Task<ApiResponseModel<List<AccountDetailsDto>>> GetDepositAccountsForUSDCard(string civilId);

    Task<List<AccountDetailsDto>?> GetAllAccounts(string civilId, bool onlyActive = true);
    Task<ApiResponseModel<string>> CreateCustomerAccount(CreateCustomerAccountRequest request);


}
