using CreditCardsSystem.Domain.Models;
using Refit;

namespace CreditCardsSystem.Domain.Interfaces;

public interface ILoadFileAppService : IRefitClient
{
    const string Controller = "/api/migs/";

    [Get($"{Controller}{nameof(LoadIds)}")]
    Task<ApiResponseModel<IEnumerable<int>>> LoadIds(DateTime? loadDate);
}