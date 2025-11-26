using CreditCardsSystem.Data.Models;
using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.Access;
using CreditCardsSystem.Web.Client.Components;
using CreditCardsSystem.Web.Client.Pages.CorporateProfile.Components;
using Kfh.Aurora.Blazor.Components.UI;
using Kfh.Aurora.Blazor.Components.ViewModels.ListTiles;
using Kfh.Aurora.Common.Components.UI.Customer;
using Kfh.Aurora.Utilities;
using Microsoft.AspNetCore.Components;

namespace CreditCardsSystem.Web.Client.Pages.CorporateProfile;

public partial class CorporateView
{
    private CorporateProfileDto? corporateProfileDto { get; set; }

    [Parameter]
    [SupplyParameterFromQuery(Name = "CivilId")]
    public required string CivilId
    {
        get { return civilId.Decode()!; }
        set { civilId = value; }
    }

    private string civilId;

    [Parameter]
    [SupplyParameterFromQuery(Name = "state")]
    public string StateVar { get; set; } = default!;

    public CorporateCreditCardList CreditCardLisRef { get; set; } = default!;
    public EditCorporateProfile editProfileRef { get; set; } = default!;

    public OffCanvas ProfileDetailsDrawerRef { get; set; } = default!;
    public OffCanvas editProfileCanvas { get; set; } = default!;
    public bool ConfirmDialogVisible { get; set; }
    public DataStatus ProcessStatus { get; set; }
    private string Action => corporateProfileDto is { IsProfileNotFoundInFDR: true } ? " Add" : "Edit";
    private string EditAction => corporateProfileDto is { IsProfileNotFoundInFDR: true } ? " Submit" : "Update";
    private bool IsAuthorizedToEdit => AuthManager.HasPermission(Permissions.CorporateProfile.Edit());
    // private UserListTileViewModel UserInfo = new();

    //Todo Check if user has access to view customer profile
    private bool HasPermissionToViewCustomerProfile { get; set; } = true;

    //Todo Check if user has access to view staff salary
    private bool HasPermissionToViewStaffSalary { get; set; } = false;
    private AuroraCustomerProfileOffCanvas CustomerProfileOffCanvasRef { get; set; } = default!;


    protected override async Task OnInitializedAsync()
    {

    }
    protected override async Task OnParametersSetAsync()
    {
        if (CivilId is not null)
        {
            await BindCustomerProfile();
        }
    }

    private UserListTileViewModel UserInfo = new();

    bool isReadyForAction { get; set; }
    ActionStatus actionStatus { get; set; } = new();
    async Task ReadyForAction(bool isSuccess = false)
    {
        isReadyForAction = isSuccess;
        await Task.CompletedTask;
    }


    private async Task OpenProfileCanvas(bool isOpen)
    {
        if (CustomerProfileOffCanvasRef != null)
        {
            if (isOpen)
            {
                await CustomerProfileOffCanvasRef.ExpandAsync();
            }
            else
            {
                await CustomerProfileOffCanvasRef.CollapseAsync();
            }

        }
    }
    async Task Listen(ActionStatus actionStatus)
    {
        this.actionStatus = actionStatus;

        if (actionStatus.IsAccessDenied)
        {
            Notification.Failure(string.IsNullOrEmpty(actionStatus.Message) ? "You do not have permission!" : actionStatus.Message);
            await ReadyForAction(false);
            return;
        }

        if (actionStatus.ProcessStatus is DataStatus.Loading)
        {
            Notification.Loading(actionStatus.Message);
            return;
        }

        if (actionStatus.ShowConfirmation)
        {
            ConfirmDialogVisible = true;
            StateHasChanged();
            return;
        }

        if (actionStatus.ProcessStatus != DataStatus.Loading)
            Notification.Show(actionStatus.IsSuccess ? AlertType.Success : AlertType.Error, actionStatus.Message);

        if (actionStatus.CloseDialog)
            StateHasChanged();

        if (actionStatus.IsSuccess)
            await CloseRequestForm();
    }
    async Task CloseRequestForm()
    {
        if (!editProfileCanvas.IsOpen)
            return;

        await editProfileCanvas.ToggleAsync();
        this.StateHasChanged();
    }

    private async Task OnCopyIconClickHandler(string civilId)
    {
        await Clipboard.CopyAsync(civilId);
        Notification.Success("Copied!");
    }

    private async Task BindCustomerProfile()
    {
        State ??= new();

        State.GenericCorporateProfile.Loading();
        var corporateProfile = await CorporateProfileAppService.GetProfileForEdit(CivilId);


        if (!corporateProfile.IsSuccess)
        {
            Notification.Failure(corporateProfile.Message);
            State.GenericCorporateProfile.Error(new(corporateProfile.Message));
            return;
        }

        corporateProfileDto = corporateProfile.Data;
        if (corporateProfileDto!.IsProfileNotFoundInFDR)
        {
            var queryParams = new Dictionary<string, string>
            {
                ["civilId"] = CivilId.Encode()!
            };

            Notification.Info("Corporate profile not found in FDR. redirecting to edit page");
            await Task.Delay(1000);
            NavigateTo("edit-corporate-profile", queryParams);
            return;
        }


        var customerProfile = await GenericCustomerProfileAppService.GetCustomerProfileMinimal(new() { CivilId = CivilId });
        if (!customerProfile.IsSuccess)
        {
            State.GenericCorporateProfile.Error(new(customerProfile.Message));
            return;
        }


        State.GenericCorporateProfile.SetData(customerProfile.Data ?? new());
        var profile = State.GenericCorporateProfile.Data;

        if (profile is not null)
        {
            UserInfo = new()
            {
                ImageUrl = profile.ImageUrl,// "dist/segments/AlNukhbah.png",
                Sex = profile.Gender == "M" ? "Y" : "N",//"Y",
                SpecialNeedStr = profile.SpecialNeed ?? "N",
                ArabicName = profile.ArabicName ?? "",
                EnglishName = profile.EnglishName ?? "",// "KFH User",
                CivilId = profile.CivilId,// "123456789012",
                CountryCode = profile.CustomerAddresses?[0]?.CountryCode ?? "",//"KWT",
                NationalityEnglish = profile.NationalityEnglish ?? "",
                IsEmployee = profile.IsEmployee,// true,
                IsBlacklist = profile.Blacklist ?? false,// true,
                IsPep = profile.PEP,// true,
                IsVip = profile.VIP
            };

            CurrentState.CustomerProfile ??= new();
            CurrentState.CustomerProfile.RimCode = profile.RimCode.ToString();
            CurrentState.CustomerProfile.CustomerType = profile.CustomerType ?? "";
            CurrentState.CustomerProfile.DateOfBirth = profile.BirthDate;
            CurrentState.CustomerProfile.FirstName = profile.FirstName ?? "";
            CurrentState.CustomerProfile.LastName = profile.LastName ?? "";
        }

        StateHasChanged();

        CurrentState.CurrentCivilId = CivilId;

    }
    private async Task SubmitRequest()
    {
        ConfirmDialogVisible = false;
        if (await editProfileRef.SubmitRequest())
        {
            await editProfileCanvas.ToggleAsync();
        }

    }



}
