using CreditCardsSystem.Data;
using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Models.UserSettings;
using CreditCardsSystem.Utility.Extensions;
using Kfh.Aurora.Auth;
using Kfh.Aurora.Organization;
using Microsoft.EntityFrameworkCore;

namespace CreditCardsSystem.Application.Configurations;

public class ConfigurationAppService : IAppService, IConfigurationAppService
{
    private readonly FdrDBContext fdrDBContext;
    private readonly IUserPreferencesClient _userPreferencesClient;
    private readonly IAuthManager authManager;
    private readonly IOrganizationClient _organizationClient;
    public ConfigurationAppService(FdrDBContext fdrDBContext, IUserPreferencesClient userPreferencesClient,
        IAuthManager authManager, IOrganizationClient organizationClient)
    {
        this.fdrDBContext = fdrDBContext;
        _organizationClient = organizationClient;
        _userPreferencesClient = userPreferencesClient;
        this.authManager = authManager;
    }
    [HttpGet]
    public async Task<string> GetValue(string configName)
    {
        return (await fdrDBContext.ConfigParameters.AsNoTracking().FirstOrDefaultAsync(x => x.ParamName == configName))?.ParamValue ?? "";
    }

    [HttpGet]
    public async Task<Branch> GetUserBranch(decimal? kfhId = null)
    {
        bool isExternalUser = kfhId != null;


        if (kfhId is null or 0)
            kfhId = Convert.ToDecimal(authManager.GetUser()?.KfhId);

        var userPreference = await _userPreferencesClient.GetUserPreferences(kfhId.ToString()!);
        int? defaultBranchId = null;
        Branch? deefaultBranch = null;

        if (userPreference.Any())
        {
            if (int.TryParse(userPreference?.FromUserPreferences().DefaultBranchIdValue, out int _defaultBranchId))
            {
                defaultBranchId = _defaultBranchId;
            }
        }

        var defaultBranches = userPreference?.FromUserPreferences().UserBranches;

        if (!defaultBranches.AnyWithNull())
            defaultBranches = await _organizationClient.GetUserBranches(kfhId.ToString()!);


        if (defaultBranchId is null)
        {
            deefaultBranch = defaultBranches?.FirstOrDefault();
        }
        else
        {
            deefaultBranch = defaultBranches?.FirstOrDefault(x => x.BranchId == defaultBranchId);
        }

        return deefaultBranch is null ? throw new ApiException(message: "Invalid user branch") : deefaultBranch;
    }
}
