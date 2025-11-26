using CreditCardsSystem.Data.Models;
using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Interfaces;
using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.Access;
using CreditCardsSystem.Domain.Models.CardIssuance;
using CreditCardsSystem.Domain.Models.CardOperation;
using CreditCardsSystem.Domain.Models.CoBrand;
using CreditCardsSystem.Domain.Models.Customer;
using CreditCardsSystem.Domain.Models.Promotions;
using CreditCardsSystem.Domain.Shared.Enums;
using CreditCardsSystem.Domain.Shared.Models.Account;
using CreditCardsSystem.Utility.Extensions;
using CreditCardsSystem.Web.Client.Components;
using Kfh.Aurora.Organization;
using Kfh.Aurora.Utilities;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;
using Telerik.Blazor;
using Telerik.Blazor.Components;
using Telerik.DataSource.Extensions;
using Exception = System.Exception;

namespace CreditCardsSystem.Web.Client.Pages.CardIssuance;

public partial class IssueCardWizard : IDisposable
{
    //-------------------------------------------------------- Parameters --------------------------------------------------------

    [Parameter]
    [SupplyParameterFromQuery(Name = "CivilId")]
    public string CivilId
    {
        get { return civilId.Decode()!; }
        set { civilId = value; }
    }

    private string civilId;

    [CascadingParameter] public DialogFactory? Dialogs { get; set; }

    //-------------------------------------------------------- Inject ------------------------------------------------------------
    [Inject] private IRequestAppService RequestAppService { get; set; } = null!;
    [Inject] private IAccountsAppService AccountAppService { get; set; } = null!;
    [Inject] private ICardIssuanceAppService CardIssuanceAppService { get; set; } = null!;
    [Inject] private IAddressAppService AddressService { get; set; } = null!;
    [Inject] private ICurrencyAppService CurrencyAppService { get; set; } = null!;
    [Inject] private IPromotionsAppService PromotionsAppService { get; set; } = null!;
    [Inject] private IMemberShipAppService MemberShipAppService { get; set; } = null!;
    [Inject] private ILookupAppService LookupAppService { get; set; } = null!;
    [Inject] private ICustomerProfileAppService GenericCustomerProfileAppService { get; set; } = null!;
    [Inject] private ICustomerProfileAppService CustomerProfileAppService { get; set; } = null!;
    [Inject] private ICorporateProfileAppService CorporateProfileAppService { get; set; } = null!;
    [Inject] private IEmployeeAppService EmployeeService { get; set; } = null!;
    [Inject] private IReportAppService ReportService { get; set; } = null!;
    [Inject] private ILogger<IssueCardWizard> Logger { get; set; } = null!;

    #region Private Properties
    public int ProductId => CardDefinition?.ProductId ?? 0;// cardDetailsState.MyCard?.Data?.CardType ?? 0;
    private CardDefinitionDto? CardDefinition { get; set; } = new();
    private DataItem<List<CreditCardPromotionDto>> CardPromotions { get; set; } = new();
    private CardIssueRequest CardRequest { get; set; } = new();
    private DataItem<List<AccountDetailsDto>> DebitAccounts { get; set; } = new();
    private DataItem<List<AccountDetailsDto>> CardAccounts { get; set; } = new();



    private EditContext EditContextRequest { get; set; } = null!;
    private List<AreaCodesDto> AreaCodes { get; set; } = new();
    private AddressType SelectedAddressType { get; set; } = AddressType.POBox;
    private GenericCustomerProfileDto? GenericCustomerProfileDto { get; set; }
    private ValidationMessageStore? validationMessage;
    private CardCurrencyDto? CardCurrency { get; set; }
    private AccountDetailsDto? SelectedDebitAccount { get; set; }
    private AccountDetailsDto? SelectedCardAccount { get; set; }

    private AccountDetailsDto? SelectedTransferCardAccount { get; set; }
    private IEnumerable<CreditCardApplication>? SelectedTransferCardAccounts { get; set; } = Enumerable.Empty<CreditCardApplication>();

    private IEnumerable<AddressTypeItem> AddressTypes { get; set; } = new List<AddressTypeItem>()
    {
        new (AddressType.POBox,AddressType.POBox.GetDescription()),
        new (AddressType.FullAddress,AddressType.FullAddress.GetDescription())
    };
    public List<CreditCardApplication> CreditCardApplicationsFiltered => financialPosition.Data?.Applications?.CreditCardApplications ?? new();
    public int? ReplaceCardId { get; set; }
    public CreditCardApplication? CardToReplace { get { return financialPosition?.Data?.SelectedApplication; } }
    private bool IsAllowToEditMembershipId { get; set; } = true;
    private bool IsAllowToDeleteMemberShip = false;
    private bool IsAllowSupplementary => ConfigurationBase.IsEnabledSupplementaryFeature && !IsCorporateCard && (ProductId == ConfigurationBase.AlOsraPrimaryCardTypeId || IsChargeCard);
    private bool IsPOBoxEnabled { get; set; } = true;
    private bool IsFullAddressEnabled { get; set; } = false;
    public bool IsExceptionCase { get; set; } = false;
    public bool IsAgreeToCreateMargin { get; set; } = false;
    private bool IsEmployeeCustomer { get; set; }
    decimal? CustomerSalary { get; set; }
    public bool IsCollateralSelected
    {
        get
        {
            return collateral != null;
        }
    }
    public bool IsAllowException
    {
        get
        {
            bool showException = collateral != null && collateral is Collateral.AGAINST_SALARY;

            if (showException && CardToReplace is not null)
                showException = SelectedTransferCardAccount != null;

            if (showException && CardToReplace is null)
                showException = true;

            if (!showException)
                showException = collateral is Collateral.EXCEPTION;

            return showException;
        }
    }

    public bool IsAllowExecution
    {
        get
        {
            bool showExecution = collateral is not Collateral.EXCEPTION;
            return showExecution;
        }
    }

    public Collateral? collateral => CardRequest.IssueDetailsModel.Collateral;

    public bool IsCobrandCard
    {
        get
        {
            //return CardDefinition?.Eligibility?.IsCobrandCredit || CardDefinition?.Eligibility?.IsCoBrandPrepaid ;
            if (CardDefinition?.Eligibility is null)
                return false;

            return CardDefinition?.Eligibility is { IsCobrandCredit: true } or { IsCoBrandPrepaid: true };

        }
    }
    public bool IsPrepaidCard
    {
        get
        {
            return CardDefinition!.Eligibility!.ProductType == ProductTypes.PrePaid || CardRequest.IssueDetailsModel.Card.ProductId == ConfigurationBase.AlOsraPrimaryCardTypeId;
        }
    }
    public bool IsCorporateCard
    {
        get
        {
            return CardDefinition!.Eligibility!.IsCorporate;
        }
    }
    private bool IsChargeCard => CardDefinition?.Eligibility?.ProductType == ProductTypes.ChargeCard;
    private bool IsTayseerCard => CardDefinition?.Eligibility?.ProductType == ProductTypes.Tayseer;
    private bool IsUsdCard => CardCurrency?.CurrencyIsoCode == ConfigurationBase.USDollerCurrency;
    public bool IsUsdChargeCard
    {
        get
        {
            return IsChargeCard && IsUsdCard;
        }
    }
    public bool AgainstDepositUsd
    {
        get
        {
            return collateral is Collateral.AGAINST_DEPOSIT && IsUsdCard;
        }
    }
    public bool AgainstSalaryUsd
    {
        get
        {
            return collateral is Collateral.AGAINST_SALARY && IsUsdCard;
        }
    }
    public bool AgainstMarginUsd
    {
        get
        {
            return collateral is Collateral.AGAINST_MARGIN && IsUsdCard;
        }
    }
    public bool DisableFinancialPosition => SelectedDebitAccount?.Acct is null || CardDefinition?.Eligibility?.ProductType is ProductTypes.PrePaid || CardDefinition.Eligibility.IsCorporate;
    private DataItem<bool> SalaryVerification { get; set; } = new();
    private DataItem<ValidateCurrencyResponse> CurrencyRate { get; set; } = new();
    private List<collateralRecord> collateralData = new();
    private record collateralRecord(string name, Collateral value);
    private bool canLoadDebitAccount => CardDefinition?.Eligibility?.ProductType is not (ProductTypes.Tayseer or ProductTypes.ChargeCard) || IsCorporateCard;
    public string ViewType = "GridView";

    public class SearchProfileInput
    {
        public string? CivilId { get; set; }
        public string? SearchText { get; set; }
    }
    private readonly SearchProfileInput _searchProfileInput = new();

    //-------------------------------------------------------- Parameters --------------------------------------------------------

    //-------------------------------------------------------- Variables --------------------------------------------------------
    private DataItem<IEnumerable<CardEligiblityMatrixDto>> CardsEligibilityMatrixDto { get; set; } = new();
    private IEnumerable<CardEligiblityMatrixDto> CardsEligibilityMatrixDtoFiltered { get; set; } = new List<CardEligiblityMatrixDto>();
    private DataItem<CardDefinitionDto> SelectedCard { get; set; } = new();

    private EditContext? _editContext;
    private List<CardCurrencyDto> CurrenciesDto { get; set; } = new();
    private string SelectedCardCurrency { get; set; } = "786";
    private class ProductType
    {
        public string Text { get; set; } = string.Empty;
        public ProductTypes Value { get; set; }
    }
    private List<ProductType> ProductTypesList { get; set; } = new();
    private ProductTypes SelectedProductType { get; set; } = ProductTypes.All;

    SkeletonAnimationType animationType = SkeletonAnimationType.Wave;
    #endregion

    private DataItem<FinancialPosition> financialPosition { get; set; } = new();


    private record DeliveryOptionItem(string name, DeliveryOption value);
    private List<DeliveryOptionItem> DeliveryOptions { get; set; } =
[
    new(DeliveryOption.BRANCH.GetDescription(), DeliveryOption.BRANCH),
    new(DeliveryOption.COURIER.GetDescription(), DeliveryOption.COURIER)
];
    private List<Branch>? Branches { get; set; }
    private async Task LoadCardDetail()
    {
        if (CardDefinition is null)
        {
            Notification.Hide();
            return;
        }
        await ResetChargeCardFields();

        CardRequest = new()
        {
            IssueDetailsModel = new()
            {
                Card = new()
                {
                    ProductId = CardDefinition!.ProductId
                },
                CoBrand = IsCobrandCard ? new() : null,
                CorporateProfile = IsCorporateCard ? new() : null
            },
            SupplementaryModel = new()
        };


        List<Task> tasks = [BindCustomerProfile(), LoadDebitAccounts(CivilId!)];


        if (IsCobrandCard)
            tasks.Add(BindCoBrandCompanyNames());

        await Task.WhenAll(tasks);

        await BindDropDownData();
        await GetMembershipId();
        await BindEditContext();

        Notification.Hide();
    }

    //-------------------------------------------------------- Methods ---------------------------------------------------------------
    #region Methods
    private async Task BindDropDownData()
    {
        if (int.TryParse(CardRequest.BillingAddressModel.AreaId, out int _areaId))
            CardRequest.BillingAddressModel.City = AreaCodes.FirstOrDefault(ac => ac.AreaId == _areaId)?.AreaName ?? string.Empty;


        collateralData = [];
        foreach (var item in Enum.GetNames(typeof(CollateralForm)))
        {
            var _collateral = (Collateral)Enum.Parse(typeof(Collateral), item);

            if (CardCurrency!.CurrencyIsoCode == ConfigurationBase.USDollerCurrency && _collateral == Collateral.AGAINST_MARGIN)
                continue;

            collateralData.Add(new(_collateral.GetDescription(), _collateral));
        }



        Branches = (await LookupAppService.GetAllBranches())?.Data?.Select(x => new Branch()
        {
            BranchId = x.BranchId,
            Name = Regex.Replace(x.Name, "@\"[^A-Za-z]\"", " ").Trim()
        }).ToList();

        await Task.CompletedTask;
    }
    private async Task BindCustomerProfile()
    {

        //ApiResponseModel<GenericCustomerProfileDto> customerProfile;

        //if (State.GenericCustomerProfile.Data is null)
        var customerProfile = await GenericCustomerProfileAppService.GetCustomerProfileMinimal(new() { CivilId = CivilId });
        //else
        //    customerProfile = new ApiResponseModel<GenericCustomerProfileDto>() { Data = State.GenericCustomerProfile.Data, IsSuccess = true };

        if (!customerProfile.IsSuccessWithData) return;

        GenericCustomerProfileDto = customerProfile.Data!;
        CardRequest.Customer = new()
        {
            CivilId = GenericCustomerProfileDto.CivilId!,
            CustomerClassCode = GenericCustomerProfileDto.RimCode.ToString(),
            IsRetiredEmployee = GenericCustomerProfileDto.IsRetired,
            Salary = GenericCustomerProfileDto.Income
        };

        IsEmployeeCustomer = GenericCustomerProfileDto.IsEmployee;
        CustomerSalary = GenericCustomerProfileDto.Income;

        await Task.CompletedTask;

        var cardCurrencyTask = Task.Run(async () =>
        {
            await BindForeignCurrencyRate();
        });
    }


    public async Task BindForeignCurrencyRate()
    {

        if (CardCurrency?.IsForeignCurrency == false) return;

        CardRequest.IssueDetailsModel.Card.IsAgreedToCurrencyRate = IsPrepaidCard ? false : true; //We can assign !IsPrepaid. but we did the below code for understanding
        CardRequest.IssueDetailsModel.Card.IsForeignCurrencyCard = CardCurrency?.IsForeignCurrency ?? false;
        CurrencyRate.Loading();
        StateHasChanged();
        var currencyRateResponse = await CurrencyAppService.ValidateCurrencyRate(new()
        {
            CivilId = CivilId!,
            ForeignCurrencyCode = CardCurrency!.CurrencyIsoCode
        });


        if (!currencyRateResponse.IsSuccess)
        {
            CurrencyRate.Error(new(GlobalResources.UnableToCalculateCurrencyRate));
            EditContextRequest.AddAndNotifyFieldError(validationMessage!, () => CardRequest.IssueDetailsModel.Card.IsAgreedToCurrencyRate!, GlobalResources.UnableToCalculateCurrencyRate);
        }
        else
        {
            CurrencyRate.SetData(currencyRateResponse.Data);
        }
        StateHasChanged();
    }
    private async Task LoadPromotions()
    {
        int productId = CardRequest.IssueDetailsModel.Card.ProductId;

        if (CardRequest.IssueDetailsModel.CoBrand is not null)
            productId = CardRequest.IssueDetailsModel.CoBrand!.Company.CardType;


        CardPromotions.Loading();
        var promotionResponse = await PromotionsAppService.GetActivePromotionsByAccountNumber(new()
        {
            AccountNumber = CardRequest.IssueDetailsModel.Card.DebitAccountNumber,
            CivilId = CardRequest.Customer.CivilId,
            ProductId = productId,
            Collateral = collateral
        });

        if (promotionResponse.IsSuccess)
        {
            if (promotionResponse.Data.AnyWithNull())
                CardRequest.PromotionModel ??= new();

            CardPromotions.SetData(promotionResponse?.Data!);
        }
        else
            CardPromotions.Error(new(promotionResponse.Message));

        StateHasChanged();
    }
    private async Task LoadBillingAddress()
    {
        var billingAddress = await AddressService.GetRecentBillingAddress(civilId: CivilId!);
        await GetAreaCodes();
        await BindBillingAddress(billingAddress);

        if (SelectedAddressType == AddressType.FullAddress)
            OnAddressTypeChange();

        await BindEditContext(FormSteps.BillingAddress);

        async Task BindBillingAddress(ApiResponseModel<BillingAddressModel> response)
        {

            BillingAddressModel? recentBillingAddress = response.Data;

            var customerAddress = GenericCustomerProfileDto?.CustomerAddresses![0];

            //taking customer profile address if there is no recent billing address
            if (recentBillingAddress is null && customerAddress is not null)
            {
                recentBillingAddress = new()
                {
                    Block = customerAddress.BlockNumber,
                    StreetNo_NM = customerAddress.Street,
                    House = customerAddress.House,
                    AreaId = customerAddress.RegionId
                };


                if (int.TryParse(customerAddress.ZipCode, out int _zip))
                    recentBillingAddress.PostalCode = _zip;

                if (int.TryParse(customerAddress.PostBoxNumber, out int _postBoxNo))
                    recentBillingAddress.PostOfficeBoxNumber = _postBoxNo;

                if (long.TryParse(customerAddress.PhoneNumber2, out long _workPhone))
                    recentBillingAddress.WorkPhone = _workPhone;

                if (long.TryParse(customerAddress.PhoneNumber1, out long _homePhone))
                    recentBillingAddress.HomePhone = _homePhone;

                if (recentBillingAddress!.PostOfficeBoxNumber == 0)
                {
                    recentBillingAddress.Street = $"Blk {recentBillingAddress.Block} st {recentBillingAddress.StreetNo_NM} Jda {recentBillingAddress.Jada} House {recentBillingAddress.House}";
                    SelectedAddressType = AddressType.FullAddress;
                }

            }
            else
            {
                if (recentBillingAddress!.PostOfficeBoxNumber == 0)
                {
                    try
                    {
                        string street = recentBillingAddress.Street;

                        int blockIndex = street.IndexOf("Blk");
                        int streetIndex = street.IndexOf("st");
                        int jdaIndex = street.IndexOf("Jda");
                        int houseIndex = street.IndexOf("House");

                        recentBillingAddress.Block = street.Substring(blockIndex += 3, streetIndex - 3).Trim();
                        recentBillingAddress.StreetNo_NM = street.Substring(streetIndex += 2, jdaIndex - streetIndex).Trim();
                        recentBillingAddress.Jada = street.Substring(jdaIndex += 3, houseIndex - jdaIndex).Trim();
                        recentBillingAddress.House = street[(houseIndex += 5)..].Trim();

                    }
                    catch (Exception)
                    {
                        Notification.Failure("Unable to retrieve data from full address");
                    }
                    SelectedAddressType = AddressType.FullAddress;
                }

            }


            //Taking mobile number always from integration service phenix 
            if (long.TryParse(customerAddress?.PhoneNumber3, out long _mobile))
                recentBillingAddress!.Mobile = _mobile;

            CardRequest.BillingAddressModel = new()
            {
                Street = recentBillingAddress!.Street,
                HomePhone = recentBillingAddress.HomePhone,
                WorkPhone = recentBillingAddress.WorkPhone,
                City = recentBillingAddress.City,
                FaxReference = recentBillingAddress.FaxReference,
                PostOfficeBoxNumber = recentBillingAddress.PostOfficeBoxNumber,
                PostalCode = recentBillingAddress.PostalCode,
                Mobile = recentBillingAddress.Mobile,
                Jada = recentBillingAddress.Jada,
                StreetNo_NM = recentBillingAddress.StreetNo_NM,
                Block = recentBillingAddress.Block,
                House = recentBillingAddress.House,
                AreaId = recentBillingAddress.AreaId
            };


            if (int.TryParse(CardRequest.BillingAddressModel.AreaId, out int _areaId))
                CardRequest.BillingAddressModel.City = AreaCodes.FirstOrDefault(ac => ac.AreaId == _areaId)?.AreaName ?? string.Empty;

            await Task.CompletedTask;
        }
    }

    #endregion
    //-------------------------------------------------------- Actions --------------------------------------------------------------
    #region Actions
    private async Task OnChangeRequiredLimit()
    {
        if (CardDefinition?.Eligibility?.ProductType != ProductTypes.ChargeCard)
            return;

        await ValidateRequiredLimit();

        EditContextRequest.NotifyValidationStateChanged();

    }
    bool IsHavingSufficientLimit { get; set; } = true;
    bool IsEligibleForReplaceCard
    {
        get
        {
            if (IsPrepaidCard)//|| IsChargeCard)
                return false;

            if (IsTayseerCard && collateral is Collateral.AGAINST_SALARY && CardRequest.Customer.Salary < ConfigurationBase.MinimumSalaryForCardTransfer)
                return false;

            return true;
        }
    }
    async Task ValidateRequiredLimit()
    {
        using IssueDetailsModel model = CardRequest.IssueDetailsModel;

        //clearing all error message on required limit field to re validate
        validationMessage?.Clear(() => model.Card.RequiredLimit);
        EditContextRequest.NotifyValidationStateChanged();

        if (!await ValidateLimitInput())
            return;

        if (IsExceptionCase)
            return;

        StateHasChanged();

        #region  Local Methods
        async Task<bool> ValidateLimitInput()
        {

            if (CardDefinition!.Eligibility!.ProductType is ProductTypes.PrePaid)
                return true;

            bool isNotInRange = model.Card.RequiredLimit < CardDefinition.MinLimit || model.Card.RequiredLimit > CardDefinition.MaxLimit;
            bool isNotRounded = model.Card.RequiredLimit % 10 != 0;

            string limitMessage = $" between card limit range minimum {CardDefinition.MinLimit} to maximum {CardDefinition.MaxLimit}";
            StringBuilder message = new();

            if (model.Card.RequiredLimit <= 0 || isNotInRange)
                message.AppendLine($"You should enter value, {limitMessage}");

            //if (isNotRounded)
            //    message.AppendLine($"You should enter rounded value, {limitMessage}");


            if (IsCorporateCard)
            {
                if (model.Card.RequiredLimit > CorporateProfileData.Data?.RemainingLimit)
                {
                    message.AppendLine($"Your corporate available limit is {CorporateProfileData.Data?.AvailableLimit}, so please enter amount below {CorporateProfileData.Data?.AvailableLimit}");
                }
            }

            if (message.Length > 0)
            {
                EditContextRequest.AddAndNotifyFieldError(validationMessage!, () => model.Card.RequiredLimit!, message.ToString());
                return false;
            }

            return await Task.FromResult(true);
        }





        #endregion
    }

    //TODO: Move to service
    async Task<bool> IsValidDebitAccount(string accountNumber)
    {
        if (SelectedDebitAccount != null && SelectedDebitAccount?.Acct == accountNumber)
            return false;

        validationMessage?.Clear(() => CardRequest.IssueDetailsModel.Card.DebitAccountNumber!);

        SelectedDebitAccount = DebitAccounts.Data?.FirstOrDefault(x => x.Acct == accountNumber);

        if (SelectedDebitAccount is null)
        {
            financialPosition = new();
            return false;
        }

        if (collateral is not (Collateral.AGAINST_MARGIN or Collateral.AGAINST_DEPOSIT))
            return true;


        //Current Account will not accept for margin and deposit collaterals
        string bankAccountCode = CardRequest.IssueDetailsModel.Card.DebitAccountNumber?.Substring(2, 3) ?? "";
        if (Enum.TryParse(typeof(BankAccountType), bankAccountCode, out object? _bankAccountCode) && (BankAccountType)_bankAccountCode == BankAccountType.Current)
        {
            EditContextRequest.AddAndNotifyFieldError(validationMessage!, () => CardRequest.IssueDetailsModel.Card.DebitAccountNumber!, $"cannot use Current Account to issue {collateral.GetDescription()} ");
            return false;
        }

        //Debit Account balance should have more than card minimum limit
        //if (SelectedDebitAccount != null && CardDefinition?.MinLimit > SelectedDebitAccount?.AvailableBalance)
        //{
        //    EditContextRequest.AddAndNotifyFieldError(validationMessageStore!, () => CardRequest.IssueDetailsModel.Card.DebitAccountNumber!, $"cannot use this account due to insufficient balance. we need minimum {CardDefinition?.MinLimit?.FormedMoney(CardCurrency?.CurrencyIsoCode??"")}");
        //    return false;
        //}

        return await Task.FromResult(true);
    }

    private async Task OnChangeSellerId()
    {
        if (CardRequest.Seller.SellerId > 0 && SellerNameData?.Data?.EmpNo == CardRequest.Seller.SellerId?.ToString())
            return;

        SellerNameData.Reset();
        validationMessage?.Clear(EditContextRequest.Field(nameof(CardRequest.Seller.IsConfirmedSellerId)));
        validationMessage?.Clear(EditContextRequest.Field(nameof(CardRequest.Seller.SellerId)));
        CardRequest.Seller.IsConfirmedSellerId = false;
        await VerifySellerId();
    }
    private async Task OnChangeCorporateCivilId()
    {
        CorporateProfileData.Reset();
        validationMessage!.Clear(() => CardRequest.IssueDetailsModel.CorporateProfile!.CorporateCivilId);
        validationMessage!.Clear(() => CardRequest.IssueDetailsModel.Card.DebitAccountNumber!);
        EditContextRequest.NotifyValidationStateChanged();
        await VerifyCorporateCivilId();
    }
    private void OnCardPromotionSelectionChange(string promotionId)
    {
        if (int.TryParse(promotionId, out int _promotionId))
        {
            CardRequest.PromotionModel!.PromotionId = _promotionId;
        }
    }
    private async Task OnDebitAccountChanged(string accountNumber)
    {
        if (!await IsValidDebitAccount(accountNumber))
            return;

        if (collateral is Collateral.AGAINST_SALARY)
            await VerifySalary();

        StateHasChanged();
    }
    private async Task OnCardAccountChanged(string accountNumber)
    {
        if (SelectedCardAccount?.Acct == accountNumber)
            return;

        IsAgreeToCreateMargin = false;

        validationMessage!.Clear(() => CardRequest.IssueDetailsModel.Card.CollateralAccountNumber!);
        SelectedCardAccount = CardAccounts.Data?.FirstOrDefault(x => x.Acct == accountNumber);

        if (collateral == Collateral.AGAINST_SALARY)
            await VerifySalary();



        StateHasChanged();
    }

    private async void OnAddressTypeChange()
    {
        switch (SelectedAddressType)
        {
            case AddressType.POBox:
                IsPOBoxEnabled = true;
                IsFullAddressEnabled = false;
                await OnPOBoxChange();
                break;

            case AddressType.FullAddress:
                IsPOBoxEnabled = false;
                IsFullAddressEnabled = true;
                CardRequest.BillingAddressModel.PostOfficeBoxNumber = 0;
                await OnFullAddressChange();
                break;
        }

        StateHasChanged();
    }
    private async Task OnPOBoxChange()
    {
        CardRequest.BillingAddressModel.Street = "P.O.BOX: " + CardRequest.BillingAddressModel.PostOfficeBoxNumber;
        StateHasChanged();
        await Task.CompletedTask;
    }
    private async Task OnFullAddressChange()
    {
        //clearing error message on street due to modify
        validationMessage?.Clear(EditContextRequest.Field(nameof(CardRequest.BillingAddressModel.Street)));

        CardRequest.BillingAddressModel.Street = "Blk " + CardRequest.BillingAddressModel.Block + " st " + CardRequest.BillingAddressModel.StreetNo_NM + " Jda " + CardRequest.BillingAddressModel.Jada + " House " + CardRequest.BillingAddressModel.House;
        StateHasChanged();
        await Task.CompletedTask;
    }

    private Collateral? lastSelectedCollateral;
    private async Task OnChangeCollateral()
    {
        if (lastSelectedCollateral == collateral) return;

        lastSelectedCollateral = collateral;
        //reloading debit account list
        await ResetChargeCardFields();
        //await GetCustomerAccountList(CivilId!);
        await LoadCollateralAccount(CivilId!);
        //await BindFinancialPositionData();
    }

    private async Task ResetChargeCardFields()
    {
        IsHavingSufficientLimit = true;
        financialPosition = new();
        validationMessage?.Clear(() => CardRequest.IssueDetailsModel.Card.CollateralAccountNumber!);
        validationMessage?.Clear(() => CardRequest.IssueDetailsModel.Card.DebitAccountNumber!);
        validationMessage?.Clear(() => CardRequest.IssueDetailsModel.Card.RequiredLimit!);
        validationMessage?.Clear(() => CardRequest.Customer.Salary!);
        CardRequest.IssueDetailsModel.Card.DebitAccountNumber = "";
        CardRequest.IssueDetailsModel.Card.CollateralAccountNumber = "";
        CardRequest.IssueDetailsModel.Card.RequiredLimit = 0;
        SelectedCardAccount = null;
        SelectedDebitAccount = null;
        EditContextRequest.NotifyValidationStateChanged();
        StateHasChanged();
        await Task.CompletedTask;


        if (collateral is Collateral.AGAINST_SALARY)
            CardRequest.Customer.Salary = CustomerSalary;

        if (collateral is Collateral.EXCEPTION)
            CardRequest.Customer.Salary = 0;

    }

    private async Task OnChangeTransferDebitAccount(string accountNumber)
    {
        SelectedTransferCardAccount = DebitAccounts.Data?.FirstOrDefault(x => x.Acct == accountNumber);
        await IsValidReplaceCard();
        StateHasChanged();
    }



    private async Task OnChangeReplaceCard(CreditCardApplication product)
    {
        if (financialPosition.Data is not null)
        {
            if (product.CardCategoryType is CardCategoryType.Primary)
            {
                Notification.Failure(GlobalResources.PrimaryCardUpgrade);
                return;
            }

            var selectedApp = CreditCardApplicationsFiltered.FirstOrDefault(x => x.ProductId == product.ProductId);


            if (!selectedApp.IsFetchedBalance)
            {
                Notification.Failure("Please wait to load card balance !");
                return;
            }

            financialPosition.Data.SelectedApplication = selectedApp;
            SelectedTransferCardAccounts = new List<CreditCardApplication>() {
                financialPosition.Data.SelectedApplication
            };
            await IsValidReplaceCard();
        }

        StateHasChanged();
        await Task.CompletedTask;
    }

    private async Task GoHome()
    {
        var queryParams = new Dictionary<string, string>
        {
            ["civilId"] = CurrentState.CurrentCivilId?.Encode()!
        };

        NavigateTo("customer-view", queryParams);
        await Task.CompletedTask;

    }
    //-------------------------------------------------------- Lookups --------------------------------------------------------------

    #endregion

    #region Wizard

    private async Task BindExceptionCaseValues()
    {
        if (IsExceptionCase)
        {
            //CardRequest.Customer.Salary =  GenericCustomerProfileDto?.Income + GenericCustomerProfileDto?.OtherIncome;
            CardRequest.Customer.Salary = 0;
            CardRequest.IssueDetailsModel.ActualCollateral = collateral;
            CardRequest.IssueDetailsModel.Collateral = Collateral.EXCEPTION;
            CardRequest.IssueDetailsModel.IsCBKRulesViolated = true;
            ///TODO: Fixed total due
        }



        await Task.CompletedTask;
    }

    private async Task RevertExceptionCaseValues()
    {
        if (IsExceptionCase)
        {
            //CardRequest.Customer.Salary = GenericCustomerProfileDto?.Income;
            CardRequest.Customer.Salary = CustomerSalary;
            CardRequest.IssueDetailsModel.Collateral = CardRequest.IssueDetailsModel.ActualCollateral;
        }

        await Task.CompletedTask;
    }
    private async Task Submit()
    {
        Notification.Processing(new ActionStatus() { Title = "Card Issuance", Message = "New card issuance is in process.." });
        await BindExceptionCaseValues();

        await BindEditContext(FormSteps.Thankyou);




        if (!EditContextRequest.Validate())
        {
            await RevertExceptionCaseValues();
            Notification.Failure("Please check the input fields!");
            return;
        }



        ApiResponseModel<CardIssueResponse> response = null!;

        //if (CardRequest.IssueDetailsModel.Card.ProductId == ConfigurationBase.AlOsraPrimaryCardTypeId)
        //{
        //    response = await CardIssuanceAppService.IssueAlousraCard(CardRequest);
        //    await ProcessResponse(response);
        //    return;
        //}

        if (IsPrepaidCard)
        {
            response = await CardIssuanceAppService.IssuePrepaidCard(new()
            {
                BillingAddress = CardRequest.BillingAddressModel,
                ProductId = CardRequest.IssueDetailsModel.Card.ProductId,
                CoBrand = CardRequest.IssueDetailsModel.CoBrand,
                Customer = CardRequest.Customer,
                DebitAccountNumber = CardRequest.IssueDetailsModel.Card.DebitAccountNumber,
                SellerId = CardRequest.Seller.SellerId,
                IsConfirmedSellerId = CardRequest.Seller.IsConfirmedSellerId,
                PinMailer = CardRequest.PinMailer,
                PromotionModel = CardRequest.PromotionModel,
                Remark = CardRequest.Remark,
                DeliveryOption = CardRequest.IssueDetailsModel.DeliveryOption,
                DeliveryBranchId = CardRequest.IssueDetailsModel.DeliveryBranchId
            });
            await ProcessResponse(response);
            return;
        }

        if (IsCorporateCard)
        {
            response = await CardIssuanceAppService.IssueCorporateCard(new()
            {
                BillingAddress = CardRequest.BillingAddressModel,
                ProductId = CardRequest.IssueDetailsModel.Card.ProductId,
                Customer = CardRequest.Customer,
                DebitAccountNumber = CardRequest.IssueDetailsModel.Card.DebitAccountNumber,
                SellerId = CardRequest.Seller.SellerId,
                IsConfirmedSellerId = CardRequest.Seller.IsConfirmedSellerId,
                PinMailer = CardRequest.PinMailer,
                PromotionModel = CardRequest.PromotionModel,
                Remark = CardRequest.Remark,
                CorporateCivilId = CardRequest.IssueDetailsModel.CorporateProfile!.CorporateCivilId,
                RequiredLimit = CardRequest.IssueDetailsModel.Card.RequiredLimit,
                DeliveryOption = CardRequest.IssueDetailsModel.DeliveryOption,
                DeliveryBranchId = CardRequest.IssueDetailsModel.DeliveryBranchId,
                Installments = financialPosition.Data != null ? new Installments((int)financialPosition.Data.TotalMurabahaInstallments, (int)financialPosition.Data.TotalRealEstateInstallments) : null
            });
            await ProcessResponse(response);
            return;
        }

        if (IsChargeCard && !IsCorporateCard)
        {
            response = await CardIssuanceAppService.IssueChargeCard(new()
            {
                BillingAddress = CardRequest.BillingAddressModel,
                ProductId = CardRequest.IssueDetailsModel.Card.ProductId,
                CoBrand = CardRequest.IssueDetailsModel.CoBrand,
                Customer = CardRequest.Customer,
                DebitAccountNumber = CardRequest.IssueDetailsModel.Card.DebitAccountNumber,
                SellerId = CardRequest.Seller.SellerId,
                IsConfirmedSellerId = CardRequest.Seller.IsConfirmedSellerId,
                PinMailer = CardRequest.PinMailer,
                PromotionModel = CardRequest.PromotionModel,
                Remark = CardRequest.Remark,
                Collateral = collateral,
                ActualCollateral = CardRequest.IssueDetailsModel.ActualCollateral,
                CollateralAccountNumber = CardRequest.IssueDetailsModel.Card.CollateralAccountNumber,
                RequiredLimit = CardRequest.IssueDetailsModel.Card.RequiredLimit,
                MaxLimit = CardRequest.IssueDetailsModel.Card.MaxLimit,
                TotalFixedDuties = financialPosition.Data?.TotalFixedDuties ?? 0,
                DeliveryOption = CardRequest.IssueDetailsModel.DeliveryOption,
                DeliveryBranchId = CardRequest.IssueDetailsModel.DeliveryBranchId,
                Installments = financialPosition.Data != null ? new Installments((int)financialPosition.Data.TotalMurabahaInstallments, (int)financialPosition.Data.TotalRealEstateInstallments) : null
            });
            await ProcessResponse(response);
            return;
        }

        if (IsTayseerCard)
        {
            response = await CardIssuanceAppService.IssueTayseerCard(new()
            {
                BillingAddress = CardRequest.BillingAddressModel,
                ProductId = CardRequest.IssueDetailsModel.Card.ProductId,
                Customer = CardRequest.Customer,
                DebitAccountNumber = CardRequest.IssueDetailsModel.Card.DebitAccountNumber,
                SellerId = CardRequest.Seller.SellerId,
                IsConfirmedSellerId = CardRequest.Seller.IsConfirmedSellerId,
                PinMailer = CardRequest.PinMailer,
                PromotionModel = CardRequest.PromotionModel,
                Remark = CardRequest.Remark,
                Collateral = collateral,
                ActualCollateral = CardRequest.IssueDetailsModel.ActualCollateral,
                CollateralAccountNumber = CardRequest.IssueDetailsModel.Card.CollateralAccountNumber,
                RequiredLimit = CardRequest.IssueDetailsModel.Card.RequiredLimit,
                T3MaxLimit = CardRequest.IssueDetailsModel.Card.T3MaxLimit,
                T12MaxLimit = CardRequest.IssueDetailsModel.Card.T12MaxLimit,
                TotalFixedDuties = financialPosition.Data?.TotalFixedDuties ?? 0,
                IsCBKRulesViolated = CardRequest.IssueDetailsModel.IsCBKRulesViolated,
                ReplaceCard = CardToReplace != null ? new ReplaceCard()
                {
                    CardNo = CardToReplace.CreditCardNumber,
                    AccountNumber = SelectedTransferCardAccount?.Acct,
                    FdAcctNo = CardToReplace.AccountNumber,
                    DebitMarginAccountNumber = CardToReplace.MarginAccount,
                    DebitMarginAmount = CardToReplace.CardLimit,
                    HoldId = CardToReplace.HoldId,
                    HoldAmount = CardToReplace.DepositAmount
                } : null,
                DeliveryOption = CardRequest.IssueDetailsModel.DeliveryOption,
                DeliveryBranchId = CardRequest.IssueDetailsModel.DeliveryBranchId,
                Installments = financialPosition.Data!=null ? new Installments((int)financialPosition.Data.TotalMurabahaInstallments, (int)financialPosition.Data.TotalRealEstateInstallments) : null 
            });
            await ProcessResponse(response);
            return;
        }

        async Task ProcessResponse(ApiResponseModel<CardIssueResponse> response)
        {
            if (response.IsSuccessWithData)
            {
                Notification.Success("New card request has been created successfully.");
                cardIssueResponse = response.Data;



                await OnStepChange(null);
            }
            else
            {
                await RevertExceptionCaseValues();
                var errors = response.ValidationErrors != null ? string.Join(",", response.ValidationErrors.Select(x => x.Error)?.ToArray() ?? Array.Empty<string>()) : "";
                Notification.Failure($"{response.Message} {errors}");
            }

        }
    }

    public CardIssueResponse? cardIssueResponse { get; set; }
    enum FormSteps
    {
        [Description("Card Selection")]
        CardSelection,
        [Description("Issue Detail")]
        IssueDetail,
        [Description("Financial Position")]
        FinancialPosition,
        [Description("Choose Seller")]
        Seller,
        [Description("Promotions")]
        Promotions,
        [Description("Billing Address")]
        BillingAddress,
        [Description("Confirmation")]
        Review,
        [Description("Thank you!")]
        Thankyou
    }

    //int CurrentStepIndex = 0;
    FormSteps CurrentStep { get; set; } = FormSteps.CardSelection;


    Dictionary<FormSteps, bool> StepStatus = new()
    {
        [FormSteps.CardSelection] = true,
        [FormSteps.IssueDetail] = true,
        [FormSteps.Seller] = true,
        [FormSteps.FinancialPosition] = true,
        [FormSteps.Promotions] = true,
        [FormSteps.BillingAddress] = true,
        [FormSteps.Review] = true,
        [FormSteps.Thankyou] = true
    };
    async Task UnBindFormEditContext()
    {
        if (EditContextRequest == null) return;
        EditContextRequest.OnValidationRequested -= (s, e) => validationMessage?.Clear();
        EditContextRequest.OnFieldChanged -= (s, e) => validationMessage?.Clear(e.FieldIdentifier);
        await Task.CompletedTask;
    }

    /// <summary>
    /// Binding EditContext and reset the message store for selected step model
    /// </summary>
    /// <param name="nextStep">Next Wizard Step</param>
    /// <returns>Next Wizard Step {StepIndex}</returns>
    async Task<FormSteps> BindEditContext(FormSteps nextStep = FormSteps.IssueDetail)
    {
        object? EditContextModel = nextStep switch
        {
            FormSteps.IssueDetail => CardRequest.IssueDetailsModel,
            FormSteps.Seller => CardRequest.Seller,
            FormSteps.FinancialPosition => CardRequest.IssueDetailsModel,
            FormSteps.Promotions => CardRequest.PromotionModel ?? new(),
            FormSteps.BillingAddress => CardRequest.BillingAddressModel,
            FormSteps.Thankyou => CardRequest,
            _ => CardRequest
        };

        EditContextRequest = new EditContext(EditContextModel!);
        validationMessage = new(EditContextRequest);
        //EditContextRequest.OnValidationRequested += (s, e) => validationMessageStore?.Clear();
        EditContextRequest.OnFieldChanged += (s, e) =>
        {
            validationMessage?.Clear(e.FieldIdentifier);
        };
        await Task.CompletedTask;
        return nextStep;
    }

    async Task OnStepChange(WizardStepChangeEventArgs? args)
    {




        var nextStep = CurrentStep switch
        {
            FormSteps.CardSelection => CardDefinition is not null ? FormSteps.IssueDetail : CurrentStep,
            FormSteps.IssueDetail => await IsValidCardDetail() ? (((IsChargeCard || IsTayseerCard) && IsCorporateCard == false) ? FormSteps.FinancialPosition : FormSteps.Seller) : CurrentStep,
            FormSteps.FinancialPosition => await IsValidFinancialPosition() ? FormSteps.Seller : CurrentStep,
            FormSteps.Seller => await IsValidSellerDetail() ? FormSteps.Promotions : CurrentStep,
            FormSteps.Promotions => await IsValidPromotion() ? FormSteps.BillingAddress : CurrentStep,
            FormSteps.BillingAddress => await IsValidBillingAddress() ? FormSteps.Review : CurrentStep,
            FormSteps.Review => cardIssueResponse is not null ? FormSteps.Thankyou : CurrentStep,
            _ => CurrentStep
        };


        Notification?.Loading($"Loading {nextStep.ToString()}");

        if (CurrentStep == nextStep)
            await Stay();
        else
            await MoveTo(nextStep);

        Notification?.Clear();


        if (nextStep is FormSteps.Thankyou)
        {
            await Download(PrintForm.Eform);

            if (collateral is Collateral.AGAINST_DEPOSIT or Collateral.AGAINST_DEPOSIT_USD)
            {
                await Download(PrintForm.DepositVoucher);
            }

            if (collateral is Collateral.AGAINST_MARGIN)
            {
                await Download(PrintForm.DebitVoucher);
            }

            Notification?.Clear();
        }

        async Task Stay()
        {
            StepStatus[CurrentStep] = false;
            if (args is not null)
                args.IsCancelled = true;

            await Task.CompletedTask;
            return;
        }

    }
    async Task MoveTo(FormSteps nextStep)
    {
        StepStatus[CurrentStep] = true;
        //await LoadFormData(nextStep);
        CurrentStep = await BindEditContext(nextStep);
        await LoadFormData(nextStep);
    }

    async Task LoadFormData(FormSteps nextStep)
    {
        if (nextStep is FormSteps.IssueDetail)
            await LoadCardDetail();

        if (nextStep is FormSteps.FinancialPosition && (IsChargeCard || IsTayseerCard))
            await LoadFinancialPositionData();

        if (nextStep is FormSteps.Promotions)
            await LoadPromotions();

        if (nextStep is FormSteps.BillingAddress)
            await LoadBillingAddress();



    }

    async Task WizardNextStep()
    {
        await OnStepChange(null);

    }

    async Task WizardBackStep()
    {
        var nextStep = CurrentStep switch
        {
            FormSteps.CardSelection => CurrentStep,
            FormSteps.IssueDetail => FormSteps.CardSelection,
            FormSteps.FinancialPosition => FormSteps.IssueDetail,
            FormSteps.Seller => IsChargeCard ? FormSteps.FinancialPosition : FormSteps.IssueDetail,
            FormSteps.Promotions => FormSteps.Seller,
            FormSteps.BillingAddress => FormSteps.Promotions,
            FormSteps.Review => FormSteps.BillingAddress,
            FormSteps.Thankyou => FormSteps.Thankyou,
            _ => CurrentStep
        };

        StepStatus[CurrentStep] = true;
        CurrentStep = await BindEditContext(nextStep);
    }
    async Task<bool> IsValidSellerDetail()
    {
        using Seller model = CardRequest.Seller;

        //if the model got changed due to steps changes in non linear flow
        if (EditContextRequest!.Model != model)
            await BindEditContext(CurrentStep);

        //if(model.SellerId is not null && model.IsConfirmedSellerId)
        //await VerifySellerId();

        return EditContextRequest.Validate();
    }
    async Task<bool> IsValidCardDetail()
    {
        using IssueDetailsModel model = CardRequest.IssueDetailsModel;

        //if the model got changed due to steps changes in non linear flow
        if (EditContextRequest!.Model != model)
            await BindEditContext(CurrentStep);

        string accountNumber = model.Card.DebitAccountNumber ?? "";

        bool IsKFHCustomer = DebitAccounts.Data.AnyWithNull();

        string? debitAccountMessage = null!;

        if (IsKFHCustomer)
        {
            if (string.IsNullOrEmpty(accountNumber) || DebitAccounts.Status == DataStatus.Loading)
                debitAccountMessage = GlobalResources.RequiredDebitAccount;
        }
        else if (!(CardDefinition!.Eligibility!.AllowedNonKfh ?? false))
        {
            debitAccountMessage = GlobalResources.RequiredDebitAccount;
        }


        if (financialPosition.Status is DataStatus.Loading)
            debitAccountMessage = GlobalResources.CalculatingFinancialPosition;

        if (debitAccountMessage is not null)
            EditContextRequest.AddAndNotifyFieldError(validationMessage!, () => model.Card.DebitAccountNumber!, debitAccountMessage, true);

        //Validate sufficient balance for foreign currency card
        if (CardCurrency is not null && CardCurrency!.IsForeignCurrency && !string.IsNullOrEmpty(accountNumber))
        {
            var fcResponse = await CurrencyAppService.ValidateSufficientFundForForeignCurrencyCards(model.Card.ProductId, accountNumber);

            if (!fcResponse.IsSuccess)
                EditContextRequest.AddAndNotifyFieldError(validationMessage!, () => model!.Card.DebitAccountNumber!, fcResponse.Message, true);
        }
        if (CardCurrency is { IsForeignCurrency: false })
            model.Card.IsAgreedToCurrencyRate = true;

        //Charger card validations
        await ChargeCardValidations();

        //card delivery selection validation

        if (model.DeliveryOption is DeliveryOption.BRANCH)
        {
            if (model.DeliveryBranchId is null)
            {
                EditContextRequest.AddAndNotifyFieldError(validationMessage!, () => model.DeliveryBranchId!, GlobalResources.PleaseSelectDeliveryBranch, true);
            }
        }


        return EditContextRequest.Validate();

        #region local methods
        async Task ChargeCardValidations()
        {
            //Move all validation into a seperate class to make clean code
            if (IsPrepaidCard || IsCorporateCard)
                return;

            if (model.Collateral == null)
            {
                EditContextRequest.AddAndNotifyFieldError(validationMessage!, () => model.Collateral!, "You must choose collateral", true);
                return;
            }

            validationMessage!.Clear(() => CardRequest.IssueDetailsModel.Card.CollateralAccountNumber!);
            if (collateral == Collateral.AGAINST_MARGIN)
            {
                //User needs to be accept for creating a new margin account, if don't have any
                if (string.IsNullOrEmpty(model.Card.CollateralAccountNumber) && !IsAgreeToCreateMargin)
                {
                    EditContextRequest.AddAndNotifyFieldError(validationMessage!, () => model.Card.CollateralAccountNumber!, "Select I Accept Creating new margin account", true);
                    return;
                }
            }

            if (collateral == Collateral.AGAINST_DEPOSIT)
            {
                //User needs to be accept for creating a new margin account, if don't have any
                if (string.IsNullOrEmpty(model.Card.CollateralAccountNumber))
                {
                    EditContextRequest.AddAndNotifyFieldError(validationMessage!, () => model.Card.CollateralAccountNumber!, "Please select deposit account", true);
                    return;
                }
            }

            if (collateral == Collateral.AGAINST_SALARY)
            {
                await ValidateCINET();
                await VerifySalary();
            }

            if (collateral == Collateral.EXCEPTION)
            {
                if (CardRequest!.Customer.Salary is null)
                {
                    EditContextRequest.AddAndNotifyFieldError(validationMessage!, () => CardRequest!.Customer.Salary!, "Please enter salary amount", true);
                    return;
                }
            }

            await ValidateRequiredLimit();
        }

        async Task ValidateCINET()
        {
            if (CardRequest.Customer.CinetId is null || CardRequest.Customer.CinetId < 0)
                EditContextRequest.AddAndNotifyFieldError(validationMessage!, () => CardRequest.Customer.CinetId!, "please enter valid Cinet ID.", true);

            if (CardRequest.Customer.TotalCinet is null || CardRequest.Customer.TotalCinet < 0)
                EditContextRequest.AddAndNotifyFieldError(validationMessage!, () => CardRequest.Customer.TotalCinet!, "please enter valid Total Cinet.", true);

            await Task.CompletedTask;
        }
        #endregion
    }
    async Task<bool> IsValidFinancialPosition()
    {
        if (financialPosition.Status == DataStatus.Error)
        {
            Notification.Failure(financialPosition!.Exception?.Message ?? "Unable to fetch financial position data!");
            return false;
        }

        if (financialPosition.Status == DataStatus.Loading)
        {
            Notification.Failure("Please wait to calculate financial position!");
            return false;
        }

        await ValidateWithFinancialPosition();

        if (IsHavingSufficientLimit)
            return true;

        if (CardToReplace is null && IsEligibleForReplaceCard && CreditCardApplicationsFiltered.Any())
            NotifyError($"Please select valid card and valid transfer debit account");

        if (CardToReplace is not null)
        {
            if (await IsValidReplaceCard())
                return true;
        }

        if (IsExceptionCase)
            return true;

        return false;

        async Task ValidateWithFinancialPosition()
        {

            if (financialPosition.Data is null)
                return;

            await financialPosition.Data!.Calculate(SelectedCardAccount);

            validationMessage?.Clear(() => CardRequest.FinancialPositionMessage!);
            var (valid, message) = await financialPosition.Data.ValidateRequiredLimit();

            IsHavingSufficientLimit = valid;

            if (valid)
                return;

            if (message.Length > 0 && CurrentStep == FormSteps.FinancialPosition)
                NotifyError(message.ToString());
        }
    }
    async Task<bool> IsValidBillingAddress()
    {
        using BillingAddressModel model = CardRequest.BillingAddressModel;
        //if the model got changed due to steps changes non linear flow
        if (EditContextRequest!.Model != model)
            await BindEditContext(CurrentStep);

        FormatAddress();

        if (string.IsNullOrEmpty(model.City))
        {
            EditContextRequest.AddAndNotifyFieldError(validationMessage!, () => model!.City!, "Please select area", true);
        }
        if (IsHavingArabicCharacters())
        {
            EditContextRequest.AddAndNotifyFieldError(validationMessage!, () => model!.Street!, "Please enter English letters in address fields!", true);
        }



        EditContextRequest.NotifyValidationStateChanged();
        return EditContextRequest.Validate();
    }
    async Task<bool> IsValidPromotion()
    {
        //if the model got changed due to steps changes non linear flow
        await BindEditContext(CurrentStep);

        if (!(CardPromotions?.Data?.Any() ?? false))
            return true;

        return EditContextRequest.Validate();
    }
    #endregion

    #region  Co-Brand
    public DataItem<CompanyLookup> coBrandCardCompany = new();
    async Task BindCoBrandCompanyNames()
    {
        //if (!CardDefinition?.Eligibility?.IsCoBrandPrepaid ?? false) return;

        coBrandCardCompany.Loading();

        var companyResponse = await LookupAppService.GetAllCompanies();
        if (companyResponse.IsSuccess)
        {
            var coBrandCompany = companyResponse.Data!.Where(x => x.CardType == CardDefinition?.ProductId)
                .Select(x => new CompanyLookup(x.CompanyId, x.CompanyName, x.CardType, x.ClubName))
                .DistinctBy(x => x.CompanyId).FirstOrDefault();

            coBrandCardCompany.SetData(coBrandCompany!);
        }
        else
            coBrandCardCompany.Error(new(companyResponse.Message));

        StateHasChanged();
    }

    async Task OnChangeMemberShipId()
    {
        if (CardRequest!.IssueDetailsModel!.CoBrand is null) return;

        var customMemberShipId = new List<MemberShipInfoDto>() { new() { ClubMembershipId = CardRequest.IssueDetailsModel.CoBrand!.MemberShipId!.Value.ToString() } };
        CardRequest.IssueDetailsModel.CoBrand.IsValidMemberShipIdToIssueCard = await IsValidMemberShips(customMemberShipId, coBrandCardCompany?.Data!);

        StateHasChanged();
    }

    async Task ResetOldMemberShipFields()
    {
        IsAllowToDeleteMemberShip = false;
        CardRequest.IssueDetailsModel!.CoBrand!.OldCivilId = null;
        CardRequest.IssueDetailsModel!.CoBrand!.ReasonForDeleteRequest = null;

        await Task.CompletedTask;
    }

    async Task GetMembershipId()
    {
        if (CardRequest!.IssueDetailsModel!.CoBrand is null) return;

        if (coBrandCardCompany?.Data?.CompanyId is null) return;

        var membershipResponse = await MemberShipAppService.GetMemberships(CivilId, coBrandCardCompany?.Data!.CompanyId);

        if (!membershipResponse.IsSuccess)
        {
            coBrandCardCompany?.Error(new(membershipResponse.Message));
            Notification.Failure(membershipResponse.Message);
            return;
        }

        await IsValidMemberShips(membershipResponse?.Data!, coBrandCardCompany?.Data!);
        StateHasChanged();
    }

    async Task<bool> IsValidMemberShips(List<MemberShipInfoDto> memberShips, CompanyLookup selectedCompany)
    {
        await ResetOldMemberShipFields();

        if (!memberShips.Any())
        {
            IsAllowToEditMembershipId = true;
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
            IsAllowToEditMembershipId = true;
            return false;
        }

        CardRequest.IssueDetailsModel.CoBrand!.MemberShipId = _memberShipId;
        CardRequest.IssueDetailsModel.CoBrand!.Company = selectedCompany;

        var memberShipConflicts = await MemberShipAppService.GetMemberShipIdConflicts(CivilId!, selectedCompany.CompanyId!, _memberShipId.ToString());
        if (memberShipConflicts.IsSuccess && memberShipConflicts.Data!.Any())
        {
            CardRequest.IssueDetailsModel.CoBrand!.OldCivilId = memberShipConflicts.Data!.FirstOrDefault()?.CivilId;
            IsAllowToDeleteMemberShip = true;
            Notification.Failure(GlobalResources.DuplicateMemberShipID);
            EditContextRequest.AddAndNotifyFieldError(validationMessage!, () => CardRequest.IssueDetailsModel.CoBrand.MemberShipId, GlobalResources.DuplicateMemberShipID);
            return false;
        }

        return true;
    }
    public bool ShowRequestDeleteMemberShipConfirmation { get; set; }
    async Task RequestDeleteMemberShip()
    {
        var removeMemberShipResponse = await MemberShipAppService.RequestDeleteMemberShip(new()
        {
            CivilId = CardRequest.IssueDetailsModel.CoBrand?.OldCivilId!,
            CompanyId = (int)coBrandCardCompany?.Data?.CompanyId!,
            ClubMembershipId = CardRequest.IssueDetailsModel.CoBrand?.MemberShipId.ToString()!,
            RequestDate = DateTime.Now,
            RequestorReason = CardRequest.IssueDetailsModel.CoBrand?.ReasonForDeleteRequest!,
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

    #region Lookups

    /// <summary>
    /// 
    /// </summary>
    DataItem<ValidateSellerIdResponse> SellerNameData = new();
    DataItem<CorporateProfileDto> CorporateProfileData = new();

    private async Task VerifyCorporateCivilId()
    {
        string corporateCivilId = CardRequest.IssueDetailsModel.CorporateProfile?.CorporateCivilId;
        if (string.IsNullOrEmpty(corporateCivilId))
            return;

        CorporateProfileData.Loading();

        var response = await CorporateProfileAppService.GetProfile(corporateCivilId);
        if (!response.IsSuccess)
        {
            CorporateProfileData.Error(new(response.Message));
            CardRequest.IssueDetailsModel.CorporateProfile = new() { CorporateCivilId = corporateCivilId };
            CardRequest.IssueDetailsModel.Card = new();
            EditContextRequest.AddAndNotifyFieldError(validationMessage!, () => CardRequest.IssueDetailsModel!.CorporateProfile.CorporateCivilId, response.Message, true);
            return;
        }
        //TODO: we can remove this method if we don't allow to choose corporate debit account 
        await GetCustomerAccountList(corporateCivilId);
        SelectedDebitAccount = DebitAccounts.Data!.FirstOrDefault(x => x.Acct == response.Data!.KfhAccountNo);

        CorporateProfileData.SetData(response.Data!);
        CardRequest.IssueDetailsModel.Card.DebitAccountNumber = response.Data!.KfhAccountNo;

        return;
    }

    private async Task VerifySellerId()
    {
        SellerNameData.Loading();

        var seller = await EmployeeService.ValidateSellerId(CardRequest.Seller.SellerId?.ToString("0")!);

        //CardRequest.IsConfirmedSellerId = seller.IsSuccess;

        if (!seller.IsSuccess)
        {
            SellerNameData.Error(new(seller.Message));
            EditContextRequest.AddAndNotifyFieldError(validationMessage!, () => CardRequest.Seller.SellerId!, seller.Message, true);
            return;
        }

        SellerNameData.SetData(seller.Data!);
    }

    private async Task VerifySalary()
    {

        //we will do validation in server side
        //return;

        string accountNumber = CardRequest?.IssueDetailsModel?.Card.DebitAccountNumber!;
        //for Salary verification, if the card currency is USD then take the Salary debit account number
        if (CardCurrency!.CurrencyIsoCode == ConfigurationBase.USDollerCurrency)
        {
            accountNumber = CardRequest?.IssueDetailsModel?.Card.CollateralAccountNumber!;

            if (string.IsNullOrEmpty(accountNumber))
                EditContextRequest.AddAndNotifyFieldError(validationMessage!, () => CardRequest!.IssueDetailsModel.Card.CollateralAccountNumber!, "You must choose salary account", true);
        }

        if (string.IsNullOrEmpty(accountNumber)) return;

        SalaryVerification.Loading();

        validationMessage.Clear(() => CardRequest!.Customer.Salary!);
        var verifySalaryResponse = await AccountAppService.VerifySalary(accountNumber, civilId: CivilId);

        if (!verifySalaryResponse.IsSuccess || !verifySalaryResponse.Data!.Verified)
        {
            string? _salaryErrorMessage = !verifySalaryResponse.IsSuccess ? verifySalaryResponse.Message : verifySalaryResponse.Data?.Description;
            SalaryVerification.Error(new(_salaryErrorMessage));
            EditContextRequest.AddAndNotifyFieldError(validationMessage!, () => CardRequest!.Customer.Salary!, _salaryErrorMessage ?? "", true);
            return;
        }

        SalaryVerification.SetData(verifySalaryResponse.Data.Verified);

        //if the salary (from phenix) greater than the verified salary then we need to take verified one
        if (SalaryVerification.Data && CardRequest.Customer.Salary > verifySalaryResponse.Data.Salary)
        {
            CustomerSalary = verifySalaryResponse.Data.Salary;
            CardRequest.Customer.Salary = CustomerSalary;
        }

    }
    private async Task GetCustomerAccountList(string CivilID, bool canLoadCollateralAccount = true)
    {
        try
        {
            bool canLoadDebitAccount = CardDefinition?.Eligibility?.ProductType is not (ProductTypes.Tayseer or ProductTypes.ChargeCard) || IsCorporateCard || IsUsdChargeCard == false;

            if (canLoadCollateralAccount)
                await LoadCollateralAccount(CivilID);

            if (canLoadDebitAccount)
                await LoadDebitAccounts(CivilID);

        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }


    }

    async Task LoadCollateralAccount(string CivilID)
    {

        //bool isUsdCard = CardCurrency?.CurrencyIsoCode == ConfigurationBase.USDollerCurrency;

        if (collateral is null)
            return;

        CardAccounts.Loading();

        ApiResponseModel<List<AccountDetailsDto>> response = collateral switch
        {
            Collateral.AGAINST_DEPOSIT => IsUsdCard ? await AccountAppService.GetDepositAccountsForUSDCard(CivilID) : await AccountAppService.GetDepositAccounts(CivilID),
            Collateral.AGAINST_SALARY => IsUsdCard ? await AccountAppService.GetSalaryAccountsForUSDCard(CivilID) : new(),
            Collateral.AGAINST_MARGIN => await AccountAppService.GetMarginAccounts(CivilID),
            _ => new()
        };

        if (!response.IsSuccess)
        {
            var errors = response.ValidationErrors != null ? string.Join(",", response.ValidationErrors.Select(x => x.Error).ToArray()) : "";
            Notification.Failure($"Technical error ! {errors}");
            CardAccounts.Error();
            StateHasChanged();
            return;
        }

        CardAccounts.SetData(response.Data ?? new());

        StateHasChanged();
    }

    async Task LoadDebitAccounts(string CivilID)
    {
        ApiResponseModel<List<AccountDetailsDto>> accountResponse = new();

        DebitAccounts.Loading();

        //TODO: Refactor ( create property for IsUSDCard in Eligibility)
        if (IsUsdChargeCard)
        {
            accountResponse = await AccountAppService.GetDebitAccountsForUSDCard(CivilID);
        }
        else
        {
            accountResponse = await AccountAppService.GetDebitAccounts(CivilID);

            string? cardCurrencyCode = CardCurrency?.CurrencyIsoCode;

            if (!string.IsNullOrEmpty(cardCurrencyCode) && cardCurrencyCode != ConfigurationBase.KuwaitCurrency)
                accountResponse.Data = accountResponse?.Data?.Where(account => account.Currency == ConfigurationBase.KuwaitCurrency).ToList();
        }


        if (!accountResponse?.IsSuccess ?? false)
        {
            var errors = accountResponse?.ValidationErrors != null ? string.Join(",", accountResponse.ValidationErrors.Select(x => x.Error).ToArray()) : "";
            Notification.Failure($"Technical error ! {errors}");
            DebitAccounts.Error();
            StateHasChanged();
            return;
        }

        DebitAccounts.SetData(accountResponse?.Data ?? new());
        StateHasChanged();
    }


    private async Task GetCardDetailsByProductID(int productId)
    {
        SelectedCard.Loading();

        //Notification.Loading("Loading card detail");

        var cardDetailsTask = CardIssuanceAppService.GetEligibleCardDetail(productId, CivilId!);
        var cardCurrencyTask = CurrencyAppService.GetCardCurrency(productId);

        await Task.WhenAll(cardDetailsTask, cardCurrencyTask);

        var cardDetails = await cardDetailsTask;
        CardCurrency = await cardCurrencyTask;

        if (!cardDetails.IsSuccess)
        {
            SelectedCard.Error(new(cardDetails.Message));
            Notification.Failure("Unable to find Card Detail");
            return;
        }

        CardDefinition = cardDetails.Data!;
        Notification.Clear();
        SelectedCard.SetData(CardDefinition);
        CardRequest.IssueDetailsModel.Card.ProductId = productId;

    }
    private async Task GetAreaCodes()
    {
        AreaCodes = (await LookupAppService.GetAreaCodes())?.Data!;
    }
    #endregion

    #region Validations
    private bool IsHavingArabicCharacters()
    {
        if (CardRequest.BillingAddressModel.Street.HasArabicCharacters()
        || CardRequest.BillingAddressModel.AddressLine1.HasArabicCharacters()
        || CardRequest.BillingAddressModel.AddressLine2.HasArabicCharacters()) return true;

        return false;

    }
    private void FormatAddress()
    {
        if (!IsFullAddressEnabled)
        {
            CardRequest.BillingAddressModel.Block = "";
            CardRequest.BillingAddressModel.Jada = "";
            CardRequest.BillingAddressModel.House = "";
            CardRequest.BillingAddressModel.StreetNo_NM = "";
        }
        else
        {
            CardRequest.BillingAddressModel.PostOfficeBoxNumber = 0;
        }
    }
    public new async void Dispose()
    {
        GC.SuppressFinalize(this);
        await UnBindFormEditContext();
    }
    #endregion

    #region FinancialPositions
    private async Task LoadFinancialPositionData()
    {
        if (SelectedDebitAccount?.Acct is null || CardDefinition?.Eligibility?.ProductType is ProductTypes.PrePaid || CardDefinition!.Eligibility!.IsCorporate)
            return;

        var accountNumber = SelectedDebitAccount?.Acct;

        financialPosition.Loading();
        StateHasChanged();

        var financialPositionResponse = await AccountAppService.GetFinancialPosition(CivilId!, (Collateral)collateral!, accountNumber!);
        if (!financialPositionResponse.IsSuccess)
        {
            financialPosition.Error(new(financialPositionResponse.Message));
            EditContextRequest.AddAndNotifyFieldError(validationMessage!, () => CardRequest.IssueDetailsModel.Card.DebitAccountNumber!, "Unable to fetch financial position!");
            Notification.Failure("Unable to fetch financial position!");
            return;
        }

        financialPosition.SetData(new(financialPositionResponse.Data, CardRequest, CardCurrency, SelectedCardAccount, SelectedDebitAccount, CardDefinition));

        validationMessage.Clear(() => CardRequest.IssueDetailsModel.Card.DebitAccountNumber!);
        await LoadCardBalances();
        await IsValidFinancialPosition();
        //LoadCardBalances();
    }

    /// <summary>
    /// Binding card balance for financial positions and doing eligible check for card transfer (Upgrade and Downgrade)
    /// </summary>
    /// <param name="cardApp"></param>
    /// <returns></returns>
    private async Task BindCardBalanceStatus(CreditCardApplication cardApp)
    {
        ApiResponseModel<BalanceCardStatusDetails> cardStatusDetail = new();

        if (!string.IsNullOrEmpty(cardApp.CreditCardNumber))
            cardStatusDetail = await CustomerProfileAppService.GetBalanceStatusCardDetail(cardApp.CreditCardNumber);

        cardApp.Message.Clear();

        if (!cardStatusDetail.IsSuccess || string.IsNullOrEmpty(cardApp.CreditCardNumber))
        {
            cardApp.Message.AppendLine("Could not get outstanding balance!");
            cardApp.IsFetchedBalance = true;
            cardApp.ShowBalance = false;
            StateHasChanged();
            return;
        }
        cardApp.ShowBalance = true;

        cardApp.AccountNumber = cardStatusDetail?.Data?.FdrAccountNumber ?? "";
        cardApp.CardBalance = cardStatusDetail?.Data?.Balance ?? 0;
        cardApp.DaysDelinquent = cardStatusDetail?.Data?.DaysDelinquent;

        if (!string.IsNullOrEmpty(cardStatusDetail?.Data?.ExternalStatus ?? "") || cardStatusDetail?.Data?.InternalStatus is ConfigurationBase.InternalInArrearsStatus or ConfigurationBase.InternalInArrearsOverLimitStatus)
            cardApp.Message.AppendLine($" The external status ({cardStatusDetail?.Data?.ExternalStatus}) and the internal status ( {cardStatusDetail?.Data?.InternalStatus} ) not match the database credit card status");

        if (ConfigurationBase.InvalidInternalStatus.Split(',').Any(x => x == cardStatusDetail?.Data?.InternalStatus.ToUpper()))
            cardApp.Message.AppendLine(" Cannot transfer");

        if (cardApp.Message.Length > 0 || cardApp.DaysDelinquent > ConfigurationBase.DelinquentForNotTransfer)
        {
            cardApp.IsFetchedBalance = true;
            StateHasChanged();
            return;
        }

        if (DateTime.TryParseExact(cardStatusDetail.Data.Expiry, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime _expiry) && _expiry != DateTime.MinValue)
        {
            if (DateTime.Today > _expiry)
            {
                cardApp.Message.AppendLine($" Expired Card");
                cardApp.IsFetchedBalance = true;
                StateHasChanged();
                return;
            }
        }

        if (collateral is Collateral.AGAINST_DEPOSIT && cardApp.HoldStatus.ToUpper() != "ACTIVE")
            cardApp.Message.AppendLine($" Hold is not active for Hold ID {cardApp.HoldId}");

        cardApp.IsValid = true;
        cardApp.IsFetchedBalance = true;
        StateHasChanged();
        return;
    }

    DataItem<bool> CardBalanceLoadStatus = new();
    async Task LoadCardBalances()
    {
        if (CardBalanceLoadStatus.Status == DataStatus.Loading || financialPosition.Data is null || financialPosition.Data.Applications is null)
            return;

        _ = Task.Run(async () =>
        {
            CardBalanceLoadStatus.Loading();
            foreach (var cardApp in financialPosition.Data.Applications.CreditCardApplications)
            {
                await BindCardBalanceStatus(cardApp);
            }
            CardBalanceLoadStatus.SetData(true);
            //await financialPosition.Data!.Calculate();
        });
        await Task.CompletedTask;
    }

    List<string> taskList = new();
    Dictionary<string, decimal> accountRb = new();


    private async Task<bool> IsValidReplaceCard()
    {
        decimal newCardLimit = CardDefinition!.MaxLimit ?? 0;

        if (CardToReplace is null) return true;

        CardToReplace.Message.Clear();
        validationMessage?.Clear(() => CardRequest.FinancialPositionMessage!);
        EditContextRequest.NotifyValidationStateChanged();

        if (SelectedTransferCardAccount is null)
        {
            if (IsEligibleForReplaceCard && CreditCardApplicationsFiltered.Any())
                NotifyError($"Please select valid card and valid transfer debit account");

            return false;
        }

        taskList = new();

        if (CardToReplace.CardBalance > newCardLimit)
        {
            NotifyError($"You must pay the outstanding amount {CardToReplace.CardBalance} in order to transfer the card number {DisplayCardNumber(CardToReplace.CreditCardNumber)}");
            return false;
        }

        var accountBalance = DebitAccounts.Data?.FirstOrDefault(x => x.Acct == SelectedTransferCardAccount!.Acct)?.AvailableBalance;


        bool isAllowtoViewBalance = IsAllowTo(Permissions.AccountsBalance.View());

        //TODO what is card Added Amount ?
        if (CardToReplace.CardBalance < newCardLimit)
            taskList.Add("Transfer Card number " + DisplayCardNumber(CardToReplace.CreditCardNumber) + " Bal: " + CardToReplace.CardBalance + " against account " + SelectedTransferCardAccount!.Acct + " Bal: " + (isAllowtoViewBalance ? accountBalance : ""));

        if (collateral == Collateral.AGAINST_DEPOSIT && CardToReplace.HoldStatus.ToUpper() != "ACTIVE")
            NotifyError($"Hold is not active for the Deposit Account {CardToReplace.DepositAccount}");

        financialPosition.Data?.Calculate(SelectedTransferCardAccount);
        //decimal maxLimit = collateral switch
        //{
        //    Collateral.AGAINST_SALARY => CardRequest.Customer.Salary ?? 0,
        //    Collateral.AGAINST_CORPORATE_CARD => CardDefinition.MaxLimit ?? 0,
        //    Collateral.AGAINST_DEPOSIT or Collateral.AGAINST_MARGIN => SelectedCardAccount?.AvailableBalance ?? 0,
        //    Collateral.EXCEPTION => 1,
        //    _ => 0
        //};

        //decimal potentialHighestLimit = collateral switch
        //{
        //    Collateral.AGAINST_DEPOSIT => maxLimit + holdAmount,
        //    Collateral.AGAINST_MARGIN => maxLimit + marginAmount,
        //    _ => maxLimit - financialPosition.Data!.TotalFixedDuties
        //};

        //await ValidateRequiredLimit();
        return await Task.FromResult(true);
    }

    void NotifyError(string errorMessage) => EditContextRequest.AddAndNotifyFieldError(validationMessage!, () => CardRequest.FinancialPositionMessage!, errorMessage);
    async Task UpdateAccountRemainingBalance(string accountNumber, decimal accountBalance, decimal cardBalance)
    {
        decimal _accountBalance = accountRb[accountNumber];

        if (_accountBalance > 0)
            accountRb.Remove(accountNumber);
        else
            _accountBalance = accountBalance;

        _accountBalance -= cardBalance;

        accountRb.Add(accountNumber, _accountBalance);

        await Task.CompletedTask;
    }

    #endregion

    private async Task ViewSupplementaryForm()
    {
        NavigateTo($"/issue-supplementary-card?requestId={cardIssueResponse?.RequestId}");
        await Task.CompletedTask;
    }

    private void OnCancel()
    {
        NavigateTo($"/customer-view?civilId={CurrentState.CurrentCivilId.Encode()}");
    }
    //private CardDetailState cardDetailsState = new();
    //private async Task GetCardDetailAsync()
    //{
    //    cardDetailsState.MyCard.Loading();
    //    var cardDetailsResponse = await CardDetailsAppService.GetCardInfo(cardIssueResponse.RequestId);

    //    if (!cardDetailsResponse.IsSuccess)
    //        cardDetailsState.MyCard.Error(new Exception(cardDetailsResponse.Message));
    //    else
    //    {
    //        cardDetailsState.MyCard.SetData(cardDetailsResponse.Data!);
    //        CardDefinition = (await CardIssuanceAppService.GetEligibleCardDetail(cardDetailsResponse.Data!.CardType, cardDetailsResponse.Data!.CivilId!))?.Data;
    //    }
    //}
    private async Task Download(PrintForm printForm)
    {
        Notification.Loading($"Downloading {printForm}..");

        var eFormResponse = printForm switch
        {
            PrintForm.Eform => await ReportService.GenerateCardIssuanceEForm(cardIssueResponse.RequestId),
            PrintForm.DebitVoucher => await ReportService.GenerateDebitVoucher(new() { RequestId = cardIssueResponse.RequestId }),
            PrintForm.DepositVoucher => await ReportService.GenerateDepositVoucher(new() { RequestId = cardIssueResponse.RequestId }),
            _ => throw new NotImplementedException()
        };

        if (!eFormResponse.IsSuccess)
        {
            Notification.Failure("Unable to download, try again later from card list");
            return;
        }

        Notification.Hide();

        var streamData = new MemoryStream(eFormResponse.Data!.FileBytes!);
        using var streamRef = new DotNetStreamReference(stream: streamData);

        await Js.InvokeVoidAsync("downloadFileFromStream", $"{eFormResponse.Data?.FileName}", streamRef);
    }
    //-------------------------------------------------------- Initialize --------------------------------------------------------

    async Task SearchByCardName()
    {
        if (_editContext != null && _editContext.Validate() && !string.IsNullOrEmpty(_searchProfileInput.SearchText))
            CardsEligibilityMatrixDtoFiltered = CardsEligibilityMatrixDto.Data!.Where(x => x.ProductName!.Contains(_searchProfileInput.SearchText, StringComparison.InvariantCultureIgnoreCase)).ToList();
        else
            FilterCards();

        await Task.CompletedTask;

    }

    protected override async Task OnInitializedAsync()
    {

        Logger.LogInformation("Issue card Init");

        if (CivilId == null)
            NavigateTo("/");

        CurrentState.CurrentCivilId = CivilId;
        _editContext = new EditContext(CardsEligibilityMatrixDto);

        if (!await IsAllowedToIssue())
        {
            Notification.Failure(message: GlobalResources.NotAuthorized);
            return;
        }

        if (await CheckIsAnyPendingRequest())
            NavigateTo("/customer-view?CivilId=" + CivilId.Encode());

        await GetCustomerAvailableCardsForIssuance();
        await BindDropDowns();
        FilterCards();
    }

    async Task<bool> IsAllowedToIssue() =>
        IsAllowTo(Permissions.Prepaid.Issue())
        || IsAllowTo(Permissions.PrepaidFC.Issue())
        || IsAllowTo(Permissions.ChargeCard.Issue())
        || IsAllowTo(Permissions.CoBrand.Issue());

    async Task<bool> CheckIsAnyPendingRequest()
    {
        var pendingRequests = await RequestAppService.GetPendingRequests(CivilId!, null);
        if (pendingRequests.IsSuccessWithData && pendingRequests.Data!.Any())
        {
            Notification.Failure(GlobalResources.CannotIssueNewCard);
            return true;
        }

        return false;
    }


    private async Task BindDropDowns()
    {
        foreach (ProductTypes item in Enum.GetValues(typeof(ProductTypes)))
        {
            if (item is ProductTypes.Supplementary or ProductTypes.Corporate)
                continue;

            ProductTypesList.Add(new ProductType { Text = item.ToString(), Value = item });
        }

        CurrenciesDto = (await LookupAppService.GetCardCurrencies()).Data ?? throw new InvalidOperationException();

        SelectedProductType = ProductTypes.All;
        SelectedCardCurrency = "786";
    }

    // protected override void OnAfterRender(bool firstRender)
    // {
    //     if (firstRender && !string.IsNullOrEmpty(CivilId))
    //     {
    //         _searchProfileInput.CivilId = CivilId;
    //         StateHasChanged();
    //     }
    // }
    //-------------------------------------------------------- Actions --------------------------------------------------------
    #region Actions

    private async Task OnCardSelect(int? productId)
    {
        if (productId is null) return;

        if (SelectedCard.Status == DataStatus.Loading) return;

        await GetCardDetailsByProductID((int)productId);
        await WizardNextStep();
        StateHasChanged();
    }


    #endregion

    #region filters

    private void FilterCards()
    {
        if (CardsEligibilityMatrixDto.Data is null) return;

        CardsEligibilityMatrixDtoFiltered = SelectedProductType switch
        {
            ProductTypes.All => CardsEligibilityMatrixDto.Data!.Where(card => card.CurrencyOriginalId == SelectedCardCurrency).ToList(),
            _ => CardsEligibilityMatrixDto.Data!.Where(card => card.ProductType == SelectedProductType && card.CurrencyOriginalId == SelectedCardCurrency).ToList()
        };

    }

    #endregion
    private async Task ReLoadProfileIfNotLoaded()
    {

        if (CurrentState.CustomerProfile is null)
        {
            State ??= new();

            if (State.GenericCustomerProfile.Status == DataStatus.Loading)
                return;

            State.GenericCustomerProfile.Loading();

            var customerProfile = await CustomerProfileAppService.GetCustomerProfileMinimal(new() { CivilId = CivilId });
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
    private async Task GetCustomerAvailableCardsForIssuance()
    {
        try
        {
            CardsEligibilityMatrixDto.Loading();


            if (CurrentState.CustomerProfile is null)
                await ReLoadProfileIfNotLoaded();

            var currentProfile = CurrentState.CustomerProfile;
            if (currentProfile is null)
            {
                Notification.Failure("Unable to find current customer profile!");
                CardsEligibilityMatrixDto.Error(new("Unable to find current customer profile!"));
                return;
            }

            var getEligibleCardsResponse = await CardIssuanceAppService.GetEligibleCards(new()
            {
                CivilId = CurrentState.CurrentCivilId!,
                CustomerType = currentProfile.CustomerType,
                DateOfBirth = currentProfile.DateOfBirth,
                RimCode = currentProfile.RimCode
            });

            if (!getEligibleCardsResponse.IsSuccess)
            {
                CardsEligibilityMatrixDto.Error(new(getEligibleCardsResponse.Message));
                return;
            }

            CardsEligibilityMatrixDto.SetData(getEligibleCardsResponse.Data!.EligibleCards);
            CardsEligibilityMatrixDtoFiltered = getEligibleCardsResponse.Data!.EligibleCards;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            CardsEligibilityMatrixDto.Error(e);
            throw;
        }

    }
}
