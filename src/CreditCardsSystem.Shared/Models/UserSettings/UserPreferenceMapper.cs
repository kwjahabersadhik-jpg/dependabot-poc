using orgNameSpace = Kfh.Aurora.Organization;

namespace CreditCardsSystem.Domain.Models.UserSettings;

public static class UserPreferencesMapperExtension
{
    public static UserSettings FromUserPreferences(this List<orgNameSpace.UserPreferences> preferences)
    {
        string language = preferences.FirstOrDefault(e => e.Key == Models.UserSettings.UserSettings.LanguageKey)?.Value ?? "";
        string themeMode = preferences.FirstOrDefault(e => e.Key == Models.UserSettings.UserSettings.ThemeModeKey)?.Value ?? "";
        string defaultBranch = preferences.FirstOrDefault(e => e.Key == Models.UserSettings.UserSettings.DefaultBranchKey)?.Value ?? "";
        string lastLoggedInIP = preferences.FirstOrDefault(e => e.Key == Models.UserSettings.UserSettings.LastLoggedInIPKey)?.Value ?? "";
        return new UserSettings(language, themeMode, defaultBranch, lastLoggedInIP);
    }
}