using CreditCardsSystem.Data.Models;
using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Interfaces;
using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.Access;
using CreditCardsSystem.Domain.Models.CardIssuance;
using CreditCardsSystem.Domain.Models.Customer;
using CreditCardsSystem.Web.Client.Components;
using CreditCardsSystem.Web.Client.Pages.CustomerProfile;
using Kfh.Aurora.Utilities;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace CreditCardsSystem.Web.Client.Pages.CorporateProfile;

public partial class EditCorporateProfile
{

    [Inject] public required ICorporateAppService CorporateAppService { get; set; }
    [Inject] public required ICorporateProfileAppService CorporateProfileAppService { get; set; }

    [Inject] public required IAccountsAppService AccountsAppService { get; set; }
    [Parameter]
    public EventCallback<bool> ReadyForAction { get; set; }
    [Parameter]
    public EventCallback<ActionStatus> Listen { get; set; }

    [Parameter]
    [SupplyParameterFromQuery(Name = "CivilId")]
    public required string CivilId
    {
        get { return civilId.Decode()!; }
        set { civilId = value; }
    }

    private string civilId;


    [Parameter]
    [SupplyParameterFromQuery]
    public string? ReturnUrl { get; set; }

    [Parameter]
    public bool ShowButtons { get; set; } = true;

    [Parameter]
    public bool IsForCanvas { get; set; } = false;

    [Parameter]
    public GenericCustomerProfileDto? CustomerProfileFromPhenix { get; set; }

    private bool NotValid => editContext!.GetValidationMessages().Any();
    public bool IsValidProfileToCreate { get; set; } = true;
    private new ApplicationState State { get; set; } = new();


    public EditContext? editContext { get; set; }
    public string? Message { get; set; }
    public CorporateProfileDto? Model { get; set; } = new();
    GenericCustomerProfileDto? profile { get; set; }
    private AccountDetailsDto? SelectedCardAccount { get; set; }
    void GoHome() => NavigationManager.NavigateTo("/");
    public DataStatus FormStatus { get; set; } = new();
    protected override async Task OnInitializedAsync()
    {

        var tasks = new List<Task>();
        await Task.Run(() =>
        {
            tasks.Add(GetCustomerProfile());
            tasks.Add(BindCorporateProfileAccount());
        });

        Notification.Hide();

    }

    private async Task OnCardAccountChanged(string accountNumber)
    {
        if (SelectedCardAccount?.Acct == accountNumber)
            return;

        Model!.KfhAccountNo = accountNumber;
        SelectedCardAccount = DebitAccounts.Data?.FirstOrDefault(x => x.Acct == accountNumber);

    }
    async Task GetCustomerProfile()
    {
        try
        {

            //if (AuthManager.HasPermission(Permissions.CorporateProfile.Request()) && AuthManager.HasPermission(Permissions.CorporateProfile.Approve()))
            //{
            //    Notification.Failure(GlobalResources.DualPrivilegesIssue);
            //    return;
            //}

            if (!AuthManager.HasPermission(Permissions.CorporateProfile.Request()))
            {
                await Listen.InvokeAsync(new() { IsAccessDenied = true, Message = GlobalResources.NoPrivilegesonCorporateProfile });
                return;
            }

            FormStatus = DataStatus.Loading;

            if (CustomerProfileFromFDR is not null)
            {
                await BindCustomerProfileFromPhenix();
                return;
            }

            //Loading Primary card profile
            var customerProfile = await CorporateProfileAppService.GetProfileForEdit(CivilId);
            //if (!customerProfile.IsSuccess || customerProfile.Data?.RimNo == 0)
            if (customerProfile is { IsSuccess: false, Data: null })
            {
                Message = customerProfile.Message;
                FormStatus = DataStatus.Error;
                await Listen.InvokeAsync(new() { IsAccessDenied = true, Message = Message });
                return;
            }

            CustomerProfileFromFDR = customerProfile.Data;

            await BindCustomerProfileFromPhenix();


        }
        catch (System.Exception ex)
        {
            State.GenericCustomerProfile.Error(ex);
        }
        finally
        {
            StateHasChanged();
        }
    }
    private DataItem<List<AccountDetailsDto>> DebitAccounts { get; set; } = new();

    [Parameter]
    public CorporateProfileDto? CustomerProfileFromFDR { get; set; }
    async Task BindCorporateProfileAccount()
    {
        DebitAccounts.Loading();
        var debitAccounts = await AccountsAppService.GetCorporateAccounts(CivilId);
        if (debitAccounts.IsSuccessWithData)
            DebitAccounts.SetData(debitAccounts.Data!);
        else
            DebitAccounts.Error(new(debitAccounts.Message));

        await InvokeAsync(StateHasChanged);
    }
    async Task BindCustomerProfileFromPhenix()
    {
        Model = CustomerProfileFromFDR ?? new();
        editContext = new(Model);

        FormStatus = DataStatus.Success;
        await ReadyForAction.InvokeAsync(true);
    }
    async Task HandleValidSubmit()
    {
        if (!editContext.Validate())
            return;


        await SubmitRequest();
    }
    void HandleInvalidSubmit()
    {
        var errors = editContext.GetValidationMessages().ToList();
    }
    public async Task DeleteProfile()
    {
        if (Model is null)
            return;

        var result = await CorporateProfileAppService.DeleteProfileInFdR(Model.CorporateCivilId!);
        Message = result.Message;

        if (result.IsSuccess)
        {
            Notification.Success(result.Message);

        }
        else
            Notification.Failure(result.Message);
    }
    public async Task<bool> SubmitRequest()
    {
        if (Model is null)
            return false;

        if (!(editContext?.Validate() ?? false))
        {
            Notification.Failure("Please check the input fields");

            return false;
        }

        string msg = $"Submitting request for corporate profile {(Model.IsProfileNotFoundInFDR ? "Add" : "Update")}";
        await Listen.NotifyStatus(msg);

        //if (Model.IsProfileNotFoundInFDR)
        //{
        //    Notification.Show(AlertType.Info, msg);
        //}
        //else
        //{
        //    await Listen.NotifyStatus(msg);
        //}


        var result = Model.IsProfileNotFoundInFDR
            ? await CorporateAppService.RequestAddProfile(Model)
            : await CorporateAppService.RequestUpdateProfile(Model);

        Message = result.Message;
        if (!result.IsSuccess)
        {
            Notification.Failure(result.Message);
            return false;
        }

        Notification.Success(Message);

        if (!IsForCanvas)
        {
            NavigationManager.NavigateTo("/");
        }

        return true;
    }
}
