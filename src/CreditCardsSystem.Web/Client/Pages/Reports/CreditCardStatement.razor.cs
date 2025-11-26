using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Interfaces;
using CreditCardsSystem.Domain.Models.Access;
using CreditCardsSystem.Domain.Models.Card;
using CreditCardsSystem.Domain.Models.CardOperation;
using CreditCardsSystem.Domain.Models.CardStatement;
using CreditCardsSystem.Utility.Extensions;
using CreditCardsSystem.Web.Client.Components;
using CreditCardsSystem.Web.Client.Pages.CardDetails;
using CreditCardsSystem.Web.Client.Pages.CardRequest;
using Kfh.Aurora.Blazor.Components.UI;
using Kfh.Aurora.Utilities;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Globalization;
using System.Text.Json.Serialization;
using Telerik.Blazor.Components;
using Telerik.DataSource.Extensions;

namespace CreditCardsSystem.Web.Client.Pages.Reports;

public partial class CreditCardStatement : IDisposable
{
    [Inject] public ICardStatementReportAppService ReportAppService { get; set; } = default!;
    [Inject] public ICardDetailsAppService CardDetailsAppService { get; set; } = default!;
    [Inject] public ICustomerProfileAppService CustomerProfileAppService { get; set; } = default!;
    [Inject] public IRequestAppService RequestAppService { get; set; } = default!;

    [Parameter]
    [SupplyParameterFromQuery]
    public string RequestId { get; set; }


    [Parameter]
    [SupplyParameterFromQuery(Name = "cardNumber")]
    public string? CardNumber
    {
        get { return cardNumber.Decode()!; }
        set { cardNumber = value; }
    }

    private string? cardNumber;

    private RequestType currentRequestType { get; set; }

    private CardDetailState cardDetailsState = new();
    private CardDetailsResponse? Card => cardDetailsState?.MyCard?.Data;
    private CreditCardDto? creditCardDto { get; set; }

    private List<BreadcrumbItem> BreadcrumbItems { get; set; } = new();
    public OffCanvas FiltersDrawerRef { get; set; } = default!;

    private string SelectedMonth { get; set; } = null!;
    private int SelectedYear { get; set; }
    private string CardimagePath { get; set; } = string.Empty;

    public enum GenerateFor
    {
        ViewOnly = 1,
        PrintOnly
    }

    //public class TabInfo
    //{
    //    public FilterType FilterType { get; set; }
    //    public Tab Tab { get; set; } = default!;
    //}

    //public enum FilterType
    //{
    //    DebitCreditFilter = 1,
    //    DurationFilter = 2,

    //}

    //public OffCanvas CardDetailRef { get; set; } = default!;

    private int ActiveTabIndex { get; set; } = 0;
    private List<int> Years { get; } = new();
    private class Months
    {
        public string? Value { get; init; }
        public string? Text { get; init; }
    }
    private List<Months> MonthsDataBind { get; set; } = new();
    private TableDataSource TableDataSource { get; set; } = new();
    List<CustomCardTransactionsDTO>? transactions = new();
    List<CustomCardTransactionsDTO>? filteredTransactions = new();
    List<CustomCardTransactionsDTO>? DefaultFilteredTransactions = new();
    DataItem<List<IGrouping<DateTime?, CustomCardTransactionsDTO>>> groupedTransactions { get; set; } = new();
    private TelerikDateRangePicker<DateTime> DatePicker { get; set; } = null!;
    private DateTime StartValue { get; set; } = DateTime.Now;
    private DateTime EndValue { get; set; } = DateTime.Now;
    private CreditCardStatus cardStatus => cardStatementBanner?.Data?.CardStatus ?? CreditCardStatus.Pending;
    private bool IsViewOnlySupplementary => cardStatus is CreditCardStatus.Closed or CreditCardStatus.ChargeOff;
    private bool IsAllowSupplementary { get; set; }
    RequestMakerPage? cardRequestFormRef { get; set; } = default!;
    DataItem<CardStatementBanner?> cardStatementBanner { get; set; } = new();
    DataItem<List<OwnedCard>> OwnedCards { get; set; } = new();

    List<OwnedCard> FilteredOwnedCards { get; set; } = new();

    public bool RenderedPage { get; set; }

    protected CancellationTokenSource? PrintReportCancellationTokenSource;
    protected CancellationToken PrintReportCancellationToken => (PrintReportCancellationTokenSource ??= new()).Token;


    protected CancellationToken SupplementaryCancellationToken = default;
    protected CancellationToken StatementCancellationToken = default;
    protected CancellationToken ActionMenuCancellationToken = default;

    protected override async Task OnInitializedAsync()
    {

        if (!await IsAuthorized())
            return;

        await PrepareFilterInputs();
        SetStatementTitle();

        if (CurrentState.CurrentRequestId == Convert.ToDecimal(RequestId))
            CurrentState.CurrentRequestId = null;

    }


    private async Task OnChangeCardSelection(decimal? requestId = null, bool reloadOwnCardList = false)
    {
        if (requestId is not null)
            CurrentState.CurrentRequestId = requestId;

        await LoadBannerSectionData(reloadOwnCardList);
        await LoadDetailSectionData();
    }

    private async Task<bool> IsAuthorized()
    {
        if (IsAllowTo(Permissions.CreditCardStatement.View()))
            return true;

        Notification.Failure(GlobalResources.NotAuthorized);
        await LoadBannerOnUnAuthorizedUsers();
        return false;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        RenderedPage = !firstRender;
        NavigationManager.LocationChanged += (_, _) => PrintReportCancellationTokenSource?.Cancel();
        //NavigationManager.LocationChanged += (_, _) => CancellationTokenSource?.Cancel();
    }

    bool shouldRender = true;
    protected override async Task OnParametersSetAsync()
    {
        if (!await IsAuthorized())
            return;

        shouldRender = await GetRequestIdFromParameter();
        if (!shouldRender)
            return;

        await OnChangeCardSelection(reloadOwnCardList: true);
    }

    private async Task LoadBannerOnUnAuthorizedUsers()
    {
        shouldRender = await GetRequestIdFromParameter();
        if (!shouldRender)
            return;

        await ReLoadProfileIfNotLoaded();
        LoadBreadCrumbs();
    }

    //protected override bool ShouldRender() => GetRequestIdFromParameter().GetAwaiter().GetResult();

    private async Task<bool> GetRequestIdFromParameter()
    {
        if (RequestId is null && CardNumber is null || (CurrentState.CurrentRequestId == Convert.ToDecimal(RequestId)))
            return false;

        if (RequestId is null && CardNumber is not null)
        {
            var requestIdResponse = await RequestAppService.GetRequestIdByCardNumber(CardNumber!);
            if (!requestIdResponse.IsSuccess)
            {
                Notification.Failure(requestIdResponse.Message);
                return false;
            }

            CurrentState.CurrentRequestId = Convert.ToDecimal(requestIdResponse.Data);
        }
        else
        {
            CurrentState.CurrentRequestId = Convert.ToDecimal(RequestId);
        }


        return true;
    }
    private async Task LoadDetailSectionData()
    {

        await CancelTokens(GenerateFor.ViewOnly);
        await CreateTokens();

        List<Task> tasks = [SearchTransactions(GenerateFor.ViewOnly, isCancellationTokenCreated: true), GetSupplementaryCardsAsync(), GetCardActionsAsync()];
        await Task.WhenAny(tasks);
    }
    private async Task LoadBannerSectionData(bool reloadList)
    {
        if (CurrentState.CurrentRequestId is null)
            return;

        List<Task> tasks = [ReLoadProfileIfNotLoaded(), LoadCardDetail(reloadList)];

        await Task.WhenAll(tasks);

        LoadBreadCrumbs();
    }
    private async Task LoadCardDetail(bool reloadOwnCardList)
    {

        if (cardStatementBanner.Status == DataStatus.Loading)
            return;

        cardStatementBanner.Loading();
        var cardDetail = await CardDetailsAppService.GetCardInfoMinimal(CurrentState.CurrentRequestId ?? 0);
        if (!cardDetail.IsSuccess)
        {
            cardStatementBanner.Error(new(cardDetail.Message));
            return;
        }

        cardStatementBanner.SetData(new()
        {
            CardImagePath = GetCreditCardImage(cardDetail.Data!.CardType),
            CardNumber = cardDetail.Data!.AUBCardNumberDto ?? cardDetail.Data!.CardNumberDto,
            ProductName = cardDetail.Data!.ProductName,
            CivilId = cardDetail.Data!.CivilId,
            CardStatus = cardDetail.Data!.CardStatus
        });

        CurrentState.CurrentCivilId = cardStatementBanner?.Data?.CivilId;


        IsAllowSupplementary = !cardDetail.Data.IsSupplementary && ConfigurationBase.IsEnabledSupplementaryFeature && (cardDetail.Data?.IsCorporateCard == false) && (cardDetail.Data?.CardType is ConfigurationBase.AlOsraPrimaryCardTypeId || cardDetail.Data?.ProductType is ProductTypes.ChargeCard);

        if (reloadOwnCardList)
            await LoadOwnedCards();
    }
    private async Task GetCardActionsAsync()
    {
        //Console.WriteLine($"CCLog GetCardActionsAsync  {CurrentState.CurrentRequestId} {ActionMenuCancellationToken.IsCancellationRequested}");
        cardDetailsState.MyCard ??= new();

        //if (cardDetailsState.MyCard?.SubStatus == DataStatus.Loading)
        //    return;

        cardDetailsState.MyCard.Loading();
        StateHasChanged();
        var cardDetailsResponse = await CardDetailsAppService.GetCardInfo(CurrentState.CurrentRequestId, includeCardBalance: true, cancellationToken: ActionMenuCancellationToken);

        if (!cardDetailsResponse.IsSuccess)
        {
            //cardDetailsState.MyCard.Error(new Exception(cardDetailsResponse.Message));
            cardDetailsState.MyCard.Error(new(cardDetailsResponse.Message));
            Notification.Failure("Unable to load card operation action menu");
        }
        else
        {

            cardDetailsState.MyCard.SetData(cardDetailsResponse.Data!);
            _ = DateTime.TryParse(Card!.Expiry!, out DateTime _expiry);
            creditCardDto = new()
            {
                CurrencyISO = Card.Currency?.CurrencyIsoCode ?? "",
                RequestId = (decimal)CurrentState.CurrentRequestId!,
                CardBalance = cardDetailsState.MyCard,
                CardNumber = Card.CardNumberDto ?? "",
                CardType = Card.CardType.ToString(),
                BranchId = Card.BranchId,
                CardLimit = Card.ApproveLimit,
                ExpirationDate = _expiry,
                StatusId = (int)Card.CardStatus,
                HoldAmount = Card.DepositAmount,
                ApprovedLimit = Card.ApproveLimit,
                MemberShipId = cardDetailsState.MyCard?.Data?.Parameters?.ClubMembershipId ?? ""
            };
        }

        LoadBreadCrumbs();
    }

    private async Task LoadOwnedCards()
    {
        OwnedCards.Reset();

        if (RenderedPage && OwnedCards is { Status: DataStatus.Success or DataStatus.Loading })
        {
            return;
        }

        OwnedCards.Loading();
        var ownedCardsResponse = await CustomerProfileAppService.GetCustomerCardsLite(new() { CivilId = CurrentState.CurrentCivilId });
        if (!ownedCardsResponse.IsSuccess)
        {
            OwnedCards.Error(new(ownedCardsResponse.Message));
            return;
        }

        var activeCards = ownedCardsResponse.Data!.Where(x => x.StatusId == (int)CreditCardStatus.Active).OrderByDescending(x => x.ExpirationDate);

        var nonActiveCards = ownedCardsResponse.Data!.Where(x => x.StatusId != (int)CreditCardStatus.Active).OrderByDescending(x => x.ExpirationDate);


        OwnedCards.SetData(activeCards.Concat(nonActiveCards).Select(x => new OwnedCard()
        {
            CardImagePath = GetCreditCardImage(x.CardType),
            CardNumber = x.AUBCardNumberDto ?? x.CardNumberDto,
            CardNumberDto = DisplayCardNumber(x.AUBCardNumberDto ?? x.CardNumberDto),
            CardStatusInEnglish = x.Status,
            CardStatus = (CreditCardStatus)x.StatusId,
            CivilId = x.CivilId,
            RequestId = x.RequestId,
            ProductName = x.ProductType,
            Expiry = x.ExpirationDate,
            IsAUB = x.IsAUB
        }).ToList());

        FilteredOwnedCards = OwnedCards.Data ?? [];
    }



    /// <summary>
    /// Cancellation token
    /// </summary>
    async Task CreateTokens()
    {
        if (CancellationTokenSource != null)
        {
            this.CancellationTokenSource = null;
        }

        var mainToken = CancellationToken;

        this.StatementCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(mainToken).Token;
        this.SupplementaryCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(mainToken).Token;
        this.ActionMenuCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(mainToken).Token;

        await Task.CompletedTask;
    }

    /// <summary>
    /// Cancellation token
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    async Task CancelTokens(GenerateFor type)
    {
        if (type == GenerateFor.ViewOnly && (groupedTransactions.Status == DataStatus.Loading && CancellationTokenSource is not null))
        {
            await CancelViewAction();

            await CancelPrintAction();
            StateHasChanged();
        }

        if (type == GenerateFor.PrintOnly && (Notification.IsProcessing && PrintReportCancellationTokenSource is not null))
        {
            await CancelPrintAction();
            StateHasChanged();
        }


        async Task CancelPrintAction()
        {
            if (PrintReportCancellationTokenSource is not null)
            {
                await PrintReportCancellationTokenSource!.CancelAsync();
                PrintReportCancellationTokenSource = null;
            }
        }

        async Task CancelViewAction()
        {
            await CancellationTokenSource!.CancelAsync();
            CancellationTokenSource = null;
            groupedTransactions.Error(new("Cancelled loading previous card transactions"));
        }
    }

    public async Task SearchTransactions(GenerateFor type, string format = "PDF", bool isCancellationTokenCreated = false)
    {

        //Console.WriteLine($"CCLog SearchTransactions  {CurrentState.CurrentRequestId}  {StatementCancellationToken.IsCancellationRequested}");

        if (!isCancellationTokenCreated)
        {
            await CancelTokens(type);
            await CreateTokens();
        }

        if (!Validate())
        {
            //if (FiltersDrawerRef.IsOpen)
            //    await FiltersDrawerRef.ToggleAsync();
            return;
        }


        var request = BindRequest();

        if (type == GenerateFor.ViewOnly)
        {
            groupedTransactions.Loading();
            StateHasChanged();



            var cardStatementReponse = await ReportAppService.GetCreditCardStatement(request, (StatementCancellationToken));

            if (!cardStatementReponse.IsSuccess)
            {
                filteredTransactions = new();
                TableDataSource = new TableDataSource();
                Notification.Failure(cardStatementReponse.Message);
                groupedTransactions.Error(new(cardStatementReponse.Message));
                StateHasChanged();
                return;
            }

            transactions = cardStatementReponse.Data!.CreditCardTransaction;
            TableDataSource = cardStatementReponse.Data;

            if (request!.Parameter != null)
                TableDataSource.Cashback = await ReportAppService.GetCashBack(request.Parameter.RequestId);

            await ApplyFilter();
            //await AssignChipFilter();
        }
        else
        {
            if (TableDataSource.CreditCardTransaction?.Count == 0)
                Notification.Failure("Transactions not found to download");

            Notification.Processing(new(Title: "Download card statement", Message: "Preparing statment to download..."));

            var eFormResponse = await ReportAppService.PrepareReport(request, format, CancellationToken);

            if (!eFormResponse.IsSuccessWithData)
            {
                filteredTransactions = new List<CustomCardTransactionsDTO>();
                TableDataSource = new TableDataSource();
                Notification.Failure(eFormResponse.Message);
                return;
            }

            if (!eFormResponse.IsSuccess)
            {
                Notification.Failure("Unable to download, try again later from card list");
                return;
            }
            Notification.Success("Card statement successfully downloaded");

            var streamData = new MemoryStream(eFormResponse.Data!.FileBytes!);
            using var streamRef = new DotNetStreamReference(stream: streamData);

            await Js.InvokeVoidAsync("downloadFileFromStream", $"{eFormResponse.Data?.FileName}", streamRef);
        }

        StateHasChanged();

        if (FiltersDrawerRef.IsOpen)
            await FiltersDrawerRef.ToggleAsync();

        Notification.Hide();


    }
    private CreditCardStatementReportRequest? BindRequest()
    {
        var transactionType = filterSummary.TryGetValue(FilterPropertyEnum.Type, out string? value) ? value : "all";

        (bool isCredit, bool isDebit) = transactionType switch
        {
            "all" => (true, true),
            "debit" => (false, true),
            "credit" => (true, false),
            _ => (true, true)
        };

        string description = transactionType is "Hold" or "Decline" ? transactionType : "";

        _ = Enum.TryParse(filterSummary[FilterPropertyEnum.Period], out ReportType reportType);


        var (fromDate, toDate) = SetStatementTitle();
        var (fromDateStr, toDateStr) = reportType switch
        {
            ReportType.CycleToDate => ("", ""),
            ReportType.MonthYear => (SelectedMonth, SelectedYear.ToString()),
            ReportType.FromToDate => (fromDate.ToString(ConfigurationBase.ReportDateFormat), toDate.ToString(ConfigurationBase.ReportDateFormat)),
            _ => ("", ""),
        };

        return new CreditCardStatementReportRequest()
        {
            Name = "",
            FromDate = fromDateStr,
            ToDate = toDateStr,
            CardNo = "",
            NoTransactions = 0,
            ReportType = reportType,
            Parameter = new CreditCardStatementReportParameter()
            {
                RequestId = CurrentState.CurrentRequestId ?? 0,
                FromDate = fromDate,
                ToDate = toDate,
                IsCredit = isCredit,
                IsDebit = isDebit,
                Description = description,
            }
        };
    }
    private void LoadBreadCrumbs()
    {
        string customerName = CurrentState?.CustomerProfile?.FullName() ?? "";
        string? customerCivilId = string.IsNullOrEmpty(CurrentState?.CurrentCivilId) ? cardStatementBanner?.Data?.CivilId : CurrentState?.CurrentCivilId;

        BreadcrumbItems =
        [
            new() { Text = customerName, Url = "/customer-view?CivilId=" + customerCivilId?.Encode() },
            new() { Text = DisplayCardNumber(cardStatementBanner?.Data?.CardNumber)?.Replace(" ","") ?? "" }
        ];



        StateHasChanged();
    }

    private async Task ReLoadProfileIfNotLoaded()
    {

        if (CurrentState.CustomerProfile is null)
        {
            State ??= new();

            if (State.GenericCustomerProfile.Status == DataStatus.Loading)
                return;

            State.GenericCustomerProfile.Loading();

            var customerProfile = await CustomerProfileAppService.GetCustomerProfileMinimal(new() { RequestId = RequestId });
            if (!customerProfile.IsSuccess)
            {
                State.GenericCustomerProfile.Error(new(customerProfile.Message));
                Notification.Failure(customerProfile.Message);
                return;
            }


            State.GenericCustomerProfile.SetData(customerProfile.Data ?? new());
            CurrentState.GenericCustomerProfile = State.GenericCustomerProfile.Data!;

            var profile = State.GenericCustomerProfile.Data;

            if (profile is not null)
            {
                CurrentState.CustomerProfile ??= new();
                CurrentState.CustomerProfile.RimCode = profile.RimCode.ToString();
                CurrentState.CustomerProfile.CustomerType = profile.CustomerType ?? "";
                CurrentState.CustomerProfile.DateOfBirth = profile.BirthDate;
                CurrentState.CustomerProfile.FirstName = profile.FirstName ?? "";
                CurrentState.CustomerProfile.LastName = profile.LastName ?? "";
                CurrentState.GenericCustomerProfile = State.GenericCustomerProfile.Data!;
                CurrentState.CurrentCivilId = profile.CivilId;
            }


        }
    }

    private async Task GetSupplementaryCardsAsync()
    {

        //Console.WriteLine($"CCLog GetSupplementaryCardsAsync  {CurrentState.CurrentRequestId}  {SupplementaryCancellationToken.IsCancellationRequested}");
        if (!IsAllowSupplementary || CurrentState.CurrentRequestId is null) return;

        cardDetailsState.SupplementaryCards.Loading();
        var cardDetailsResponse = await CardDetailsAppService.GetSupplementaryCardsByRequestId((decimal)CurrentState.CurrentRequestId!, cancellationToken: SupplementaryCancellationToken);

        if (!cardDetailsResponse.IsSuccess)
        {
            cardDetailsState.SupplementaryCards.Error(new Exception(cardDetailsResponse.Message));
            StateHasChanged();
            return;
        }



        cardDetailsState.SupplementaryCards.SetData(cardDetailsResponse.Data!);
        ActiveTabIndex = 0;
        IsAllowSupplementary = !(cardDetailsState.MyCard?.Data?.IsSupplementaryCard ?? false);
        StateHasChanged();
    }

    async Task ReloadData()
    {

        if (currentRequestType is (RequestType.Activate or RequestType.CardPayment or RequestType.CreditReverse))
        {
            await GetCardActionsAsync();
        }

    }

    async Task ReflectCardStatus(NewCardStatus newCardStatus) => await Task.CompletedTask;

    async Task OnChangeAction(CardRequestFormRequest selectedRequestType)
    {
        currentRequestType = selectedRequestType.RequestType;
        cardRequestFormRef!.SelectedCard = selectedRequestType.Card;
        await cardRequestFormRef!.OnChangeAction(selectedRequestType.RequestType);
    }

    public string StatementTitle { get; set; } = default!;
    (DateTime start, DateTime end) SetStatementTitle()
    {

        string dateFormat = ConfigurationBase.StatementDateFormat;
        _ = Enum.TryParse(filterSummary[FilterPropertyEnum.Period], out ReportType reportType);

        DateTime startDate = DateTime.MinValue;
        DateTime endDate = DateTime.Now;
        DateTime today = DateTime.Today;
        DateTime cycleDateFrom = DateTime.Now;
        DateTime cycleDateTo = DateTime.Now;

        if (today.Day <= 15)
        {
            cycleDateFrom = cycleDateFrom.AddMonths(-1);
        }

        cycleDateFrom = new DateTime(cycleDateFrom.Year, cycleDateFrom.Month, 16);

        (startDate, endDate) = reportType switch
        {
            ReportType.CycleToDate => (cycleDateFrom, cycleDateTo),
            ReportType.MonthYear => (new DateTime(SelectedYear, Convert.ToInt16(SelectedMonth), 1), today),
            ReportType.FromToDate => DateRangeToDates("usingDates"),
            _ => new(today, today)
        }; ;


        StatementTitle = reportType switch
        {
            ReportType.CycleToDate => $"{cycleDateFrom.Formed(dateFormat)} - {cycleDateTo.Formed(dateFormat)}",
            ReportType.MonthYear => startDate.Formed("MMMM, yyyy"),
            ReportType.FromToDate => $"{startDate.Formed(dateFormat)} - {endDate.Formed(dateFormat)}",
            _ => ""
        };

        return (startDate, endDate);
    }

    public async Task OnSearchTransaction(string keyword)
    {

        if (!string.IsNullOrEmpty(keyword))
        {
            string? cardType = null;
            decimal? amount = null;
            DateTime? dateValue = null;

            if (keyword.Equals("pri", StringComparison.InvariantCultureIgnoreCase))
            {
                cardType = "0";
            }

            if (keyword.Equals("supp", StringComparison.InvariantCultureIgnoreCase))
            {
                cardType = "1";
            }

            if (decimal.TryParse(keyword, out decimal _amount))
            {
                amount = _amount;
            }

            if (DateTime.TryParse(keyword, out DateTime _dateValue))
            {
                dateValue = _dateValue;
            }

            filteredTransactions = DefaultFilteredTransactions?.Where(x =>
            x.descriptionField.Contains(keyword, StringComparison.InvariantCultureIgnoreCase)
            || x.currencyField.Contains(keyword, StringComparison.InvariantCultureIgnoreCase)
            || (cardType != null && x.cardTypeField == cardType)
            || (amount != null && x.amountField == amount)
            || (amount != null && x.CardCurrentBalance == amount)
            || (x.dateField != null && x.dateField.Value.ToString("dd/MMM") == keyword)
            ).ToList();


        }
        else
        {
            filteredTransactions = DefaultFilteredTransactions;
        }

        groupedTransactions.SetData(filteredTransactions?.GroupBy(ft => ft.dateField).OrderByDescending(ft => ft.Key).ToList() ?? new());
    }

    #region FIlters

    async Task OpenFilter()
    {
        if (groupedTransactions.Status == DataStatus.Loading)
        {
            Notification.Show(AlertType.Info, "Please wait to complete current process!");
            return;
        }

        await FiltersDrawerRef.ToggleAsync();
    }
    async Task PrepareFilterInputs()
    {
        //Default selection is Cycle to date

        filterSummary[FilterPropertyEnum.Period] = ReportType.CycleToDate.ToString();
        filterSummary[FilterPropertyEnum.Type] = "all";

        // filling drop down list for months & years
        for (var i = 1; i <= 12; i++)
        {
            MonthsDataBind.Add(new()
            {
                Text = new DateTime(2010, i, 1).ToString("MMMM"),
                Value = i.ToString()
            });
        }

        SelectedMonth = DateTime.Now.Month.ToString();
        SelectedYear = DateTime.Now.Year;

        for (var i = 1990; i <= DateTime.Now.Year; i++)
        {
            Years.Add(i);
        }
        await Task.CompletedTask;


    }

    public Dictionary<FilterPropertyEnum, string> filterSummary { get; set; } = new();
    public List<string> chipFilterSummary { get; set; } = new();



    private Task ApplyFilter(string keyword = "")
    {
        if (!TableDataSource.CreditCardTransaction.AnyWithNull())
        {
            groupedTransactions.SetData([]);
            StateHasChanged();
            return Task.CompletedTask;
        }


        filteredTransactions = filterSummary[FilterPropertyEnum.Type].ToLower() switch
        {
            "decline" => transactions!.Where(c => c.descriptionField.Contains("Decline")).ToList(),
            "hold" => transactions!.Where(c => c.descriptionField.Contains("AUTH CODE")).ToList(),
            "credit" => transactions!.Where(d => d.isCreditField).ToList(),
            "debit" => transactions!.Where(d => d.isCreditField == false).ToList(),
            _ => transactions
        };



        DefaultFilteredTransactions = filteredTransactions?.ToList();
        groupedTransactions.SetData(filteredTransactions?.GroupBy(ft => ft.dateField).OrderByDescending(ft => ft.Key).ToList() ?? []);
        StateHasChanged();
        return Task.CompletedTask;
    }
    private (DateTime start, DateTime end) DateRangeToDates(string range)
    {
        DateTime today = DateTime.Today;
        return range switch
        {
            "recent" => (StartValue = today.AddDays(-1), EndValue = today),
            "7days" => (StartValue = today.AddDays(-7), EndValue = today),
            "3months" => (StartValue = today.AddDays(-90), EndValue = today),
            "6months" => (StartValue = today.AddDays(-180), EndValue = today),
            "usingDates" => (StartValue, EndValue),
            _ => throw new Exception($"Unrecognized search value of {range}")
        };
    }


    


    #endregion

    #region Validations

    public bool Validate()
    {
        var (startDate, endDate) = DateRangeToDates("usingDates");

        if (filterSummary[FilterPropertyEnum.Period] == ReportType.MonthYear.ToString() && string.IsNullOrEmpty(SelectedMonth))
            return false;

        if (!(filterSummary[FilterPropertyEnum.Period] == ReportType.FromToDate.ToString()))
            return true;

        if (startDate.ToString(CultureInfo.CurrentCulture) == "" || endDate.ToString(CultureInfo.CurrentCulture) == "")
            return false;

        if ((startDate > endDate) && startDate != DateTime.MinValue && endDate != DateTime.MinValue)
            return false;

        return true;
    }

    public void Dispose()
    {
        Notification.Hide();
    }

    #endregion

    public async Task RefreshData()
    {
        StateHasChanged();
    }



}

public enum FilterPropertyEnum
{
    All,
    Type,
    Period
}


public class CardStatementBanner
{
    public string CardImagePath { get; set; } = string.Empty;
    [JsonIgnore]
    public string? CardNumber { get; set; }
    public string? CardNumberDto { get; set; }
    public string ProductName { get; set; } = null!;
    public string? CivilId { get; internal set; }
    public string? HolderName { get; internal set; }
    public CreditCardStatus? CardStatus { get; internal set; }
}

public class OwnedCard : CardStatementBanner
{
    public decimal RequestId { get; internal set; }
    public string? CardStatusInEnglish { get; internal set; }
    public DateTime? Expiry { get; internal set; }
    public int IsAUB { get; internal set; }
}