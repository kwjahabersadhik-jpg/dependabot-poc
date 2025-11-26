using CreditCardsSystem.Data;
using CreditCardsSystem.Data.Models;
using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Interfaces.Workflow;
using CreditCardsSystem.Domain.Models.CardIssuance;
using CreditCardsSystem.Domain.Models.CardOperation;
using CreditCardsSystem.Domain.Models.Workflow;
using CreditCardsSystem.Domain.Shared.Interfaces;
using CreditCardsSystem.Domain.Shared.Models.Reports;
using CreditCardsSystem.Domain.Shared.Models.RequestActivity;
using Kfh.Aurora.Auth;
using Kfh.Aurora.Logging;
using Microsoft.Extensions.Logging;
using Telerik.DataSource.Extensions;

namespace CreditCardsSystem.Application.CardIssuance.Builders;

[NonController]
public class SupplementaryChargeCardBuilder : ICardBuilder
{
    #region Variables
    private readonly IRequestActivityAppService _requestActivityAppService;
    private readonly IWorkflowCalculationService workflowCalculationService;
    private readonly IAuthManager authManager;
    private readonly FdrDBContext _fdrDBContext;
    private readonly IRequestAppService _requestAppService;
    private readonly ICurrencyAppService _currencyAppService;
    private readonly IMemberShipAppService _memberShipAppService;
    private readonly IEmployeeAppService _employeeAppService;
    private readonly IPromotionsAppService _promotionsAppService;
    private readonly ICustomerProfileAppService _genericCustomerProfileAppService;
    private readonly ICustomerProfileAppService _customerProfileAppService;
    private readonly IAuditLogger<SupplementaryChargeCardBuilder> _auditLogger;
    private readonly ILogger<SupplementaryChargeCardBuilder> _logger;
    private readonly IDeliveryAppService _deliveryAppService;
    private readonly ICardDetailsAppService _cardDetailsAppService;
    private readonly IPreRegisteredPayeeAppService _preRegisteredPayeeAppService;

    private RequestDto newRequest;
    private RequestDto primaryCardRequest;
    private BillingAddressModel BillingAddress { get; set; } = null!;
    private CardDefinitionDto? cardDefinition;
    private CardCurrencyDto cardCurrency;
    private List<ValidationError> validationErrors;
    private SupplementaryCardIssueRequest request;


    public decimal? NewCardRequestId { get; set; }
    private bool IsForeignCurrencyCard => cardCurrency.IsForeignCurrency;
    private SupplementaryEditModel supplementary => request.SupplementaryCards.FirstOrDefault()!;
    private bool IsPrimaryOrNormalCard => request.SupplementaryCards?.Count != 1;
    public int PrimaryProductId { get; private set; }
    private string PrimaryCardHolderName { get; set; } = null!;
    private string PrimaryCivilId { get; set; } = null!;
    private decimal PrimaryCardRequestId { get; set; }
    private Collateral PrimaryCollateral { get; set; }
    private int ProductTypeId { get; set; }

    private string? PrimaryCardParameter { get; set; } = null!;
    public bool IsMinorCustomer { get; private set; }
    #endregion

    public SupplementaryChargeCardBuilder(FdrDBContext fdrDBContext, IOptions<IntegrationOptions> options, IRequestAppService requestAppService, ICurrencyAppService currencyAppService,
        IMemberShipAppService memberShipAppService, IEmployeeAppService employeeAppService, IPromotionsAppService promotionsAppService, ICustomerProfileAppService genericCustomerProfileAppService,
        ICustomerProfileAppService customerProfileAppService,
         IAuditLogger<SupplementaryChargeCardBuilder> auditLogger,
                             ILogger<SupplementaryChargeCardBuilder> logger,
                             IDeliveryAppService deliveryAppService, ICardDetailsAppService cardDetailsAppService, IPreRegisteredPayeeAppService preRegisteredPayeeAppService, IRequestActivityAppService requestActivityAppService, IWorkflowCalculationService workflowCalculationService, IAuthManager authManager)
    {
        _fdrDBContext = fdrDBContext;
        _requestAppService = requestAppService;
        _currencyAppService = currencyAppService;
        _memberShipAppService = memberShipAppService;
        _employeeAppService = employeeAppService;
        _promotionsAppService = promotionsAppService;
        _genericCustomerProfileAppService = genericCustomerProfileAppService;
        _customerProfileAppService = customerProfileAppService;
        this._auditLogger = auditLogger;
        this._logger = logger;
        this._deliveryAppService = deliveryAppService;
        _cardDetailsAppService = cardDetailsAppService;
        _preRegisteredPayeeAppService = preRegisteredPayeeAppService;

        request = new();
        newRequest = new();
        primaryCardRequest = new();
        validationErrors = new();
        _requestActivityAppService = requestActivityAppService;
        this.workflowCalculationService = workflowCalculationService;
        this.authManager = authManager;
    }

    public ICardBuilder WithRequest(BaseCardRequest cardRequest)
    {
        request = cardRequest as SupplementaryCardIssueRequest;
        return this;
    }
    public ICardBuilder CollectPrimaryCardData(decimal primaryCardRequestId)
    {
        PrimaryCardRequestId = primaryCardRequestId;
        BindRequestFromParentRequestData().GetAwaiter();
        return this;
    }
    public async Task Validate()
    {
        //await request.ModelValidationAsync();
        cardDefinition = await _cardDetailsAppService.GetCardWithExtension(PrimaryProductId);
        cardCurrency = await _currencyAppService.GetCardCurrency(PrimaryProductId);

        if ((CreditCardStatus)primaryCardRequest.ReqStatus is CreditCardStatus.Closed or CreditCardStatus.ChargeOff)
            throw new ApiException(message: GlobalResources.NoSupplementaryOnClosedCard);

        //we will not create work flow if the primary card is not approved
        if ((CreditCardStatus)primaryCardRequest.ReqStatus is not (CreditCardStatus.Pending or CreditCardStatus.Active or CreditCardStatus.Approved))
            throw new ApiException(message: GlobalResources.PrimaryIsNotActive);

        if (cardDefinition is null)
            throw new ApiException(message: "Invalid product Id!");

        if (await _requestAppService.HasPendingOrActiveCard(request.SupplementaryCards[0].CivilId, cardDefinition.CardType))
            throw new ApiException(message: "Cannot issue the same card again!");

        //if (IsForeignCurrencyCard)
        //    await ValidateDebitAccount();

        //if (request!.CoBrand is not null)
        //    await CoBrandValidations();

        //uncomment the below validation if we need server side validation
        //await ValidateRequiredLimit(); 

        if (request.DeliveryOption is DeliveryOption.BRANCH)
        {
            if (request.DeliveryBranchId is null)
            {
                throw new ApiException(new() { new(nameof(request.CoBrand.Company.CardType), GlobalResources.PleaseSelectDeliveryBranch) });
            }
        }

        validationErrors.ThrowErrorsIfAny();

        #region local functions
        //async Task CoBrandValidations()
        //{
        //    if (request.CoBrand?.Company.CardType <= 0)
        //        throw new ApiException(new() { new(nameof(request.CoBrand.Company.CardType), "Invalid Co-Brand Product!") });

        //    request.ProductId = request.CoBrand!.Company.CardType;

        //    var memberShipConflicts = await _memberShipAppService.GetMemberShipIdConflicts(request.Customer.CivilId, request.CoBrand.Company.CompanyId, request.CoBrand.MemberShipId.ToString() ?? "");

        //    if (memberShipConflicts.IsSuccess && memberShipConflicts.Data!.Any())
        //        throw new ApiException(new() { new(nameof(request.CoBrand.MemberShipId), GlobalResources.DuplicateMemberShipID) });

        //    await Task.CompletedTask;
        //}

        //async Task ValidateDebitAccount()
        //{
        //    if (string.IsNullOrEmpty(request.DebitAccountNumber))
        //        return;

        //    var validateSufficient = await _currencyAppService.ValidateSufficientFundForForeignCurrencyCards(request.ProductId, request.DebitAccountNumber);
        //    if (!validateSufficient.IsSuccess)
        //        validationErrors.AddRange(validateSufficient.ValidationErrors);

        //    await Task.CompletedTask; return;
        //}
        #endregion
    }
    public async Task Prepare()
    {
        var customerProfile = ((await _genericCustomerProfileAppService.GetDetailedGenericCustomerProfile(new() { CivilId = supplementary.CivilId }))?.Data) ??
           throw new ApiException(message: "invalid customer");

        decimal reqId = await _requestAppService.GenerateNewRequestId(supplementary.CivilId);

        var currentUser = await _employeeAppService.GetCurrentLoggedInUser();
        string cardType = $"{ProductTypes.Supplementary}{ProductTypes.ChargeCard}";

        if (PrimaryProductId is ConfigurationBase.AlOsraPrimaryCardTypeId)
        {
            PrimaryProductId = ConfigurationBase.AlOsraSupplementaryCardTypeId;
            cardType = $"{ProductTypes.PrePaid}";
        }

        newRequest = new RequestDto()
        {
            RequestId = reqId,
            AcctNo = primaryCardRequest.AcctNo,
            CardType = PrimaryProductId,
            SellerId = request.SellerId,
            Remark = request.Remark,
            TellerId = Convert.ToDecimal(authManager.GetUser()?.KfhId),
            RequestedLimit = supplementary.SpendingLimit,
            ApproveLimit = supplementary.SpendingLimit,
            CivilId = supplementary.CivilId,
            Mobile = supplementary.Mobile,

            //Billing Address
            City = BillingAddress.City,
            AddressLine1 = BillingAddress.AddressLine1,
            AddressLine2 = BillingAddress.AddressLine2,
            FaxReference = BillingAddress.FaxReference,
            HomePhone = BillingAddress.HomePhone ?? 0,
            PostOfficeBoxNumber = BillingAddress.PostOfficeBoxNumber,
            PostalCode = BillingAddress.PostalCode ?? 0,
            Street = BillingAddress.Street,
            WorkPhone = BillingAddress.WorkPhone,
            BranchId = await BindCustomerBranchId(customerProfile.EmployeeNumber, newRequest.AcctNo),
            MurInstallments = request.Installments?.MurabahaInstallments,
            ReInstallments = request.Installments?.RealEstateInstallment

        };

        await BindRequestParameters();
        SetMinorValue();

        #region local function
        async Task<int> BindCustomerBranchId(string employeeNumber = "", string? debitAccountNumber = "")
        {
            if (!string.IsNullOrEmpty(debitAccountNumber))
            {
                // if the employeeNumber available then set 0 branch, else set the branch to officer branch
                return !string.IsNullOrEmpty(employeeNumber) ? 0 : Convert.ToInt16(debitAccountNumber[..2]);
            }

            // added by Haitham Salem: for non kfh we will use Active Directory Branch ID (logged in user) - "currentUser",
            // but for BCD branch and call center because there id's are 999, 995 we will use mapping
            var currentUserBranch = ConfigurationBase.CoBrandADPhnxBranchMapping.Split(',')
                .FirstOrDefault(pnxBranch => pnxBranch.Split('@')[0] == currentUser!.Location)?.Split('@')[1];

            _ = int.TryParse(currentUserBranch, out int _currentUserBranch);

            // If this branch is regular branch (not call center or BCD)
            if (_currentUserBranch != 0)
                return _currentUserBranch;

            // Updating the Logic; If location digit of length 3 and customer branch =0 then raise error
            if (currentUser!.Location is not null && currentUser!.Location.Length == 3)
                throw new ApiException(message: "Issuing Co-Brand card: not allowed branch"); //TODO: we need to change this error message. it seems not related to this validation

            _ = int.TryParse(currentUser.Location, out int _currentUserLocation);
            return await Task.FromResult(_currentUserLocation);
        }
        async Task BindRequestParameters()
        {

            //Request Parameters
            newRequest.Parameters = new()
            {
                KFHStaffID = customerProfile.EmployeeNumber,
                CustomerClassCode = customerProfile.RimCode.ToString(),
                SellerGenderCode = (await _employeeAppService.ValidateSellerId(request?.SellerId?.ToString() ?? ""))?.Data?.Gender ?? "",
                Employment = customerProfile.IsRetired ? "1" : "0",
                Collateral = PrimaryCollateral.ToString(),
                Relation = supplementary?.RelationName,
                PrimaryCardCivilId = PrimaryCivilId,
                PrimaryCardNumber = primaryCardRequest.CardNo!,
                PrimaryCardRequestId = PrimaryCardRequestId.ToString().Replace(".0", ""),
                PrimaryCardHolderName = PrimaryCardHolderName,
                KFHCustomer = customerProfile.IsEmployee ? "1" : "0",
                IsSupplementaryOrPrimaryChargeCard = ChargeCardType.S.ToString(),
                CardType = cardType,
                DeliveryOption = request.DeliveryOption.ToString(),
                DeliveryBranchId = request.DeliveryBranchId?.ToString()
            };

            if (PrimaryCollateral is Collateral.AGAINST_MARGIN)
            {
                newRequest.Parameters.MarginAccountNumber = primaryCardRequest.Parameters.MarginAccountNumber;
                newRequest.Parameters.MarginAmount = primaryCardRequest.Parameters.MarginAmount;
                newRequest.Parameters.MarginTransferReferenceNumber = primaryCardRequest.Parameters.MarginTransferReferenceNumber;
            }

            if (PrimaryCollateral is Collateral.AGAINST_DEPOSIT)
            {
                newRequest.Parameters.DepositAccountNumber = primaryCardRequest.Parameters.DepositAccountNumber;
                newRequest.Parameters.DepositNumber = primaryCardRequest.Parameters.DepositNumber;
                newRequest.Parameters.DepositAmount = primaryCardRequest.Parameters.DepositAmount;
                newRequest.Parameters.DepositReferenceNumber = primaryCardRequest.Parameters.DepositReferenceNumber;
            }

            await BindPromotion();

            await Task.CompletedTask;
        }
        async Task BindPromotion()
        {
            if (supplementary?.PromotionId is null)
            {
                newRequest.Parameters.PCTFlag = GetDefaultPCT(newRequest!.Parameters.KFHStaffID);
                return;
            }

            var selectedCardPromotion = await _promotionsAppService.GetPromotionById(new()
            {
                AccountNumber = primaryCardRequest.AcctNo,
                CivilId = supplementary.CivilId,
                ProductId = PrimaryProductId,
                PromotionId = supplementary.PromotionId,
                Collateral = PrimaryCollateral
            });
            if (selectedCardPromotion == null) return;

            selectedCardPromotion.pctFlag ??= GetDefaultPCT(newRequest!.Parameters.KFHStaffID);

            newRequest.Parameters.SetPromotion(selectedCardPromotion);

            await Task.CompletedTask;
        }
        string GetDefaultPCT(string? kfhStaffId)
        {
            string PCT_Default = cardDefinition?.Extension?.PctDefault!;
            string PCT_Default_Staff = cardDefinition?.Extension?.PctDefaultStaff!;

            if (kfhStaffId != "0" && !string.IsNullOrEmpty(PCT_Default_Staff))
                return PCT_Default_Staff;

            return PCT_Default;
        }
        void SetMinorValue()
        {

            DateTime today = DateTime.Today;
            int age = today.Year - customerProfile.BirthDate!.Value.Year;

            if (customerProfile.BirthDate!.Value > today.AddYears(-age))
                age--;

            IsMinorCustomer = age < 21;
        };
        #endregion
    }
    public async Task<decimal> Issue()
    {
        using var transaction = await _fdrDBContext.Database.BeginTransactionAsync();
        try
        {
            await _requestAppService.CreateNewRequest(newRequest);
            await _requestAppService.AddRequestParameters(newRequest.Parameters, newRequest.RequestId);



            if (!string.IsNullOrEmpty(newRequest.Parameters.PromotionName))
                await _promotionsAppService.AddPromotionToBeneficiary(new()
                {
                    ApplicationDate = DateTime.Now,
                    CivilId = newRequest.CivilId,
                    PromotionName = newRequest.Parameters.PromotionName,
                    Remarks = newRequest.Remark,
                    RequestId = newRequest.RequestId
                });

            await transaction.CommitAsync();
            NewCardRequestId = newRequest.RequestId;
            PreregisteredPayee preregisteredPayee = new()
            {
                CivilId = PrimaryCivilId,// newRequest.CivilId,
                CardNo = newRequest.RequestId.ToString(CultureInfo.CurrentCulture),
                Description = "",
                StatusId = 3,
                TypeId = ProductTypeId,
                FullName = supplementary.HolderName
            };
            await _preRegisteredPayeeAppService.AddPreregisteredPayee(preregisteredPayee);

            try
            {
                await _deliveryAppService.RequestDelivery(request.DeliveryOption, NewCardRequestId, request.DeliveryBranchId);
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Unable to request delivery");
            }

        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            throw;
        }


        //setting primary request parameter
        if (string.IsNullOrEmpty(PrimaryCardParameter) || PrimaryCardParameter != "P")
            await _requestAppService.UpdateRequestParameter(PrimaryCardRequestId, "IsSupplementaryOrPrimaryChargeCard", "P");

        return newRequest.RequestId;
    }

    private async Task BindRequestFromParentRequestData()
    {
        primaryCardRequest = (await _requestAppService.GetRequestDetail(PrimaryCardRequestId))?.Data!;
        ProductTypeId = await _cardDetailsAppService.GetPayeeProductType(primaryCardRequest.CardType);
        Profile? primaryCustomer = (await _customerProfileAppService.GetCustomerProfileFromFdRlocalDbByRequestId(primaryCardRequest.RequestId))?.Data;

        PrimaryProductId = primaryCardRequest.CardType;
        PrimaryCardHolderName = primaryCustomer?.HolderName ?? "";
        PrimaryCivilId = primaryCustomer?.CivilId ?? "";
        BillingAddress = primaryCardRequest!.BillingAddress;
        if (Enum.TryParse(primaryCardRequest!.Parameters.Collateral, out Collateral _collateral))
            PrimaryCollateral = _collateral;

        PrimaryCardParameter = primaryCardRequest.Parameters.IsSupplementaryOrPrimaryChargeCard;
    }

    public async Task InitiateWorkFlow()
    {


        bool isNewMarginAccount = string.IsNullOrEmpty(primaryCardRequest.AcctNo);
        var percentage = await workflowCalculationService.GetPercentage(NewCardRequestId ?? 0);
        var maxPercentage = primaryCardRequest.Parameters.MaxLimit;
        if (cardDefinition?.IsTayseer ?? false)
            maxPercentage = cardDefinition!.Installments is 12 ? newRequest.Parameters.T12MaxLimit : newRequest.Parameters.T3MaxLimit;

        RequestActivityDto requestActivity = new()
        {
            RequestId = newRequest.RequestId,
            BranchId = newRequest.BranchId,
            CivilId = request.SupplementaryCards[0].CivilId,
            IssuanceTypeId = (int)IssuanceTypes.CHARGE,
            CfuActivityId = (int)CFUActivity.Card_Request,
            WorkflowVariables = new()  {
            { WorkflowVariables.ProductType, cardDefinition!.ProductType},
            { WorkflowVariables.Description,$"{cardDefinition!.Name} (Supplementary Card) request for {newRequest.CivilId }" },
            { WorkflowVariables.Percentage, percentage},
            { WorkflowVariables.CardType, newRequest.CardType },
            { WorkflowVariables.IsMinorCustomer, IsMinorCustomer },
            { WorkflowVariables.MaxPercentage, maxPercentage??"" },
            { WorkflowVariables.BcdFlag, newRequest.Parameters.BCDFlag??"" },
            { WorkflowVariables.Collateral, newRequest.Parameters.Collateral??"" } }
        };
        if (PrimaryCollateral is Collateral.AGAINST_MARGIN && isNewMarginAccount)
        {
            requestActivity.CfuActivityId = (int)CFUActivity.MARGIN_ACCOUNT_CREATE;
            requestActivity.Details = new()
                    {
                        {ReportingConstants.MARGIN_ACCOUNT_NO,  newRequest.Parameters.MarginAccountNumber!} ,
                        {ReportingConstants.MARGIN_AMOUNT,  newRequest.Parameters.MarginAmount! }
                    };

            requestActivity.WorkflowVariables.AddRange(new Dictionary<string, object>() {

                        {WorkflowVariables.MarginAccountNumber,  newRequest.Parameters.MarginAccountNumber!} ,
                        {WorkflowVariables.MarginAmount,  newRequest.Parameters.MarginAmount! }
                    });
        }
        if (PrimaryCollateral is Collateral.AGAINST_DEPOSIT)
        {
            requestActivity.CfuActivityId = (int)CFUActivity.HOLD_ADD;
            requestActivity.Details = new()
                    {
                        {ReportingConstants.DEPOSIT_ACCOUNT_NO,  newRequest.Parameters.DepositAccountNumber! } ,
                        {ReportingConstants.DEPOSIT_AMOUNT,  newRequest.Parameters.DepositAmount!},
                        {ReportingConstants.DEPOSIT_NUMBER,  newRequest.Parameters.DepositNumber!}
                    };

            requestActivity.WorkflowVariables.AddRange(new Dictionary<string, object>() {
                        {WorkflowVariables.DepositAccountNumber,  newRequest.Parameters.DepositAccountNumber! } ,
                        {WorkflowVariables.DepositAmount,  newRequest.Parameters.DepositAmount!},
                        {WorkflowVariables.DepositNumber,  newRequest.Parameters.DepositNumber!}
                    });
        }
        await _requestActivityAppService.LogRequestActivity(requestActivity!, searchExist: false, isNeedWorkflow: true);
    }
}


