using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.CardIssuance;
using CreditCardsSystem.Domain.Models.Employee;
using CreditCardsSystem.Domain.Shared.Models.Account;
using Refit;

namespace CreditCardsSystem.Domain.Interfaces;

public interface IEmployeeAppService : IRefitClient
{
    const string Controller = "/api/Employee/";

    [Get($"{Controller}{nameof(GetCurrentLoggedInUser)}")]
    Task<UserDto?> GetCurrentLoggedInUser(decimal? kfhId = null);

    [Get($"{Controller}{nameof(ValidateSellerId)}")]
    Task<ApiResponseModel<ValidateSellerIdResponse>> ValidateSellerId(string sellerId);

    [Get($"{Controller}{nameof(GetEmployeeNumberByAccountNumber)}")]
    Task<ApiResponseModel<EmployeeInfo>> GetEmployeeNumberByAccountNumber(string accountNumber);
}
