using CreditCardsSystem.Domain.Enums;
using Kfh.Aurora.Notify;
using orgNamespace = Kfh.Aurora.Organization;

namespace CreditCardsSystem.Domain.Models.UserSettings;

public class UserSettings
{
    #region Preference Dictionary Keys (Key to get the Value from Preferences API)

    public static string LanguageKey => "user_language";
    public static string ThemeModeKey => "user_theme_mode";
    public static string DefaultBranchKey => "user_default_branch";
    public static string LastLoggedInIPKey => "user_last_logged_in_ip";

    #endregion

    #region Properties

    public string LanguageValue { get; set; } = nameof(Language.English);
    public string ThemeModeValue { get; set; } = nameof(ThemeMode.Light);

    public string LastLoggedInIP { get; set; } = string.Empty;
    public string? DefaultBranchIdValue { get; set; }
    public bool DidIpChangeFromLastLogin { get; set; } = true;

    public List<orgNamespace.Branch> UserBranches { get; set; } = new();

    #endregion

    #region Checks

    public bool HasDefaultBranch() => !string.IsNullOrEmpty(DefaultBranchIdValue);

    public bool HasLastLoggedInIP()
    {
        if (string.IsNullOrEmpty(LastLoggedInIP))
        {
            return false;
        }

        return true;
    }

    public bool ShouldPromptBranchSelection()
    {
        if ((!HasDefaultBranch() && UserBranches.Count > 1 && !HasLastLoggedInIP() && DidIpChangeFromLastLogin))
        {
            return true;
        }

        return false;
    }

    #endregion

    #region Setters

    public void SetLanguage(Language language)
    {
        LanguageValue = language == Language.Arabic ? nameof(Language.Arabic) : nameof(Language.English);
    }

    public void SetThemeMode(ThemeMode themeMode)
    {
        ThemeModeValue = themeMode == ThemeMode.Dark ? nameof(ThemeMode.Dark) : nameof(ThemeMode.Light);
    }

    #endregion

    #region Getters

    public List<KeyValuePair<string, string>> BranchesKeyValues()
    {
        return UserBranches.Select(e => new KeyValuePair<string, string>(e.BranchId.ToString(), e.Name)).ToList();
    }

    public Language GetLanguage()
    {
        try
        {
            if (LanguageValue.ToLower() == "arabic")
            {
                return Language.Arabic;
            }
            return Language.English;
        }
        catch (Exception)
        {
            return Language.English;
        }
    }

    public ThemeMode GetThemeMode()
    {
        try
        {
            var themeMode = System.Enum.Parse<ThemeMode>(ThemeModeValue, true);
            return themeMode;
        }
        catch (Exception)
        {
            return ThemeMode.Light;
        }
    }

    #endregion

    #region Toggles

    public void ToggleLanguage()
    {
        if (GetLanguage() == Language.Arabic)
        {
            SetLanguage(Language.English);
        }
        else
        {
            SetLanguage(Language.Arabic);
        }
    }

    public void ToggleThemeMode()
    {
        if (GetThemeMode() == ThemeMode.Dark)
        {
            SetThemeMode(ThemeMode.Light);
        }
        else
        {
            SetThemeMode(ThemeMode.Dark);
        }
    }

    #endregion

    #region Constructors

    public UserSettings()
    {
    }

    public UserSettings(string languageValue, string themeModeValue, string defaultBranchIdValue, string lastLoggedInIP)
    {
        LanguageValue = languageValue;
        ThemeModeValue = themeModeValue;
        DefaultBranchIdValue = defaultBranchIdValue;
        LastLoggedInIP = lastLoggedInIP;
    }

    #endregion

    #region Factory Methods

    public static UserSettings DefaultSettings() => new UserSettings();

    #endregion


    #region Generators

    public List<orgNamespace.UserPreferences> Preferences()
    {
        var preferencesDictionary = new List<orgNamespace.UserPreferences>()
        {
            new()
            {
                Key = LanguageKey,
                Value = nameof(Language)
            },
            new()
            {
                Key = ThemeModeKey,
                Value = nameof(ThemeModeKey)
            }
        };
        if (HasDefaultBranch())
        {
            preferencesDictionary.Add(new()
            {
                Key = DefaultBranchKey,
                Value = DefaultBranchIdValue!
            });
        }

        return preferencesDictionary;
    }

    #endregion
}