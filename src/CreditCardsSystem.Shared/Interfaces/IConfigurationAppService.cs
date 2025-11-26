using Kfh.Aurora.Organization;
using Refit;

namespace CreditCardsSystem.Domain.Interfaces;
public interface IConfigurationAppService : IRefitClient
{
    const string Controller = "/api/Configuration/";

    [Get($"{Controller}{nameof(GetValue)}")]
    Task<string> GetValue(string configName);

    [Get($"{Controller}{nameof(GetUserBranch)}")]
    Task<Branch> GetUserBranch(decimal? kfhId = null);
}
