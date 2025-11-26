using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.Access;
using CreditCardsSystem.Utility.Crypto;
using CreditCardsSystem.Utility.Extensions;
using CreditCardsSystem.Web.Client.Pages.CustomerProfile;
using Kfh.Aurora.Auth;
using Kfh.Aurora.Blazor.Utilities;
using Kfh.Aurora.Common.Components.UI.Search.RecentSearch;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.WebUtilities;

namespace CreditCardsSystem.Web.Client.Components;

public class ApplicationComponent : ComponentBase
{

    [Inject]
    public AppState CurrentState { get; set; } = default!;

    [Inject]
    private ILogger<ApplicationComponent> Logger { get; set; } = default!;

    [Inject]
    public IAuthManager AuthManager { get; set; } = default!;

    [Inject]
    public ClipboardExtensions Clipboard { get; set; } = default!;

    [CascadingParameter]
    public Notification Notification { get; set; } = default!;

    [CascadingParameter]
    public ApplicationState State { get; set; } = default!;

    [CascadingParameter]
    public RecentSearch AdvancedSearchRef { get; set; } = default!;

    [Inject]
    public NavigationManager NavigationManager { get; set; } = default!;

    //[Parameter]
    //public string Permission { get; set; } = null!;

    public bool IsProcessing { get => Notification?.IsProcessing ?? false; }
    protected async Task CopyToClipboard(string text)
    {
        await Clipboard.CopyAsync(text);
        Notification.Success("Copied!");
    }
    public bool CanViewCardNumber => IsAllowTo(Permissions.CreditCardsNumber.View());

    public string? DisplayCardNumber(string? cardNumber)
    {
        if (string.IsNullOrEmpty(cardNumber?.Trim()))
            return null;

        cardNumber = cardNumber?.Length > 16 ? cardNumber.Replace("-", "").DeSaltThis() : cardNumber;

        if (cardNumber!.Contains('x', StringComparison.InvariantCultureIgnoreCase))
            return cardNumber;

        return (CanViewCardNumber ? cardNumber : cardNumber.Masked(6, 6)).SplitByFour();
    }

    public bool IsAllowTo(string permission) => AuthManager.HasPermission(permission);

    public void NavigateTo(string url, Dictionary<string, string>? queryParams = null, bool forceLoad = false, bool replace = false)
    {
        if (queryParams is null)
            NavigationManager.NavigateTo(url, forceLoad: forceLoad, replace: replace);
        else
            NavigationManager.NavigateTo(QueryHelpers.AddQueryString(url, queryParams), forceLoad: forceLoad, replace: replace);
    }


    public async Task HandleApiResponse<T>(ApiResponseModel<T> response, EditContext? editContext = null, ValidationMessageStore? messageStore = null, bool showToast = true) where T : class, new()
    {
        Notification.Hide();

        if (showToast)
        {
            if (response.IsSuccess)
                Notification.Success(response.Message);
            else
                Notification?.Failure(response.Message);
        }

        if (editContext == null)
        {
            await Task.CompletedTask;
            return;
        }

        messageStore?.Clear();

        if (response.ValidationErrors != null)
            foreach (var error in response.ValidationErrors.Where(x => x.Property != null))
            {
                messageStore?.Add(editContext!.Field(error.Property!), error.Error);
            }

        editContext!.NotifyValidationStateChanged();
    }

    public async Task HandleApiResponse(ApiResponseModel response, EditContext? editContext = null, ValidationMessageStore? messageStore = null, bool showToast = true)
    {
        Notification.Hide();

        if (showToast)
        {
            if (response.IsSuccess)
                Notification.Success(response.Message);
            else
                Notification?.Failure(response.Message);
        }

        if (editContext == null)
        {
            await Task.CompletedTask;
            return;
        }

        messageStore?.Clear();

        if (response.ValidationErrors != null)
            foreach (var error in response.ValidationErrors.Where(x => x.Property != null))
            {
                messageStore?.Add(editContext!.Field(error.Property!), error.Error);
            }

        editContext!.NotifyValidationStateChanged();
    }


    public void ConfigEditContext<T>(EditContext editContext, T model, ValidationMessageStore messageStore)
    {
        editContext = new EditContext(model);
        messageStore = new(editContext);
        editContext.OnValidationRequested += (s, e) => messageStore?.Clear();
        editContext.OnFieldChanged += (s, e) => messageStore?.Clear(e.FieldIdentifier);
    }


    public string GetCreditCardImage(int? type) => $"/api/creditCardsImages/{type}?DummyId={DateTime.Now.Ticks}";
    public const string DefaultImage = "/dist/KFHCreditCards/credit/DefaultImage-light.png";
}


public static class ApiResponseNotification
{
    public async static Task UnAuthorized(this EventCallback<ActionStatus> listen)
    {
        await listen.InvokeAsync(new() { IsAccessDenied = false });
    }

    public async static Task NotifyStatus(this EventCallback<ActionStatus> listen, DataStatus dataStatus = DataStatus.Loading, string Title = "", string Message = "")
    {
        await listen.InvokeAsync(new(false, dataStatus, Title: Title, Message: Message));
    }

    public async static Task NotifyStatus(this EventCallback<ActionStatus> listen, string Message = "")
    {
        await listen.InvokeAsync(new(false, DataStatus.Loading, Message: Message));
    }

    public async static Task NotifyStatus<T>(this EventCallback<ActionStatus> listen, ApiResponseModel<T>? data = null) where T : class, new()
    {
        DataStatus ProcessStatus = data is null ? DataStatus.Loading : (data.IsSuccess ? DataStatus.Success : DataStatus.Error);

        string valiationError = data?.ValidationErrors != null ? string.Join("\n", data?.ValidationErrors!.Select(x => x.Error)) : "";
        await listen.InvokeAsync(new(data?.IsSuccess ?? false, ProcessStatus, Message: $"{data?.Message}{valiationError}"));
    }


    public static async Task HandleApiResponse<T>(this EventCallback<ActionStatus> listen, ApiResponseModel<T> response, EditContext? editContext = null, ValidationMessageStore? messageStore = null) where T : class, new()
    {
        DataStatus ProcessStatus = response is null ? DataStatus.Loading : (response.IsSuccess ? DataStatus.Success : DataStatus.Error);

        await listen.InvokeAsync(new(ProcessStatus: ProcessStatus, IsSuccess: response.IsSuccess, Message: response.Message));

        if (editContext == null)
        {
            await Task.CompletedTask;
            return;
        }

        messageStore?.Clear();

        if (response.ValidationErrors != null)
            foreach (var error in response.ValidationErrors.Where(x => x.Property != null))
            {
                messageStore?.Add(editContext!.Field(error.Property!), error.Error);
            }

        editContext!.NotifyValidationStateChanged();
    }

}


