using CreditCardsSystem.Data;
using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Models.CardIssuance;
using CreditCardsSystem.Domain.Models.CardOperation;
using CreditCardsSystem.Domain.Models.Customer;
using CreditCardsSystem.Domain.Models.Workflow;
using CreditCardsSystem.Domain.Shared.Models.RequestActivity;
using Kfh.Aurora.Auth;
using Kfh.Aurora.Logging;
using Kfh.Aurora.Organization;
using Microsoft.Extensions.Logging;

namespace CreditCardsSystem.Application.CardIssuance.Builders;

[NonController]
public class PrepaidCardBuilder : ICardBuilder
{
    #region Variables
    private readonly FdrDBContext _fdrDBContext;
    private readonly IRequestAppService _requestAppService;
    private readonly ICurrencyAppService _currencyAppService;
    private readonly IMemberShipAppService _memberShipAppService;
    private readonly IEmployeeAppService _employeeAppService;
    private readonly IPromotionsAppService _promotionsAppService;
    private readonly ICardDetailsAppService _cardDetailsAppService;
    private readonly ICustomerProfileAppService _genericCustomerProfileAppService;
    private readonly ILogger<PrepaidCardBuilder> _logger;
    private readonly IAuditLogger<PrepaidCardBuilder> _auditLogger;
    private readonly IConfigurationAppService configurationAppService;
    private readonly IDeliveryAppService _deliveryAppService;
    private readonly IRequestActivityAppService _requestActivityAppService;
    private readonly IAuthManager authManager;


    public PrepaidCardRequest request;
    private RequestDto newRequest;
    private CardDefinitionDto? cardDefinition;
    private CardCurrencyDto? cardCurrency;
    private GenericCustomerProfileDto? customerProfile;
    public decimal NewCardRequestId { get; set; }
    private bool IsForeignCurrencyCard => cardCurrency?.IsForeignCurrency ?? false;
    #endregion

    public PrepaidCardBuilder(FdrDBContext fdrDBContext, IRequestAppService requestAppService, ICurrencyAppService currencyAppService,
        IMemberShipAppService memberShipAppService, IEmployeeAppService employeeAppService, IPromotionsAppService promotionsAppService,
        ICardDetailsAppService cardDetailsAppService, ICustomerProfileAppService genericCustomerProfileAppService, ILogger<PrepaidCardBuilder> logger,
        IRequestActivityAppService requestActivityAppService, IAuthManager authManager, IAuditLogger<PrepaidCardBuilder> auditLogger, IConfigurationAppService configurationAppService, IDeliveryAppService deliveryAppService)
    {

        _fdrDBContext = fdrDBContext;
        _requestAppService = requestAppService;
        _currencyAppService = currencyAppService;
        _memberShipAppService = memberShipAppService;
        _employeeAppService = employeeAppService;
        _promotionsAppService = promotionsAppService;
        request = new();
        newRequest = new();
        _cardDetailsAppService = cardDetailsAppService;
        _genericCustomerProfileAppService = genericCustomerProfileAppService;
        _logger = logger;
        _requestActivityAppService = requestActivityAppService;
        this.authManager = authManager;
        _auditLogger = auditLogger;
        this.configurationAppService = configurationAppService;
        _deliveryAppService = deliveryAppService;
    }

    public ICardBuilder WithRequest(BaseCardRequest cardRequest)
    {
        request = (cardRequest as PrepaidCardRequest)!;
        return this;
    }
    public async Task Validate()
    {
        await request.ModelValidationAsync();

        cardDefinition = await _cardDetailsAppService.GetCardWithExtension(request.ProductId);
        cardCurrency = await _currencyAppService.GetCardCurrency(request!.ProductId);

        if (request.ProductId == 0)
            throw new ApiException(message: "Invalid product Id!");

        if (await _requestAppService.HasPendingOrActiveCard(request.Customer.CivilId, request.ProductId))
            throw new ApiException(message: "Cannot issue the same card again!");

        if (IsForeignCurrencyCard)
        {
            var validateSufficient = await _currencyAppService.ValidateSufficientFundForForeignCurrencyCards(request.ProductId, request.DebitAccountNumber);
            if (!validateSufficient.IsSuccess)
                throw new ApiException(message: validateSufficient.Message, errors: validateSufficient.ValidationErrors);
        }

        if (request!.CoBrand is not null)
            await CoBrandValidations();

        async Task CoBrandValidations()
        {
            if (request.CoBrand?.Company.CardType <= 0)
                throw new ApiException(new() { new(nameof(request.CoBrand.Company.CardType), "Invalid Co-Brand Product!") });

            request.ProductId = request.CoBrand!.Company.CardType;

            var memberShipConflicts = await _memberShipAppService.GetMemberShipIdConflicts(request.Customer.CivilId, request.CoBrand.Company.CompanyId, request.CoBrand.MemberShipId.ToString() ?? "");

            if (memberShipConflicts.IsSuccess && memberShipConflicts.Data!.Any())
                throw new ApiException(new() { new(nameof(request.CoBrand.MemberShipId), GlobalResources.DuplicateMemberShipID) });

            await Task.CompletedTask;
        }

        if (request.DeliveryOption is DeliveryOption.BRANCH)
        {
            if (request.DeliveryBranchId is null)
            {
                throw new ApiException(new() { new(nameof(request.CoBrand.Company.CardType), GlobalResources.PleaseSelectDeliveryBranch) });
            }
        }


    }
    public async Task Prepare()
    {
        customerProfile = ((await _genericCustomerProfileAppService.GetDetailedGenericCustomerProfile(new() { CivilId = request.Customer.CivilId }))?.Data) ?? throw new ApiException(message: "invalid customer");

        decimal reqId = await _requestAppService.GenerateNewRequestId(request.Customer.CivilId);
        //var currentUser = await _employeeAppService.GetCurrentLoggedInUser();
        Branch userBranch = await configurationAppService.GetUserBranch();
        request.BranchId = userBranch.BranchId;

        newRequest = new RequestDto()
        {
            RequestId = reqId,
            CardType = request.ProductId,
            BranchId = request.BranchId,
            CivilId = request.Customer.CivilId,
            Salary = request.Customer.Salary,

            City = request.BillingAddress.City,
            AddressLine1 = request.BillingAddress.AddressLine1,
            AddressLine2 = request.BillingAddress.AddressLine2,
            FaxReference = request.BillingAddress.FaxReference,
            HomePhone = request.BillingAddress.HomePhone ?? 0,
            PostOfficeBoxNumber = request.BillingAddress.PostOfficeBoxNumber,
            PostalCode = request.BillingAddress.PostalCode ?? 0,
            Mobile = request.BillingAddress.Mobile,
            Street = request.BillingAddress.Street,
            WorkPhone = request.BillingAddress.WorkPhone,

            SellerId = request.SellerId,
            Remark = request.Remark,
            TellerId = Convert.ToDecimal(authManager.GetUser()?.KfhId),
            AcctNo = request.DebitAccountNumber
        };

        await BindRequestParameters();

        #region local functions
        async Task BindRequestParameters()
        {
            bool isCoBrand = request.CoBrand != null && request.CoBrand?.Company.CardType > 0;

            var employeeInfo = await _employeeAppService.GetEmployeeNumberByAccountNumber(request.DebitAccountNumber!);
            var sellerInfo = await _employeeAppService.ValidateSellerId(request.SellerId?.ToString("0")!);

            newRequest.Parameters = new()
            {
                KFHStaffID = employeeInfo?.Data?.EmployeeNumber ?? "",
                CustomerClassCode = customerProfile.RimCode.ToString(),
                Employment = Convert.ToInt16(customerProfile.IsRetired).ToString(),
                SellerGenderCode = sellerInfo?.Data?.Gender ?? "",
                ClubMembershipId = isCoBrand ? request!.CoBrand!.MemberShipId.ToString() : null,
                CompanyName = isCoBrand ? request!.CoBrand!.Company.CompanyName.ToString() : null,
                ClubName = isCoBrand ? request!.CoBrand!.Company.ClubName! : null,
                IsVIP = Convert.ToInt16(request!.Customer!.IsVIP).ToString(),
                IssuePinMailer = Convert.ToInt16(request.PinMailer).ToString(),
                CardType = ProductTypes.PrePaid.ToString(),
                Collateral = IsForeignCurrencyCard ? Collateral.FOREIGN_CURRENCY_PREPAID_CARDS.ToString() : Collateral.PREPAID_CARDS.ToString(),
                DeliveryOption = request.DeliveryOption.ToString(),
                DeliveryBranchId = request.DeliveryBranchId?.ToString()
            };

            await BindPromotion();

            await Task.CompletedTask;
        }

        async Task BindPromotion()
        {
            if (request.PromotionModel?.PromotionId is null)
            {
                newRequest.Parameters.PCTFlag = GetDefaultPCT(newRequest!.Parameters.KFHStaffID.ToInt());
                return;
            }

            var selectedCardPromotion = await _promotionsAppService.GetPromotionById(new()
            {
                AccountNumber = request.DebitAccountNumber,
                CivilId = request.Customer!.CivilId,
                ProductId = request.ProductId,
                PromotionId = request.PromotionModel.PromotionId,
                Collateral = Collateral.PREPAID_CARDS
            });

            selectedCardPromotion ??= new();

            selectedCardPromotion.pctFlag ??= GetDefaultPCT(newRequest!.Parameters.KFHStaffID.ToInt());

            newRequest.Parameters.SetPromotion(selectedCardPromotion);

            await Task.CompletedTask;
        }
        string GetDefaultPCT(int kfhStaffId = 0)
        {
            string PCT_Default = cardDefinition?.Extension?.PctDefault!;
            string PCT_Default_Staff = cardDefinition?.Extension?.PctDefaultStaff!;

            if (kfhStaffId != 0 && !string.IsNullOrEmpty(PCT_Default_Staff))
                return PCT_Default_Staff;

            return PCT_Default;
        }
        #endregion


    }
    public async Task<decimal> Issue()
    {
        using var transaction = await _fdrDBContext.Database.BeginTransactionAsync();
        try
        {
            if (request!.CoBrand is not null)
                await _memberShipAppService.DeleteAndCreateMemberShipIfAny(newRequest.CivilId, request.CoBrand);

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


            try
            {
                await _deliveryAppService.RequestDelivery(request.DeliveryOption, NewCardRequestId, request.DeliveryBranchId);
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Unable to request delivery");
            }



            return newRequest.RequestId;


        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            throw;
        }




    }


    public async Task InitiateWorkFlow()
    {
        //Insert Log activity and details
        RequestActivityDto requestActivity = new()
        {
            RequestId = newRequest.RequestId,
            BranchId = newRequest.BranchId,
            CivilId = request.Customer.CivilId,
            IssuanceTypeId = (int)IssuanceTypes.PREPAID,
            CfuActivityId = (int)CFUActivity.Card_Request,
            RequestActivityStatusId = (int)RequestActivityStatus.New,
            WorkflowVariables = new() {
                {WorkflowVariables.ProductType, ProductTypes.PrePaid },
                { WorkflowVariables.Description,$"{cardDefinition!.Name} requested for {newRequest.CivilId }" }
            }
        };
        await _requestActivityAppService.LogRequestActivity(requestActivity!, searchExist: false, isNeedWorkflow: true);
    }
}


