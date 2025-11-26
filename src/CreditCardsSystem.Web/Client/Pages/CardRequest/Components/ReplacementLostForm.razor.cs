using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Interfaces;
using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.Access;
using CreditCardsSystem.Domain.Models.CardIssuance;
using CreditCardsSystem.Domain.Models.CardOperation;
using CreditCardsSystem.Domain.Models.CoBrand;
using CreditCardsSystem.Utility.Extensions;
using CreditCardsSystem.Web.Client.Components;
using Kfh.Aurora.Organization;
using Microsoft.AspNetCore.Components;
using System.Text.RegularExpressions;

namespace CreditCardsSystem.Web.Client.Pages.CardRequest.Components;

public partial class ReplacementLostForm : IWorkflowMethods
{
    private bool IsAuthorized => IsAllowTo(TaskDetail is not null ? Permissions.ReplaceOnLostStolen.EnigmaApprove() : Permissions.ReplaceOnLostStolen.Request());

    [Inject] IReplacementAppService ReplacementAppService { get; set; } = null!;
    [Inject] ILookupAppService LookupAppService { get; set; } = null!;
    [Inject] IMemberShipAppService _memberShipService { get; set; } = null!;


    [Parameter]
    public CardReplacementRequest Model { get; set; } = new();

    [Parameter]
    public List<AccountDetailsDto>? DebitAccounts { get; set; }

    private record DeliveryOptionItem(string name, DeliveryOption value);
    private List<DeliveryOptionItem> DeliveryOptions { get; set; } =
[
    new(DeliveryOption.BRANCH.GetDescription(), DeliveryOption.BRANCH),
    new(DeliveryOption.COURIER.GetDescription(), DeliveryOption.COURIER)
];

    private record ReplaceForItem(string name, ReplaceOn value);
    private List<ReplaceForItem> ReplaceForOptions { get; set; } = new List<ReplaceForItem>()
{
    new(ReplaceOn.Damage.GetDescription(), ReplaceOn.Damage),
    new(ReplaceOn.LostOrStolen.GetDescription(), ReplaceOn.LostOrStolen)
};



    private List<Branch>? Branches { get; set; }
    List<string> replacementReasons = new() {
    "Card replacement due to a technical error" ,
    "Card replacement due to employee error" ,
    "Card replacement due to card fraud",
    "Card replacement due to Not Received"
    };



    async Task PrepareRequestForm()
    {
        if (!IsAuthorized)
        {
            await Listen.InvokeAsync(new() { IsAccessDenied = true });
            return;
        }

        BindFormEditContext(Model);

        Model = new()
        {
            CardNumber = SelectedCard.CardNumberDto!,
            ProductName = SelectedCard.ProductName,
            RequestId = SelectedCard.RequestId,
            ReplaceOn = ReplaceOn.LostOrStolen
        };

        if (long.TryParse(SelectedCard.MemberShipId, out long _memberShipId))
        {
            Model.OldMembershipId = _memberShipId;
            await BindCoBrandCompanyNames();
        }

        Branches = (await LookupAppService.GetAllBranches())?.Data?.Select(x => new Branch() { BranchId = x.BranchId, Name = Regex.Replace(x.Name, "@\"[^A-Za-z]\"", " ").Trim() }).ToList();
        BindFormEditContext(Model);
        await ReadyForAction.InvokeAsync(true);

    }

    public async Task<bool> SubmitRequest(CancellationToken? cancellationToken = default)
    {

        if (!await IsFormValid()) return false;


        await Listen.NotifyStatus(DataStatus.Processing, Title: "Card replacement", Message: $"Requesting card replacement for lost card");

        var response = await ReplacementAppService.RequestCardReplacement(Model);
        await NotifyStatus(response.IsSuccess, response.Message);
        return response.IsSuccess;
    }




    async Task BindTaskDetail()
    {
        if (!IsAuthorized)
        {
            await Listen.InvokeAsync(new() { IsAccessDenied = true });
            return;
        }

    }

    public async Task ProcessAction(ActionType actionType, string ReasonForRejection)
    {
        //await Listen.NotifyStatus($"{actionType.GetDescription()} in progress..");

        if (TaskDetail is not null)
        {
            var result = await ReplacementAppService.ProcessCardReplacementRequest(new()
            {
                ReasonForRejection = ReasonForRejection,
                ActionType = actionType,
                RequestActivityId = RequestActivity!.RequestActivityId,
                TaskId = TaskDetail?.Id,
                WorkFlowInstanceId = TaskDetail?.InstanceId
            });
            await Listen.NotifyStatus(data: result); ;
        }
    }







    //string? PrimaryCardAccountNumber { get; set; }
    protected override async Task OnInitializedAsync()
    {

        //Notification.Loading($"Loading data...");

        if (TaskDetail is not null)
            await BindTaskDetail();
        else
            await PrepareRequestForm();


        Notification.Hide();
    }

    //List<AccountDetailsDto>? debitAccounts;
    //private Collateral collateral;
    public Task PrintApplication()
    {
        throw new NotImplementedException();
    }
    async Task NotifyStatus(bool isSuccess = false, string message = "", bool isStarted = false)
    {
        if (isStarted)
        {
            await Listen.InvokeAsync(new(ProcessStatus: DataStatus.Loading, CloseDialog: false));
            return;
        }

        ActionStatus actionStatus = new(IsSuccess: isSuccess, Message: message, ProcessStatus: isSuccess ? DataStatus.Success : DataStatus.Error);
        await Listen.InvokeAsync(actionStatus);
    }

    public Task Cancel()
    {
        throw new NotImplementedException();
    }



    //Need component
    #region MemberShip
    public DataItem<CompanyLookup> coBrandCardCompany = new();
    async Task BindCoBrandCompanyNames()
    {
        //if (!CardDefinition?.Eligibility?.IsCoBrandPrepaid ?? false) return;

        coBrandCardCompany.Loading();

        var companyResponse = await LookupAppService.GetAllCompanies();
        if (companyResponse.IsSuccess)
        {
            var coBrandCompany = companyResponse.Data!.Where(x => x.CardType == SelectedCard?.CardType)
                .Select(x => new CompanyLookup(x.CompanyId, x.CompanyName, x.CardType, x.ClubName))
                .DistinctBy(x => x.CompanyId).FirstOrDefault();

            coBrandCardCompany.SetData(coBrandCompany!);
        }
        else
            coBrandCardCompany.Error(new(companyResponse.Message));

        StateHasChanged();
    }

    private CoBrandRequest CoBrand { get; set; } = new();
    private bool IsAllowToDeleteMemberShip = false;
    async Task OnChangeMemberShipId()
    {
        if (Model.OldMembershipId is null) return;

        var customMemberShipId = new List<MemberShipInfoDto>() { new() { ClubMembershipId = Model.NewMemberShipId!.Value.ToString() } };
        CoBrand.IsValidMemberShipIdToIssueCard = await IsValidMemberShips(customMemberShipId, coBrandCardCompany?.Data!);

        StateHasChanged();
    }
    public bool ShowRequestDeleteMemberShipConfirmation { get; set; }
    async Task<bool> IsValidMemberShips(List<MemberShipInfoDto> memberShips, CompanyLookup selectedCompany)
    {
        IsAllowToDeleteMemberShip = false;
        if (memberShips.Count == 0)
        {
            return false;
        }

        var membershipIds = memberShips.Select(mi => mi.ClubMembershipId);
        if (membershipIds?.Count() > 1)
        {
            Notification.Failure("Multiple membership Info are found for the selected company!");
            return false;
        }

        if (!int.TryParse(membershipIds?.FirstOrDefault(), out int _memberShipId))
        {
            Notification.Failure("Invalid Membership Id");
            return false;
        }

        CoBrand.MemberShipId = _memberShipId;
        CoBrand.Company = selectedCompany;

        var memberShipConflicts = await _memberShipService.GetMemberShipIdConflicts(SelectedCard?.CivilId!, selectedCompany.CompanyId!, _memberShipId.ToString());
        if (memberShipConflicts.IsSuccess && memberShipConflicts.Data!.Any())
        {
            CoBrand!.OldCivilId = memberShipConflicts.Data!.FirstOrDefault()?.CivilId;
            IsAllowToDeleteMemberShip = true;
            Notification.Failure(GlobalResources.DuplicateMemberShipID);
            IsAllowToDeleteMemberShip = true;
            return false;
        }

        return true;

    }
    async Task RequestDeleteMemberShip()
    {
        var removeMemberShipResponse = await _memberShipService.RequestDeleteMemberShip(new()
        {
            CivilId = CoBrand.OldCivilId!,
            CompanyId = (int)coBrandCardCompany?.Data?.CompanyId!,
            ClubMembershipId = CoBrand.MemberShipId.ToString()!,
            RequestDate = DateTime.Now,
            RequestorReason = Model.ReasonForDeleteRequest!,
            RequestedBy = AuthManager.GetUser()?.KfhId?.ToInt() ?? 0
        });

        ShowRequestDeleteMemberShipConfirmation = false;

        if (removeMemberShipResponse.IsSuccess)
        {
            Notification.Success(removeMemberShipResponse.Message);
            IsAllowToDeleteMemberShip = false;
        }
        else
        {
            Notification.Failure(removeMemberShipResponse.Message);
        }

        StateHasChanged();

    }
    #endregion
}
