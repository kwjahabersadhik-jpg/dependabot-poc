using CreditCardsSystem.Domain.Models.BCDPromotions.Requests;
using CreditCardsSystem.Domain.Shared.Interfaces;
using Kfh.Aurora.Auth;
using Microsoft.AspNetCore.Components;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace CreditCardsSystem.Web.Client.Components;

public class PromotionsRequestsHelpers : ComponentBase
{
    [Inject] public IRequestsOpsAppService RequestsOpsAppService { get; set; } = default!;
    [Inject] public IAuthManager AuthManager { get; set; } = default!;

    public string GetDisplayName<T>(string propertyName)
    {
        var type = typeof(T);
        var prop = type.GetProperty(propertyName);
        var displayAttribute = prop?.GetCustomAttribute<DisplayAttribute>();
        return displayAttribute?.Name ?? string.Empty;
    }

    public string GetParamValue(List<RequestActivityDetailsDto> reqDetails, string parameter, bool getOld = false)
    {
        if (getOld)
            parameter = "Old_" + parameter;

        var param = reqDetails.FirstOrDefault(r => r.Parameter == parameter);
        return param != null ? !string.IsNullOrEmpty(param.Value) ? param.Value : "N/A" : "N/A";
    }

}