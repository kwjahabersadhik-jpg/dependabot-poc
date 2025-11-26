using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Models.Access;
using CreditCardsSystem.Domain.Models.Card;
using CreditCardsSystem.Domain.Models.CardOperation;
using CreditCardsSystem.Domain.Models.Corporate;
using CreditCardsSystem.Utility.Extensions;
using CreditCardsSystem.Web.Client.Pages.CardRequest;
using Kfh.Aurora.Blazor.Components.UI;
using Kfh.Aurora.Utilities;
using Microsoft.AspNetCore.Components;
using Telerik.Blazor.Components;

namespace CreditCardsSystem.Web.Client.Pages.CorporateProfile.Components;

public partial class CorporateCreditCardList
{

    [Parameter]
    [SupplyParameterFromQuery]
    public string CivilId { get; set; } = default!;

    [Parameter]
    public EventCallback<Task> LoadCreditCards { get; set; }
    public TelerikGrid<CorporateCard> CardgridRef { get; set; } = null!;


    [Parameter]
    public List<CorporateCard> Cards { get; set; } = null!;

    public List<string> StatusValue { get; set; } = new List<string> { "All", "Active", "Pending", "Closed", "Lost" };
    public List<string> CardCategories { get; set; } = new List<string> { "All", CardCategoryType.Primary.ToString(), CardCategoryType.Supplementary.ToString(), CardCategoryType.Normal.ToString() };

    //double _zero = 0.000;
    bool Dialog { get; set; }
    bool CardDetails { get; set; }
    string FilterCardStatus { get; set; } = "All";
    string FilterCardCategory { get; set; } = "All";
    CorporateCard? _selectedCard = new();
    OffCanvas CreditCarDetailsDrawerRef { get; set; } = default!;

    List<CorporateCard>? FilteredCards { get; set; } = default;

    RequestMakerPage cardRequestFormRef { get; set; } = default!;
    public ValidateCardClosureResponse? CardClosureFormData { get; set; }
    public List<ListItemGroup<RequestType>> EligibleActions { get; set; } = new();

    protected override async Task OnInitializedAsync()
    {
        IsLoadedAllCardBalances = false;
        await base.OnInitializedAsync();
    }
    protected override async Task OnParametersSetAsync()
    {
        if (Cards is not null)
            await LoadData();

        NavigationManager.LocationChanged += NavigationManager_LocationChanged;
    }

    //public async Task ReflectCardStatus(NewCardStatus newCardStatus)
    //{
    //    FilteredCards!.ForEach((x) =>
    //    {
    //        if (x.RequestId == newCardStatus.RequestId)
    //            x.StatusId = (int)newCardStatus.Status;
    //    });

    //    StateHasChanged();
    //    await Task.CompletedTask;
    //}

    async Task OnChangeAction(CardRequestFormRequest selectedRequestType)
    {
        cardRequestFormRef!.SelectedCard = selectedRequestType!.Card;
        await cardRequestFormRef!.OnChangeAction(selectedRequestType.RequestType);
    }

    private void NavigationManager_LocationChanged(object? sender, Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs e)
    {
        CancellationToken.ThrowIfCancellationRequested();
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


    string StatusClass(int status) => Helpers.GetStatusClass((CreditCardStatus)status);

    string StatementPageLink(string requestId) => $"credit-card-statement?requestId={requestId}";
    string CustomerViewPageLink(string civilid) => $"customer-view?civilId={civilid.Encode()}";


    public bool IsLoadedAllCardBalances { get; set; } = false;

    async Task LoadData()
    {
        var allCards = Cards!.ToList();
        if (!Enum.TryParse(typeof(CreditCardStatus), FilterCardStatus, out object _selectedCardStatus))
            return;

        if ((CreditCardStatus)_selectedCardStatus == CreditCardStatus.Pending)
        {
            FilteredCards = allCards.Where(c => ConfigurationBase.PendingStatuses.Any(s => s.Equals((CreditCardStatus)c.StatusId))).ToList();
        }
        else
        {
            FilteredCards = FilterCardStatus == "All" ? allCards : allCards.Where(x => x.StatusId == (int)_selectedCardStatus).ToList();
        }

        FilteredCards = FilterCardCategory == "All" ? FilteredCards : FilteredCards.Where(x => x.CardCategory == Enum.Parse<CardCategoryType>(FilterCardCategory)).ToList();

        if (!EligibleActions.Exists(s => s.Text == RequestType.Detail.GetDescription()))
        {
            EligibleActions.Add(new(RequestType.Detail, RequestTypeGroup.General));
        }

        

        StateHasChanged();
        await Task.CompletedTask;
    }


 

 
 




}
