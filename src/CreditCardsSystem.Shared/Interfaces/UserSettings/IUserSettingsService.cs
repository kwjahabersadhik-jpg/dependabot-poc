using Kfh.Aurora.Organization;
using Refit;

namespace CreditCardsSystem.Domain.Interfaces.UserSettings;

public interface IUserSettingsAppService : IRefitClient
{
    const string Controller = "/api/UserSettings/";

    [Post($"{Controller}{nameof(AddOrUpdatePreferences)}")]
    Task AddOrUpdatePreferences([Body] List<UserPreferences> preferences);

    [Delete($"{Controller}{nameof(RemovePreference)}" + "/{key}")]
    Task RemovePreference(string key);

    [Delete($"{Controller}{nameof(ClearPreference)}")]
    Task ClearPreference();

    [Get($"{Controller}{nameof(Preferences)}")]
    Task<Models.UserSettings.UserSettings> Preferences();

    [Get($"{Controller}{nameof(GetUserBranches)}")]
    Task<List<Branch>> GetUserBranches();
}