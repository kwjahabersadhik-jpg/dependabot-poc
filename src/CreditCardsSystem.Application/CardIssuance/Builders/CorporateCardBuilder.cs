using CreditCardsSystem.Data;
using CreditCardsSystem.Data.Models;
using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Models.CardIssuance;
using CreditCardsSystem.Domain.Models.CardOperation;
using CreditCardsSystem.Domain.Models.Workflow;
using CreditCardsSystem.Domain.Shared.Models.RequestActivity;
using Kfh.Aurora.Auth;
using Kfh.Aurora.Logging;
using Kfh.Aurora.Organization;
using Microsoft.Extensions.Logging;
using Collateral = CreditCardsSystem.Domain.Enums.Collateral;

namespace CreditCardsSystem.Application.CardIssuance.Builders;

[NonController]
public class CorporateCardBuilder : ICardBuilder
{

    #region Variables
    private readonly FdrDBContext _fdrDBContext;
    private readonly IRequestAppService _requestAppService;
    private readonly IEmployeeAppService _employeeAppService;
    private readonly IPromotionsAppService _promotionsAppService;
    private readonly ICustomerProfileAppService _genericCustomerProfileAppService;
    private readonly IRequestActivityAppService _requestActivityAppService;
    private readonly ICardDetailsAppService _cardDetailsAppService;
    private readonly ICorporateProfileAppService _corporateProfileAppService;


    private RequestDto newRequest;
    private CardDefinitionDto? cardDefinition;
    private List<ValidationError> validationErrors;
    private CorporateCardRequest request;
    private CorporateProfileDto? corporateProfileDto;
    private readonly IConfigurationAppService configurationAppService;
    private readonly IAuditLogger<CorporateCardBuilder> _auditLogger;
    private readonly ILogger<CorporateCardBuilder> _logger;
    private readonly IDeliveryAppService _deliveryAppService;
    private readonly IAuthManager authManager;
    public decimal NewCardRequestId { get; set; }
    #endregion

    public CorporateCardBuilder(IAuthManager authManager, FdrDBContext fdrDBContext,
        IRequestAppService requestAppService,
        IEmployeeAppService employeeAppService,
        IPromotionsAppService promotionsAppService,
        ICustomerProfileAppService genericCustomerProfileAppService,
        IRequestActivityAppService requestActivityAppService,
        ICardDetailsAppService cardDetailsAppService,
        ICorporateProfileAppService corporateProfileAppService,
        IConfigurationAppService configurationAppService,
         IAuditLogger<CorporateCardBuilder> auditLogger,
                             ILogger<CorporateCardBuilder> logger,
                             IDeliveryAppService deliveryAppService
        )
    {
        this.authManager = authManager;
        _fdrDBContext = fdrDBContext;
        _requestAppService = requestAppService;
        _employeeAppService = employeeAppService;
        _promotionsAppService = promotionsAppService;
        _genericCustomerProfileAppService = genericCustomerProfileAppService;
        _requestActivityAppService = requestActivityAppService;
        _cardDetailsAppService = cardDetailsAppService;

        request = new();
        newRequest = new();
        validationErrors = new();
        _corporateProfileAppService = corporateProfileAppService;
        this.configurationAppService = configurationAppService;
        this._auditLogger = auditLogger;
        this._logger = logger;
        this._deliveryAppService = deliveryAppService;
    }

    public ICardBuilder WithRequest(BaseCardRequest cardRequest)
    {
        request = cardRequest as CorporateCardRequest;
        return this;
    }
    public async Task Validate()
    {
        await request.ModelValidationAsync();
        cardDefinition = await _cardDetailsAppService.GetCardWithExtension(request!.ProductId);

        if (request.ProductId == 0)
            throw new ApiException(message: "Invalid product Id!");

        if (await _requestAppService.HasPendingOrActiveCard(request.Customer.CivilId, request.ProductId))
            throw new ApiException(message: "Cannot issue the same card again!");

        await ValidateRequiredLimit();

        if (request.DeliveryOption is DeliveryOption.BRANCH)
        {
            if (request.DeliveryBranchId is null)
            {
                throw new ApiException(new() { new(nameof(request.CoBrand.Company.CardType), GlobalResources.PleaseSelectDeliveryBranch) });
            }
        }

        validationErrors.ThrowErrorsIfAny();

        #region local functions
        async Task ValidateRequiredLimit()
        {
            bool isNotInRange = request.RequiredLimit < cardDefinition.MinLimit || request.RequiredLimit > cardDefinition.MaxLimit;
            bool isNotRounded = request.RequiredLimit % 10 != 0;
            string limitMessage = $" between card limit range minimum {cardDefinition.MinLimit} to maximum {cardDefinition.MaxLimit}";

            if (request.RequiredLimit <= 0 || isNotInRange)
                validationErrors.Add(new(nameof(request.RequiredLimit), $"You should enter value, {limitMessage}"));

            //if (isNotRounded)
            //    validationErrors.Add(new(nameof(request.RequiredLimit), $"You should enter rounded value, {limitMessage}"));

            if (cardDefinition.Duality != ConfigurationBase.DualityFlag)
            {
                decimal requestPercentage = request.RequiredLimit / (cardDefinition.MaxLimit ?? 0);
                if (requestPercentage > ConfigurationBase.MaximumPercentage || requestPercentage < 0)
                    validationErrors.Add(new(nameof(request.RequiredLimit), $"The highest limit available ({cardDefinition.MaxLimit}) is not enough to issue card with limit ({request.RequiredLimit})"));
            }

            var corporateProfile = await _corporateProfileAppService.GetProfile(request.CorporateCivilId);
            if (!corporateProfile.IsSuccess)
                throw new ApiException(message: corporateProfile.Message);

            corporateProfileDto = corporateProfile.Data;

            if (corporateProfileDto!.RemainingLimit < request.RequiredLimit)
                validationErrors.Add(new(nameof(request.RequiredLimit), "The available corporate limit is less than card limit"));

        }
        #endregion
    }
    public async Task Prepare()
    {
        var customerProfile = ((await _genericCustomerProfileAppService.GetDetailedGenericCustomerProfile(new() { CivilId = request.Customer.CivilId }))?.Data) ??
           throw new ApiException(message: "invalid customer");

        decimal reqId = await _requestAppService.GenerateNewRequestId(request.Customer.CivilId);
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
            AcctNo = request.DebitAccountNumber,
            RequestedLimit = request.RequiredLimit,
            ApproveLimit = request.RequiredLimit,
            Parameters = new(),
            MurInstallments = request.Installments?.MurabahaInstallments,
            ReInstallments = request.Installments?.RealEstateInstallment
        };

        await BindRequestParameters();

        #region local function

        async Task BindRequestParameters()
        {
            newRequest.Parameters = new()
            {
                IsVIP = request.Customer.IsVIP ? "1" : "0",
                IssuePinMailer = request.PinMailer ? "1" : "0",
                CardType = ProductTypes.ChargeCard.ToString(),
                CustomerClassCode = customerProfile.RimCode.ToString(),
                SellerGenderCode = (await _employeeAppService.ValidateSellerId(request?.SellerId?.ToString() ?? ""))?.Data?.Gender ?? "",
                KFHStaffID = customerProfile.EmployeeNumber,
                Employment = customerProfile.IsRetired ? "1" : "0",

                CorporateCivilId = corporateProfileDto!.CorporateCivilId,
                CommitmentType = corporateProfileDto!.GlobalLimitDto.CommitmentType,
                Amount = corporateProfileDto!.GlobalLimitDto.Amount.ToString(),
                CommitmentNo = corporateProfileDto!.GlobalLimitDto.CommitmentNo,
                Status = corporateProfileDto!.GlobalLimitDto.Status,
                Undisbursed = corporateProfileDto!.GlobalLimitDto.UndisbursedAmount.ToString(),
                DeliveryOption = request.DeliveryOption.ToString(),
                DeliveryBranchId = request.DeliveryBranchId?.ToString()
            };

            if (corporateProfileDto!.GlobalLimitDto.MaturityDateSpecified)
                newRequest.Parameters.MaturityDate = corporateProfileDto!.GlobalLimitDto.MaturityDate.ToString();

            await BindCollateral();
            await BindPromotion();

            await Task.CompletedTask;
        }

        async Task BindCollateral()
        {
            //Try to set in client side
            newRequest.Parameters.Collateral = Collateral.AGAINST_CORPORATE_CARD.ToString();
            await Task.CompletedTask;
        }
        async Task BindPromotion()
        {
            if (request?.PromotionModel?.PromotionId is null) return;

            var selectedCardPromotion = await _promotionsAppService.GetPromotionById(new()
            {
                AccountNumber = request.DebitAccountNumber,
                CivilId = request.Customer!.CivilId,
                ProductId = request.ProductId,
                PromotionId = request.PromotionModel.PromotionId,
                Collateral = Collateral.AGAINST_CORPORATE_CARD
            });
            if (selectedCardPromotion == null) return;

            selectedCardPromotion.pctFlag ??= GetDefaultPCT(newRequest!.Parameters.KFHStaffID);

            newRequest.Parameters.SetPromotion(selectedCardPromotion);

            await Task.CompletedTask;
        }


        //string GetUserId() => _httpContextAccessor.HttpContext.User.Claims.Single(x => x.Type == "sub").Value;
        string GetDefaultPCT(string? kfhStaffId)
        {
            string PCT_Default = cardDefinition?.Extension?.PctDefault!;
            string PCT_Default_Staff = cardDefinition?.Extension?.PctDefaultStaff!;

            if (kfhStaffId != "0" && !string.IsNullOrEmpty(PCT_Default_Staff))
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
        RequestActivityDto requestActivity = new()
        {
            RequestId = newRequest.RequestId,
            BranchId = newRequest.BranchId,
            CivilId = request.Customer.CivilId,
            IssuanceTypeId = (int)IssuanceTypes.OTHERS,
            CfuActivityId = (int)CFUActivity.Card_Request,
            WorkflowVariables = new() {
                { WorkflowVariables.CardType, newRequest.CardType },
               {WorkflowVariables.ProductType, ProductTypes.Corporate },
                { WorkflowVariables.Description,$"{cardDefinition!.Name} request for { request.Customer.CivilId }" },
            }
        };

        await _requestActivityAppService.LogRequestActivity(requestActivity!, searchExist: false, isNeedWorkflow: true);
    }



    #region Private Methods

    #endregion
}


