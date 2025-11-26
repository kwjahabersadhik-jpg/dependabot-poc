using CreditCardsSystem.Data.Models;
using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Interfaces;
using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.Access;
using CreditCardsSystem.Domain.Models.Card;
using CreditCardsSystem.Domain.Models.CardIssuance;
using CreditCardsSystem.Domain.Models.Customer;
using CreditCardsSystem.Domain.Models.Promotions;
using CreditCardsSystem.Domain.Models.SupplementaryCard;
using CreditCardsSystem.Utility.Extensions;
using CreditCardsSystem.Utility.Validations;
using CreditCardsSystem.Web.Client.Components;
using CreditCardsSystem.Web.Client.Pages.CustomerProfile;
using Kfh.Aurora.Blazor.Components.UI;
using Kfh.Aurora.Utilities;
using Mapster;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Telerik.Blazor.Components;

namespace CreditCardsSystem.Web.Client.Pages.CardIssuance;

public partial class IssueSupplementaryCardWizard
{
    [Inject] private ICustomerProfileAppService GenericCustomerProfileAppService { get; set; } = null!;
    [Inject] private ICustomerProfileAppService CustomerProfileAppService { get; set; } = null!;
    [Inject] private ICardIssuanceAppService CardIssuanceAppService { get; set; } = null!;
    [Inject] private ICardDetailsAppService CardDetailsAppService { get; set; } = null!;
    [Inject] private ILookupAppService LookupAppService { get; set; } = null!;
    [Inject] private IPromotionsAppService PromotionsAppService { get; set; } = null!;
    [Inject] private IAccountsAppService AccountsAppService { get; set; } = null!;
    [Inject] private IEmployeeAppService EmployeeAppService { get; set; } = null!;
    [Inject] private IRequestAppService RequestAppService { get; set; } = null!;
    [Inject] private IAddressAppService AddressService { get; set; } = null!;

    #region parameters
    [Parameter]
    [SupplyParameterFromQuery]
    public decimal RequestId { get; set; }

    public CardDetailsResponse cardInfo { get; set; }

    [CascadingParameter(Name = "Reload")]
    public EventCallback ReloadCardInfo { get; set; }
    #endregion

    #region Variables
    private int WizardStepIndex { get; set; }
    private bool IsValid { get; set; } = true;
    private bool ConfirmDialogVisible { get; set; }
    private DataStatus ProcessStatus { get; set; }

    private EditCustomerProfile EditProfileRef = null!;

    private OffCanvas EditProfileCanvas = null!;

    private OffCanvas NewSupplementaryCanvas = null!;

    private DataItem<List<AccountDetailsDto>> DebitAccounts { get; set; } = new();
    private SupplementaryCardIssueRequest Model { get; set; } = new();
    private EditContext? EditContext { get; set; } = null!;
    private ValidationMessageStore ValidationMessageStore = null!;

    private DataItem<List<CreditCardPromotionDto>> CardPromotions { get; set; } = new();

    private List<SupplementaryCardDetail> ExistingSupplementries { get; set; } = new();

    //private List<SupplementaryEditModel> NewSupplementaries { get; set; }

    private SupplementaryEditModel? NewSupplementary { get; set; } = new();
    private SupplementaryEditModel? EditableSupplementary { get; set; } = new();


    private Validate Validate { get; set; } = new();

    private CardsCount CardsCount { get; set; } = new();

    private List<Relationship> RelationShipData { get; set; } = new();

    private ProfileDto? CustomerProfileFromFDR { get; set; } = null;
    private GenericCustomerProfileDto? CustomerProfileFromPhenix { get; set; } = null;
    private bool IsFormReady { get; set; } = false;

    private bool CanAddNew { get; set; } = true;

    private bool SaveInProgress { get; set; } = false;
    private bool ShowPromotion { get; set; } = false;

    private decimal AvailableLimit { get; set; }

    private IEnumerable<AddressTypeItem> AddressTypes { get; set; } = new List<AddressTypeItem>()
    {
        new (AddressType.POBox,AddressType.POBox.GetDescription()),
        new (AddressType.FullAddress,AddressType.FullAddress.GetDescription())
    };

    private AddressType SelectedAddressType { get; set; } = AddressType.POBox;
    private List<AreaCodesDto> AreaCodes { get; set; } = new();

    private bool IsPOBoxEnabled { get; set; } = true;
    private bool IsFullAddressEnabled { get; set; } = false;
    #endregion
    private bool IsAllowSupplementary { get; set; }
    private bool IsChargeCard { get; set; }
    private bool IsCorporateCard { get; set; }
    private string ErrorMessage { get; set; } = "Sorry, we cannot add supplementary to this card !";
    private GenericCustomerProfileDto? GenericCustomerProfileDto { get; set; }
    protected override async Task OnParametersSetAsync()
    {
        await LoadCardDetails();
    }

    async Task LoadCardDetails()
    {
        if (!ConfigurationBase.IsEnabledSupplementaryFeature)
        {
            Notification.Failure("This feature is not available now!");
            return;
        }

        if (!IsAllowTo(Permissions.Supplementary.Issue()))
        {
            Notification.Failure(message: GlobalResources.NotAuthorized);
            return;
        }

        var cardInfoResponse = await CardDetailsAppService.GetCardInfo(RequestId);
        if (!cardInfoResponse.IsSuccess)
        {
            Notification.Failure("Invalid card!");
            return;
        }

        cardInfo = cardInfoResponse.Data!;


        //if (await IsHavingAnyPendingRequest())
        //    return;

        if (await IsInvalidPrimaryCardStatus())
            return;


        IsChargeCard = cardInfo.ProductType == ProductTypes.ChargeCard;
        IsCorporateCard = cardInfo.IsCorporateCard;
        IsAllowSupplementary = ConfigurationBase.IsEnabledSupplementaryFeature && !IsCorporateCard && (cardInfo?.CardType == ConfigurationBase.AlOsraPrimaryCardTypeId || cardInfo?.CardType == ConfigurationBase.AlOsraSupplementaryCardTypeId || IsChargeCard);


        if (cardInfo is null || !IsAllowSupplementary)
        {
            Notification.Failure(ErrorMessage);
            StateHasChanged();
            return;
        }

        Model = new()
        {
            PrimaryCardRequestID = RequestId,
            SupplementaryCards = new()
        };

        await Task.WhenAll(LoadExistingSupplementaryCards(), GetRelationshipsData());

        EditContext = new(Model);
        IsFormReady = true;
    }

    async Task<bool> IsInvalidPrimaryCardStatus()
    {
        if (cardInfo.CardStatus is CreditCardStatus.Closed or CreditCardStatus.ChargeOff)
        {
            ErrorMessage = GlobalResources.NoSupplementaryOnClosedCard;
            IsAllowSupplementary = false;
            Notification.Failure(ErrorMessage);
            return true;
        }

        if (cardInfo.CardStatus is not (CreditCardStatus.Pending or CreditCardStatus.Active or CreditCardStatus.Approved))
        {
            ErrorMessage = GlobalResources.PrimaryIsNotActive;
            IsAllowSupplementary = false;
            Notification.Failure(ErrorMessage);
            return true;
        }


        return false;
    }
    async Task<bool> IsHavingAnyPendingRequest()
    {
        var cardType = cardInfo.CardType;
        var pendingRequests = await RequestAppService.GetPendingRequests(cardInfo.CivilId!, null);
        if (pendingRequests.IsSuccessWithData && pendingRequests.Data!.Any(x => x.CardType != cardType))
        {
            Notification.Failure(GlobalResources.CannotIssueNewCard);
            return true;
        }

        return false;
    }

    private void OnCancel()
    {
        NavigateTo($"/customer-view?civilId={cardInfo.CivilId.Encode()}");
    }
    private async Task GetRelationshipsData()
    {
        var response = await LookupAppService.GetRelationships();
        if (response.IsSuccessWithData)
        {
            RelationShipData = response.Data!;
        }
    }

    private async Task LoadExistingSupplementaryCards()
    {
        // Get all Supplementary cards for primary civil ID
        var response = await CardDetailsAppService.GetSupplementaryCardsByRequestId(cardInfo.RequestId);
        if (response.IsSuccessWithData)
        {
            ExistingSupplementries = response.Data ?? new();
            int totalExistingCards = ExistingSupplementries?.Count(x => x.CardStatus != CreditCardStatus.Closed) ?? 0;
            CardsCount = new CardsCount(totalExistingCards);
        }

        await VerifyCounts();
    }
    private async Task LoadPromotions()
    {
        int productId = cardInfo!.CardType;
        _ = Enum.TryParse(typeof(Collateral), cardInfo.Collateral, out object? _collateral);
        //TODO: Co Brand Type
        //if (CardDetail.CoBrand is not null)
        //    productId = CardDetail.Company.CardType;
        CardPromotions.Loading();
        var promotionResponse = await PromotionsAppService.GetActivePromotionsByAccountNumber(new()
        {
            CivilId = NewSupplementary!.CivilId,
            ProductId = productId,
            Collateral = _collateral != null ? (Collateral)_collateral : null
        });

        if (promotionResponse.IsSuccess)
            CardPromotions.SetData(promotionResponse?.Data!);
        else
            CardPromotions.Error(new(promotionResponse.Message));


        ShowPromotion = true;
    }
    private void OnCardPromotionSelectionChange(string promotionId)
    {
        if (int.TryParse(promotionId, out int _promotionId) && NewSupplementary is not null)
        {
            NewSupplementary.PromotionId = _promotionId;
        }
    }

    DataStatus profileLoadStatus { get; set; } = DataStatus.Uninitialized;
    string profileMessage { get; set; } = string.Empty;
    private async Task OnChangeSupplementaryCivilID(SupplementaryEditModel model)
    {

        if (string.IsNullOrEmpty(model.CivilId))
        {
            return;
        }

        //Validate Civil ID
        if (!Validate.IsValidCivilId(model.CivilId))
        {
            Notification.Failure("Civil ID is not valid");
            await ResetNewSupplemenetaryForm();
            return;
        }

        if (profileLoadStatus is not DataStatus.Uninitialized && CustomerProfileFromFDR is not null && CustomerProfileFromFDR.CivilId == model.CivilId)
            return;

        await ResetNewSupplemenetaryForm();


        // Check if supplementary civil ID is equal to primary civil ID
        if (model.CivilId == cardInfo!.CivilId)
        {
            Notification.Failure("Supplementary and primary Civil ID cannot be the same");
            //   "Supplementary  Cards is not for the Primary Civil ID   البطاقات الإضافية ليست للرقم المدني الأساسي";
            return;
        }

        CustomerProfileFromFDR = null;
        profileLoadStatus = DataStatus.Loading;

        var FDRResult = await CustomerProfileAppService.GetCustomerProfileFromFdRlocalDb(model!.CivilId);
        var PhxResult = await GenericCustomerProfileAppService.GetCustomerProfileMinimal(new() { CivilId = model!.CivilId });

        if (FDRResult is { IsSuccess: false, Data: null } | FDRResult is { IsSuccess: true, Data: null })
        {
            bool notInPhenix = (PhxResult is { IsSuccess: false, Data: null });

            profileMessage = notInPhenix ? "Profile not found in Phenix" : "Profile not found please create first!";
            Notification.Failure(profileMessage);
            profileLoadStatus = DataStatus.Error;

            if (notInPhenix)
                return;

            CustomerProfileFromPhenix = PhxResult.Data;
            await EditProfileCanvas.ToggleAsync();
            return;
        }

        CustomerProfileFromFDR = FDRResult.Data;
        profileLoadStatus = DataStatus.Success;


        if (isValidAge() == false)
            return;

        model.Mobile = Convert.ToInt64(PhxResult?.Data?.MobileNumber);
        model.HolderName = CustomerProfileFromFDR?.HolderName;

        await LoadPromotions();
    }
    async Task ResetNewSupplemenetaryForm()
    {
        NewSupplementary ??= new();
        NewSupplementary.RelationId = 0;
        NewSupplementary.Mobile = null;
        NewSupplementary.SpendingLimit = 0;
    }
    bool isValidAge()
    {
        int customerAge = DateTime.Now.Year - CustomerProfileFromFDR!.Birth.Year;
        if (customerAge < ConfigurationBase.SupplementaryCardHolderAge)
        {
            Notification.Failure($"Customer is just {customerAge} yrs old. he is too young to receive supplementary card!. he must complete {ConfigurationBase.SupplementaryCardHolderAge} yrs");
            return false;
        }

        return true;
    }

    async Task WizardNextStep()
    {
        await OnStepChange(null);

    }
    async Task OnStepChange(WizardStepChangeEventArgs? args)
    {

        var nextStep = WizardStepIndex + 1;
        bool IsSupplementaryStep = (IsHavingSupplementaryCards ? 1 : 0) == WizardStepIndex;
        bool IsSellerStep = (IsHavingSupplementaryCards ? 2 : 1) == WizardStepIndex;

        if (IsSupplementaryStep && Model.SupplementaryCards.Count == 0)
        {
            Notification.Failure("Unable to continue, please add supplementary cards!");

            await OpenNewForm();

            if (args is not null)
                args.IsCancelled = true;

            return;
        }

        if (IsSellerStep)
        {
            ValidationMessageStore?.Clear();

            if (Model.SellerId is null || !Model.IsConfirmedSellerId)
            {
                if (args is not null)
                    args.IsCancelled = true;

                return;
            }
        }


        WizardStepIndex = nextStep;

        await Task.CompletedTask;
    }

    public TelerikGrid<SupplementaryEditModel> newItemsGridRef { get; set; } = null!;
    private async Task OpenNewForm()
    {
        NewSupplementary = new();
        profileLoadStatus = DataStatus.Uninitialized;
        await NewSupplementaryCanvas.ToggleAsync();
        StateHasChanged();
        await Task.CompletedTask;
    }
    private async Task AddToList()
    {
        SaveInProgress = true;
        var result = NewSupplementary?.Id is not null ? await UpdateItem() : await ConfirmAdd();
        SaveInProgress = false;

        if (!result)
            return;

        await NewSupplementaryCanvas.ToggleAsync();
        newItemsGridRef.Rebind();
    }
    private async Task CancelToAdd()
    {
        await NewSupplementaryCanvas.ToggleAsync();
        NewSupplementary = new();
        await Task.CompletedTask;
    }
    private async Task<bool> ValidateBeforeAdd()
    {
        if (!EditContext!.Validate())
            return false;

        if (!await IsValidToAddSupplementary())
            return false;


        return true;
        //await ConfirmToAdd(customerProfileFromFDR.Data!, customerProfileFromPhx.Data!);
    }
    private async Task<bool> ConfirmAdd()
    {
        if (!await ValidateBeforeAdd())
            return false;

        ShowPromotion = false;
        Model.SupplementaryCards?.Add(new()
        {
            Id = Guid.NewGuid(),
            FirstName = CustomerProfileFromFDR!.FirstName,
            MiddleName = CustomerProfileFromFDR.MiddleName,
            LastName = CustomerProfileFromFDR.LastName,
            HolderName = CustomerProfileFromFDR.HolderName,
            SpendingLimit = NewSupplementary!.SpendingLimit,
            CivilId = NewSupplementary!.CivilId,
            Mobile = NewSupplementary.Mobile,
            RelationName = RelationShipData.FirstOrDefault(x => x.Id == NewSupplementary.RelationId)!.NameEn,
            RelationId = NewSupplementary.RelationId,
            PromotionId = NewSupplementary.PromotionId,
            Remarks = NewSupplementary.Remarks,
        });


        await VerifyCounts();

        return await Task.FromResult(true);
    }
    async Task Edit(Guid? id)
    {
        EditableSupplementary = Model.SupplementaryCards.ToList().Find(x => x.Id == id);

        if (EditableSupplementary is null)
            return;

        NewSupplementary = EditableSupplementary.Adapt<SupplementaryEditModel>();

        EditContext = new(NewSupplementary);
        await NewSupplementaryCanvas.ToggleAsync();
        await LoadPromotions();
    }
    private async Task<bool> UpdateItem()
    {
        if (EditableSupplementary is null || NewSupplementary is null)
            return false;

        if (EditableSupplementary.CivilId != NewSupplementary.CivilId || EditableSupplementary.SpendingLimit != NewSupplementary.SpendingLimit)
        {
            if (!await ValidateBeforeAdd())
                return false;
        }

        Model.SupplementaryCards!.Remove(EditableSupplementary);
        NewSupplementary.RelationName = RelationShipData.FirstOrDefault(x => x.Id == NewSupplementary.RelationId)!.NameEn;
        Model.SupplementaryCards!.Add(NewSupplementary);

        return await Task.FromResult(true);
    }
    private async Task DeleteItem(Guid? id)
    {
        var removableEntry = Model.SupplementaryCards.Find(x => x.Id == id);
        if (removableEntry is null)
            return;

        Model.SupplementaryCards!.Remove(removableEntry);
        await VerifyCounts();
        newItemsGridRef.Rebind();
        await Task.CompletedTask;
    }

    DataItem<ApiResponseModel<SupplementaryCardIssueResponse>> supplementaryResponse = new();
    private async Task Submit()
    {

        EditContext = new(Model);
        if (!EditContext.Validate() || Model.SupplementaryCards.Count == 0)
        {
            EditContext.NotifyValidationStateChanged();
            return;
        }

        Notification.Processing(new ActionStatus() { Title = "Supplementary card", Message = "New supplementary card issuance is in process.." });
        supplementaryResponse.Loading();
        var response = await CardIssuanceAppService.IssueSupplementaryCards(Model);
        supplementaryResponse.SetData(response);
        Notification.Hide();

        if (!response.IsSuccess)
        {
            Notification.Failure(response.Message);
            return;
        }



        if (response.Data?.FailedCards.Count != 0)
        {
            response.Data!.FailedCards.ForEach(scard =>
            {
                Notification.Show(AlertType.Info, $"Cannot able to create supplementary for this civilId {scard.Message}, Error :{scard.ValidationErrors?.Select(x => x.Error)}");
            });

            if (response.Data?.FailedCards.Count == Model.SupplementaryCards.Count)
            {
                Notification.Failure(response.Message);
                return;
            }
        }

        if (supplementaryResponse.Status != DataStatus.Success)
        {
            Notification.Failure(supplementaryResponse?.Exception?.Message ?? "");
            return;
        }
        else
        {
            WizardStepIndex++;
        }

        StateHasChanged();
    }

    #region Validations
    private async Task VerifyCounts()
    {
        await SetAvailableLimit();

        CardsCount.New = Model.SupplementaryCards!.Count;
        CanAddNew = AvailableLimit > 0 && CardsCount.Remaining > 0;
        if (cardInfo.CardType == ConfigurationBase.AlOsraPrimaryCardTypeId)
            CanAddNew = CardsCount.Remaining > 0;

        if (!CanAddNew)
            Notification.Show(AlertType.Warning, "You have reached maximum number of supplementary for this card !");

        var cardDefinition = (await CardDetailsAppService.GetCardDefinitionExtensionsByProductId(cardInfo!.CardType))?.Data;
        if (int.TryParse(cardDefinition!.MaxSupplimentaryCards, out int _maximumPerCardType))
        {
            CardsCount.MaximumIssue = _maximumPerCardType;
        }
        else
        {

        }
        // TODO CHeck if no primary card selected then disable add button
    }
    private async Task SetAvailableLimit()
    {
        decimal existingCardsLimit = ExistingSupplementries.Where(x => x.CardStatus != CreditCardStatus.Closed).Sum(x => x.CardData?.ApprovedLimit ?? 0);

        decimal addedCardsLimit = 0;
        if (EditableSupplementary?.Id is not null)
        {
            addedCardsLimit = Model.SupplementaryCards?.Where(x => x.Id != EditableSupplementary.Id).Sum(x => x.SpendingLimit) ?? 0;
        }
        else
        {
            addedCardsLimit = Model.SupplementaryCards?.Sum(x => x.SpendingLimit) ?? 0;

        }
        AvailableLimit = cardInfo!.ApproveLimit - (existingCardsLimit + addedCardsLimit);
        await Task.CompletedTask;
    }
    private async Task<bool> IsValidToAddSupplementary()
    {

        var supplementaryCivilID = NewSupplementary!.CivilId.Trim().ToString();


        if (isValidAge() == false)
            return false;

        //Validate Civil ID
        if (CardPromotions!.Data?.Any() ?? false)
        {
            if (NewSupplementary.PromotionId is null)
            {
                Notification.Failure("Please select any promotion");
                return false;
            }
        }

        //Validate Civil ID
        if (!Validate.IsValidCivilId(supplementaryCivilID))
        {
            Notification.Failure("Civil ID is not valid");
            return false;
        }

        if (NewSupplementary.Id is null)
        {
            //Check if civil ID already exist in add new supplementary model list
            if (Model.SupplementaryCards?.Any(x => x.CivilId == supplementaryCivilID) ?? false)
            {
                //"Civil ID is already in the Add new Cards List   الرقم المدني موجود في قائمة البطاقات المضافة";
                Notification.Failure("Civil ID is already in the Add new Cards List");
                return false;
            }
        }
        // Check if supplementary civil ID is equal to primary civil ID
        if (supplementaryCivilID == cardInfo!.CivilId)
        {
            Notification.Failure("Supplementary and primary Civil ID cannot be the same");
            //   "Supplementary  Cards is not for the Primary Civil ID   البطاقات الإضافية ليست للرقم المدني الأساسي";
            return false;
        }

        // Check If new supplementary civil ID is exist and not closed or lost --> not checking status is active because if its temporary stopped you still can not issue new card
        if (ExistingSupplementries.Any(card => card.CivilId == supplementaryCivilID && (card.CardStatus != CreditCardStatus.Closed && card.CardStatus != CreditCardStatus.Lost)))
        {
            Notification.Failure("Civil ID is already in the Existing Cards List");
            //"Civil ID is already in the Existing Cards List    الرقم المدني موجود في قائمة البطاقات";
            return false;
        }

        //TODO: Validate Required Limit against primaryCard approved limit

        await SetAvailableLimit();

        if (cardInfo.CardType is not (ConfigurationBase.AlOsraPrimaryCardTypeId or ConfigurationBase.AlOsraSupplementaryCardTypeId))
        {
            if (NewSupplementary.SpendingLimit <= 0)
            {
                Notification.Failure("Invalid SpendingLimit");
                return false;
            }

            if (AvailableLimit < NewSupplementary.SpendingLimit)
            {
                Notification.Failure($"you are reached maximim limit {cardInfo.ApproveLimit}. Now you are allowed to spend only maximum {AvailableLimit}");
                return false;
            }

        }

        if (!await IsValidDobByCivilId())
        {
            //"Customer is too young for this card type   عمر العميل أقل من الحد المسموح به";
            Notification.Failure("Customer is too young for this card type");
            return false;
        }

        //var customerProfile = await CustomerProfileAppService.GetCustomerProfile(newSupplementary.CivilId);
        //if (!customerProfile.IsSuccess)
        //{
        //    Notification.Failure("This person is not a KFH customer");
        //    return false;
        //}


        var counts = await CardDetailsAppService.GetIssuedSupplementaryCardCounts(supplementaryCivilID, cardInfo.CardType);
        if (counts.sameCard > 0 || counts.alOusra > 0)
        {
            Notification.Failure("Customer already having same card, so we cannot issue multiple cards");
            return false;
        }


        if (counts.total >= CardsCount.MaximumReceive)
        {
            Notification.Failure("Customer has reached maximum cards");
            return false;
        }

        if (NewSupplementary.RelationId == 0)
        {
            Notification.Failure("Relationship is required");
            return false;
        }

        if (!Validate.IsValidMobilePhoneNumber(NewSupplementary.Mobile?.ToString() ?? ""))
        {
            Notification.Failure("Invalid Mobile number");
            return false;
        }



        if (CardPromotions?.Data?.Count != 0 && NewSupplementary!.PromotionId is null)
        {
            Notification.Failure("Please choose any promotion!");
            return false;
        }

        return true;

    }
    private async Task<bool> IsValidDobByCivilId()
    {
        var DOB = Validate.GetDOBFromCivilId(NewSupplementary!.CivilId);
        var suppCardDetail = await CardIssuanceAppService.GetEligibleCardDetail(cardInfo!.CardType, NewSupplementary.CivilId);
        if (!suppCardDetail.IsSuccessWithData)
            return false;

        if (!int.TryParse(suppCardDetail.Data!.Extension!.AgeMinimumLimit, out int _minimumAge))
        {
            var customerAge = DateTime.Today.Year - DOB.Year;
            customerAge += (DateTime.Today.DayOfYear - DOB.DayOfYear) >= 0 ? 0 : -1;
            return customerAge > _minimumAge;
        }

        return true;
    }

    DataItem<ValidateSellerIdResponse> SellerNameData = new();


    private async Task OnChangeSellerId()
    {

        if (Model.SellerId > 0 && SellerNameData?.Data?.EmpNo == Model.SellerId?.ToString())
            return;

        SellerNameData.Reset();
        if (Model.SellerId is not null)
        {
            ValidationMessageStore?.Clear(EditContext!.Field(nameof(Model.IsConfirmedSellerId)));
            ValidationMessageStore?.Clear(EditContext!.Field(nameof(Model.SellerId)));
            Model.IsConfirmedSellerId = false;
            await VerifySellerId();
        }
    }
    private async Task VerifySellerId()
    {
        SellerNameData.Loading();

        var seller = await EmployeeAppService.ValidateSellerId(Model.SellerId?.ToString("0") ?? "");

        //CardRequest.IsConfirmedSellerId = seller.IsSuccess;

        if (!seller.IsSuccess)
        {
            SellerNameData.Error(new(seller.Message));
            EditContext!.AddAndNotifyFieldError(ValidationMessageStore!, () => Model.SellerId!, GlobalResources.InvalidSellerId, true);
            return;
        }

        SellerNameData.SetData(seller.Data!);
    }

    private async Task UpdateProfile()
    {
        ConfirmDialogVisible = false;
        if (await EditProfileRef.SubmitRequest())
        {
            await EditProfileCanvas.ToggleAsync();
            await OnChangeSupplementaryCivilID(NewSupplementary);
        }
    }



    #endregion
}
