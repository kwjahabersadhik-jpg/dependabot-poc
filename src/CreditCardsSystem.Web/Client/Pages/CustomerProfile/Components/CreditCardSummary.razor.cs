using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Models.Access;
using CreditCardsSystem.Domain.Models.Card;
using CreditCardsSystem.Web.Client.Pages.CardRequest;
using Kfh.Aurora.Blazor.Components.UI;
using Kfh.Aurora.Utilities;
using Microsoft.AspNetCore.Components;
using static Telerik.Blazor.ThemeConstants;

namespace CreditCardsSystem.Web.Client.Pages.CustomerProfile.Components;

public partial class CreditCardSummary
{

    [Parameter]
    [SupplyParameterFromQuery]
    public string CivilId { get; set; } = default!;

    [Parameter]
    public EventCallback<Task> LoadCreditCards { get; set; }

    [Parameter]
    public EventCallback<Task> ReloadStandingOrders { get; set; }

    //TODO: remove hard coded value
    public List<string> StatusValue { get; set; } = ["All", "Active", "Approved", "Pending", "Closed", "Lost"];
    public List<string> CardCategories { get; set; } = ["All", CardCategoryType.Primary.ToString(), CardCategoryType.Supplementary.ToString(), CardCategoryType.Normal.ToString()];

    //double _zero = 0.000;
    bool Dialog { get; set; }
    bool StopLoadingCardBalance { get; set; }

    bool CardDetails { get; set; }
    string FilterCardStatus { get; set; } = "Active";
    string FilterCardCategory { get; set; } = "All";
    CreditCardDto? _selectedCard = new();
    OffCanvas CreditCarDetailsDrawerRef { get; set; } = default!;
    List<CreditCardDto>? Cards => State?.CreditCards.Data;
    List<CreditCardDto>? FilteredCards { get; set; } = default;

    RequestMakerPage cardRequestFormRef { get; set; } = default!;

    string StatusClass(int status) => Helpers.GetStatusClass((CreditCardStatus)status);


    public bool IsLoadedAllCardBalances { get; set; } = false;

    bool IsNewProfileLoaded { get; set; } = true;
    protected override async Task OnInitializedAsync()
    {
        IsLoadedAllCardBalances = false;
    }
    protected override async Task OnParametersSetAsync()
    {
        if (State?.CreditCards?.Status == DataStatus.Success)
        {
            if (!IsNewProfileLoaded)
                return;

            FilterCardStatus = "All";
            FilterCardCategory = "All";
            IsNewProfileLoaded = false;
            await LoadData();
        }
        else
        {
            IsNewProfileLoaded = true;
        }
    }

    async Task OnChangeCardStatus(string filter)
    {
        FilterCardStatus = filter;
        await LoadData();
    }

    async Task OnChangeCardCategory(string filter)
    {
        FilterCardCategory = filter;
        await LoadData();
    }

    async Task LoadData()
    {
        Console.WriteLine("LoadData");
        var allCards = Cards!.ToList();
        if (!Enum.TryParse(typeof(CreditCardStatus), FilterCardStatus, out object _selectedCardStatus))
        {
            return;
        }

        if ((CreditCardStatus)_selectedCardStatus == CreditCardStatus.Pending)
        {
            FilteredCards = allCards.Where(c => ConfigurationBase.PendingStatuses.Any(s => s.Equals((CreditCardStatus)c.StatusId))).ToList();
        }
        else if ((CreditCardStatus)_selectedCardStatus == CreditCardStatus.Closed)
        {
            FilteredCards = allCards.Where(c => ConfigurationBase.ClosedStatuses.Any(s => s.Equals((CreditCardStatus)c.StatusId))).ToList();
        }
        else
        {
            FilteredCards = FilterCardStatus == "All" ? allCards : allCards.Where(x => x.StatusId == (int)_selectedCardStatus).ToList();
        }

        FilteredCards = FilterCardCategory == "All" ? FilteredCards : FilteredCards.Where(x => x.CardCategory == Enum.Parse<CardCategoryType>(FilterCardCategory)).ToList();

        if (!IsLoadedAllCardBalances)
        {
            _ = Task.Run(async () => { await LoadAllCardBalances(); });
        }

        await Task.CompletedTask;

        CardgridRef?.Rebind();
    }
    async Task LoadAllCardBalances()
    {
        //List<Task> balanceTasks = new();
        foreach (var card in FilteredCards!)
        {
            if (card.CardBalance.Status != DataStatus.Uninitialized)
                continue;

            if (CancellationToken.IsCancellationRequested)
                return;

            if (StopLoadingCardBalance)
                return;

            await LoadCardBalance(card);
        }

        //await Task.WhenAll(balanceTasks);
        IsLoadedAllCardBalances = !Cards?.Any(x => x.CardBalance.Status == DataStatus.Uninitialized) ?? true;
    }

    async Task LoadSingleCardBalance(decimal requestId)
    {
        CreditCardDto card = FilteredCards!.First(x => x.RequestId == requestId);
        card.CardBalance.Reset();
        await LoadCardBalance(card);
    }
    async Task LoadCardBalance(CreditCardDto card)
    {
        if (StopLoadingCardBalance || (card.CardBalance.Status is not DataStatus.Uninitialized)) return;

        card.CardBalance.Loading();
        try
        {
            var cardBalance = await CardDetailsAppService.GetCardInfo(card.RequestId, includeCardBalance: true, cancellationToken: CancellationToken);
            if (cardBalance.IsSuccessWithData)
                card.CardBalance.SetData(cardBalance.Data!);
            else
                card.CardBalance.Error(new(cardBalance.Message));
        }
        catch (Exception)
        {
            card.CardBalance.Error(new("Error"));
        }

        CardgridRef?.Rebind();
    }


    #region Actions

    async Task GoToStatementPage(CreditCardDto card)
    {
        if (!IsAllowTo(Permissions.CreditCardStatement.View()))
        {
            Notification.Failure(GlobalResources.NotAuthorized);
            return;
        }

        CancelTask();
        NavigateTo($"credit-card-statement?RequestId={card?.RequestId}");
        await Task.CompletedTask;
    }

    async Task GoToNewIssuePage()
    {

        if (!await IsAllowedToIssue())
        {
            Notification.Failure(message: GlobalResources.NotAuthorized);
            return;
        }


        if (CurrentState.GenericCustomerProfile.IsPendingBioMetric)
        {
            Notification.Failure(message: GlobalResources.BioMetricRestriction);
            return;
        }

        if (await CheckIsAnyPendingRequest())
        {
            Notification.Failure(GlobalResources.CannotIssueNewCard);
            return;
        }

        CancelTask();

        NavigateTo($"new-card-wizard?civilId={CurrentState.CurrentCivilId.Encode()!}");
    }

    void CancelTask()
    {
        StopLoadingCardBalance = true;
        Notification.Hide();
        Thread.Sleep(1000);
    }


    async Task<bool> IsAllowedToIssue() =>
            IsAllowTo(Permissions.Prepaid.Issue())
            || IsAllowTo(Permissions.PrepaidFC.Issue())
            || IsAllowTo(Permissions.ChargeCard.Issue())
            || IsAllowTo(Permissions.CoBrand.Issue());



    async Task<bool> CheckIsAnyPendingRequest()
    {
        IEnumerable<int> pendingStatuses = (ConfigurationBase.PendingStatuses).Cast<int>();
        return Cards is not null && Cards.Any(x => pendingStatuses.Any(status => status == x.StatusId));
    }

    #endregion




}
