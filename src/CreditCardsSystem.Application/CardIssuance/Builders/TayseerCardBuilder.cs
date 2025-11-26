using CreditCardsSystem.Data;
using CreditCardsSystem.Data.Models;
using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Interfaces.Workflow;
using CreditCardsSystem.Domain.Models.CardIssuance;
using CreditCardsSystem.Domain.Models.CardOperation;
using CreditCardsSystem.Domain.Models.Customer;
using CreditCardsSystem.Domain.Models.Workflow;
using CreditCardsSystem.Domain.Shared.Models.Account;
using CreditCardsSystem.Domain.Shared.Models.Reports;
using CreditCardsSystem.Domain.Shared.Models.RequestActivity;
using CreditCardsSystem.Utility.Extensions;
using HoldManagementServiceReference;
using Kfh.Aurora.Auth;
using Kfh.Aurora.Integration;
using Kfh.Aurora.Logging;
using Kfh.Aurora.Organization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Telerik.DataSource.Extensions;

namespace CreditCardsSystem.Application.CardIssuance.Builders;

[NonController]
public class TayseerCardBuilder : ICardBuilder
{
    #region Variables

    private readonly IRequestActivityAppService _requestActivityAppService;
    private readonly IWorkflowCalculationService workflowCalculationService;
    private readonly IAuthManager authManager;
    private readonly FdrDBContext _fdrDBContext;
    private readonly IRequestAppService _requestAppService;
    private readonly ICurrencyAppService _currencyAppService;
    private readonly IEmployeeAppService _employeeAppService;
    private readonly IPromotionsAppService _promotionsAppService;
    private readonly ICardDetailsAppService _cardDetailsAppService;
    private readonly IAuditLogger<TayseerCardBuilder> _auditLogger;
    private readonly ILogger<TayseerCardBuilder> _logger;
    private readonly IDeliveryAppService _deliveryAppService;
    private readonly ICustomerProfileAppService _genericCustomerProfileAppService;
    private readonly IAccountsAppService _accountsAppService;
    private readonly ICardPaymentAppService _cardPaymentAppService;
    private readonly IConfigurationAppService configurationAppService;

    private readonly HoldManagementServiceClient _holdManagementServiceClient;
    public TayseerCardRequest request;
    private RequestDto newRequest;
    private CardDefinitionDto? cardDefinition;
    private CardCurrencyDto? cardCurrency;
    private GenericCustomerProfileDto? customerProfile;
    private bool IsValidDebitMargin { get; set; } = false;
    public decimal NewCardRequestId { get; set; }
    private bool IsForeignCurrencyCard => cardCurrency!.IsForeignCurrency;
    private bool IsMinorCustomer { get; set; }
    #endregion

    public TayseerCardBuilder(FdrDBContext fdrDBContext, IIntegrationUtility integrationUtility, IOptions<IntegrationOptions> options, IRequestAppService requestAppService, ICurrencyAppService currencyAppService,
         IEmployeeAppService employeeAppService, IPromotionsAppService promotionsAppService,
        ICardDetailsAppService cardDetailsAppService,
         IAuditLogger<TayseerCardBuilder> auditLogger,
                             ILogger<TayseerCardBuilder> logger,
                             IDeliveryAppService deliveryAppService, ICustomerProfileAppService genericCustomerProfileAppService, IAccountsAppService accountsAppService, ICardPaymentAppService cardPaymentAppService, IAuthManager authManager, IRequestActivityAppService requestActivityAppService, IWorkflowCalculationService workflowCalculationService, IConfigurationAppService configurationAppService)
    {
        _fdrDBContext = fdrDBContext;
        _requestAppService = requestAppService;
        _currencyAppService = currencyAppService;
        _employeeAppService = employeeAppService;
        _promotionsAppService = promotionsAppService;
        request = new();
        newRequest = new();
        _cardDetailsAppService = cardDetailsAppService;
        this._auditLogger = auditLogger;
        this._logger = logger;
        this._deliveryAppService = deliveryAppService;
        _genericCustomerProfileAppService = genericCustomerProfileAppService;
        _accountsAppService = accountsAppService;
        _cardPaymentAppService = cardPaymentAppService;

        _holdManagementServiceClient = integrationUtility.GetClient<HoldManagementServiceClient>
        (options.Value.Client, options.Value.Endpoints.HoldManagment, options.Value.BypassSslValidation);
        this.authManager = authManager;
        _requestActivityAppService = requestActivityAppService;
        this.workflowCalculationService = workflowCalculationService;
        this.configurationAppService = configurationAppService;
    }

    public ICardBuilder WithRequest(BaseCardRequest cardRequest)
    {
        request = (cardRequest as TayseerCardRequest)!;
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
                throw new ApiException(validateSufficient.ValidationErrors);
        }


        if (request.ReplaceCard is not null)
            await ValidateReplaceCard();

        //TODO : Card Replace Validation
        async Task ValidateReplaceCard()
        {
            if (request.ReplaceCard.AccountNumber is null)
                throw new ApiException(message: "invalid transfer account number");

            var cardAccount = await _accountsAppService.GetDebitAccountsByAccountNumber(request.ReplaceCard.AccountNumber);

            if (request.Collateral is Collateral.AGAINST_DEPOSIT)
            {

            }

            if (request.Collateral is Collateral.AGAINST_MARGIN)
            {
                if (string.IsNullOrEmpty(request.ReplaceCard.DebitMarginAccountNumber?.Trim()))
                    IsValidDebitMargin = request.CollateralAccountNumber != request.ReplaceCard.DebitMarginAccountNumber;
            }


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
        Branch userBranch = await configurationAppService.GetUserBranch();
        request.BranchId = userBranch.BranchId;
        _ = int.TryParse(authManager.GetUser()?.KfhId, out int _userId);

        newRequest = new RequestDto()
        {
            ReqStatus = (int)CreditCardStatus.PendingForCreditCheckingReview,
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
            MurInstallments = request.Installments?.MurabahaInstallments,
            ReInstallments = request.Installments?.RealEstateInstallment
        };

        await BindRequestParameters();

        if (request.Collateral is Collateral.AGAINST_DEPOSIT)
            await PrepareDeposit();

        if (request.Collateral is Collateral.AGAINST_MARGIN)
            await PrepareMargin();

        SetMinorValue();

        #region local functions

        async Task BindRequestParameters()
        {
            newRequest.Parameters = new()
            {
                Employment = Convert.ToInt16(customerProfile.IsRetired).ToString(),
                KFHStaffID = customerProfile.EmployeeNumber,
                CustomerClassCode = customerProfile.RimCode.ToString(),
                SellerGenderCode = (await _employeeAppService.ValidateSellerId(request?.SellerId?.ToString() ?? ""))?.Data?.Gender ?? "",
                IsVIP = request!.Customer.IsVIP ? "1" : "0",
                IssuePinMailer = request.PinMailer ? "1" : "0",
                CardType = $"T{cardDefinition!.Installments}",
                T3MaxLimit = request.T3MaxLimit.ToString(),
                T12MaxLimit = request.T12MaxLimit.ToString(),
                IsCBKRulesViolated = request.IsCBKRulesViolated.ToString(),
                TotalFixedDuties = request.TotalFixedDuties.ToString()
            };

            await BindCollateral();
            await BindPromotion();
            await BindReplaceCard();

            await Task.CompletedTask;
        }

        async Task BindCollateral()
        {
            //Try to set in client side
            newRequest.Parameters.Collateral = (request.Collateral is null ? Collateral.EXCEPTION : request.Collateral).ToString();

            if (request.ActualCollateral is not null && request.Collateral != request.ActualCollateral)
            {
                newRequest.Parameters.ActualCollateral = request.ActualCollateral.ToString();
            }

            await Task.CompletedTask;
        }

        async Task BindPromotion()
        {
            if (request.PromotionModel?.PromotionId is null) return;

            var selectedCardPromotion = await _promotionsAppService.GetPromotionById(new()
            {
                AccountNumber = request.DebitAccountNumber,
                CivilId = request.Customer!.CivilId,
                ProductId = request.ProductId,
                PromotionId = request.PromotionModel.PromotionId,
                Collateral = Collateral.PREPAID_CARDS
            });

            if (selectedCardPromotion == null) return;

            selectedCardPromotion.pctFlag ??= GetDefaultPCT(newRequest!.Parameters.KFHStaffID);

            newRequest.Parameters.SetPromotion(selectedCardPromotion);

            await Task.CompletedTask;
        }

        async Task BindReplaceCard()
        {
            //Try to set in client side
            if (request.ReplaceCard is not null)
            {
                newRequest.Parameters.OldCardNumberEncrypted = request.ReplaceCard.CardNo;
                newRequest.Parameters.OldFixedDepositAccountNumber = request.ReplaceCard.FdAcctNo;
            }
            await Task.CompletedTask;
        }


        async Task PrepareDeposit()
        {
            var accounts = await _accountsAppService.GetAllAccounts(request.Customer.CivilId);

            _ = int.TryParse(request.DepositNumber, out int _holdId);
            _ = int.TryParse(authManager.GetUser()?.KfhId, out int _userId);

            string description = cardDefinition?.Name + "-" + newRequest.RequestedLimit;

            var depositAccount = accounts?.FirstOrDefault(x => x.Acct == request.CollateralAccountNumber)
                ?? throw new ApiException(new() { new(nameof(request.CollateralAccountNumber), "Invalid deposit account number!") });
            if (_holdId <= 0)
            {
                await CreateNewHold();
                return;
            }

            var holdData = (await _holdManagementServiceClient.viewAllHoldDetailsAsync(new() { acct = depositAccount.Acct, holdId = _holdId }))?.viewAllHoldDetailsResult;

            if (holdData == null || holdData?.Status?.ToLower() == "closed")
            {
                await CreateNewHold();
                return;
            }

            //remove existing hold and recreate new one, if the hold amount not matched with required limit
            bool inSufficientBalance = holdData != null && (double)request.RequiredLimit != holdData.Amount;
            if (inSufficientBalance)
            {
                _ = await _holdManagementServiceClient.removeHoldAsync(new()
                {
                    accountNumber = depositAccount.Acct,
                    description = description,
                    userId = _userId,
                    blockNumber = _holdId,
                    blockAmount = (double)request.RequiredLimit
                });

                await CreateNewHold();
                await UpdateHoldId();
            }

            #region local methods
            async Task UpdateHoldId()
            {
                var holdRequest = await _fdrDBContext.Requests.AsNoTracking().FirstOrDefaultAsync(x => x.DepositNo == holdData!.HoldId.ToString());

                if (holdRequest is null)
                {
                    RequestParameter? holdRequestParameter = await _fdrDBContext.RequestParameters.AsNoTracking().Include(x => x.RequestParameterNavigation)
                        .FirstOrDefaultAsync(x => x.Parameter == "DEPOSIT_NUMBER" && x.Value == holdData!.HoldId.ToString());
                    holdRequest = holdRequestParameter?.RequestParameterNavigation;
                }

                if (holdRequest is null)
                    return;

                holdRequest.DepositNo = newRequest.DepositNo;
                holdRequest.DepositAmount = (int?)newRequest.DepositAmount;
            }

            async Task CreateNewHold()
            {
                if (request.RequiredLimit > depositAccount.AvailableBalance)
                    throw new ApiException(new() { new(nameof(request.RequiredLimit), $"Hold amount {request.RequiredLimit} is greater than deposit account available balance {depositAccount.AvailableBalance.ToMoney()}") });

                var newHold = (await _holdManagementServiceClient.addHoldAsync(new()
                {
                    acctNo = depositAccount.Acct,
                    amount = (double)request.RequiredLimit,
                    currency = depositAccount.Currency,
                    holdExpiryDate = new DateTime(2075, 12, 31),
                    description = description,
                    userId = _userId,
                })).addHoldResult;

                newRequest.Parameters.DepositAccountNumber = depositAccount.Acct;
                newRequest.Parameters.DepositNumber = newHold.HoldId.ToString();
                newRequest.Parameters.DepositAmount = request.RequiredLimit.ToString();
                newRequest.Parameters.DepositReferenceNumber = newHold.ReferenceNo.ToString();

                newRequest.DepositNo = newHold.HoldId.ToString();
                newRequest.DepositAmount = request.RequiredLimit;
            }

            #endregion
            //TODO Create Voucher
        }
        async Task PrepareMargin()
        {
            var accounts = await _accountsAppService.GetAllAccounts(request.Customer.CivilId) ?? throw new ApiException(message: "accounts not found");

            string debitMarginAccountNumber = "";//TODO: assign from UI
            decimal debitMarginAmount = 0;

            if (IsValidDebitMargin)
            {
                debitMarginAccountNumber = request.ReplaceCard!.DebitMarginAccountNumber ?? "";
                debitMarginAmount = request.ReplaceCard!.DebitMarginAmount ?? 0;
            }


            MarginAccount marginAccount = new(request.RequiredLimit, debitMarginAccountNumber, debitMarginAmount);

            if (!string.IsNullOrEmpty(request.CollateralAccountNumber))
            {
                var account = accounts.FirstOrDefault(x => x.Acct == request.CollateralAccountNumber) ?? throw new ApiException(message: "margin account not found");

                marginAccount.AccountNumber = account.Acct;
                marginAccount.AvailableBalance = account?.AvailableBalance ?? 0;
            }
            else
            {
                // creating new margin account
                var debitAccount = accounts.FirstOrDefault(x => x.Acct == request.DebitAccountNumber) ?? throw new ApiException(message: "debit account not found"); ;


                var newMarginAccountResponse = await _accountsAppService.CreateCustomerAccount(
                        new(empID: ConfigurationBase.CreateCustomerAccountEmpId,
                    branchNo: debitAccount.BranchId?.ToString() ?? "",
                        acctType: ConfigurationBase.MarginAccountType,
                    acctClassCode: 1,
                    rimNo: customerProfile.RimNo,
                  title1: debitAccount.Title1 ?? "",
                    title2: debitAccount.Title2 ?? "",
                    description: "Opening RIM account",
                    faceAmount: 0));



                if (!newMarginAccountResponse.IsSuccess)
                    throw new ApiException(message: "Failed to Create the Margin Account");


                //TODO: Print Voucher
                //await _voucherAppService.CreateVoucher(new()
                // {
                //     AccountNo=debitAccount.Acct,
                //     AccountName= debitAccount.Title,
                //     CivilID= request.Customer.CivilId,
                //     IBAN=debitAccount.Iban,
                //     PrintDate=DateTime.Now,
                //     AccountCurrency=debitAccount.Currency,
                // });

                marginAccount.AccountNumber = newMarginAccountResponse.Data ?? "";
            }

            newRequest.Parameters.MarginAccountNumber = marginAccount.AccountNumber;

            //Transfer money in case of low balance
            if (marginAccount.HasInSuffienctBalance)
            {
                var transferResponse = await _cardPaymentAppService.TransferMonetary(new()
                {
                    DebitAccountNumber = request.DebitAccountNumber!,
                    MarginAccount = marginAccount
                });

                if (!transferResponse.IsSuccess)
                    throw new ApiException(message: "Money Transfer Failed");

                newRequest.Parameters.MarginTransferReferenceNumber = transferResponse.Data?.ReferenceNumber ?? "";
                newRequest.Parameters.VoucherAmount = marginAccount.RemainingAmount.ToString();
                newRequest.Parameters.MarginAmount = marginAccount.RequestedLimit.ToString();
            }
            else
            {
                newRequest.Parameters.MarginAmount = marginAccount.AvailableBalance.ToString();
            }

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
        }
        ;
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
            catch (System.Exception ex)
            {

                _logger.LogError(ex, "Unable to request delivery");
            }

            return newRequest.RequestId;
        }
        catch (System.Exception ex)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }


    public async Task InitiateWorkFlow()
    {
        bool isNewMarginAccount = string.IsNullOrEmpty(request.CollateralAccountNumber);
        var percentage = await workflowCalculationService.GetPercentage(NewCardRequestId);
        var maxPercentage = cardDefinition!.Installments is 12 ? newRequest.Parameters.T12MaxLimit : newRequest.Parameters.T3MaxLimit;

        RequestActivityDto requestActivity = new()
        {
            RequestId = newRequest.RequestId,
            BranchId = newRequest.BranchId,
            CivilId = request.Customer.CivilId,
            IssuanceTypeId = (int)IssuanceTypes.TAYSEER,
            CfuActivityId = (int)CFUActivity.Card_Request,
            WorkflowVariables = new(){
            { WorkflowVariables.ProductType, ProductTypes.Tayseer },
            { WorkflowVariables.Percentage, percentage},
            { WorkflowVariables.Description,$"{cardDefinition!.Name} request for {newRequest.CivilId }" },
            { WorkflowVariables.CardType, newRequest.CardType },
            { WorkflowVariables.IsMinorCustomer, IsMinorCustomer },
            { WorkflowVariables.MaxPercentage,maxPercentage},
            { WorkflowVariables.ReqStatus, newRequest.ReqStatus } }
        };

        if (!string.IsNullOrEmpty(newRequest.Parameters.BCDFlag))
            requestActivity.WorkflowVariables.Add(WorkflowVariables.BcdFlag, newRequest.Parameters.BCDFlag);

        if (!string.IsNullOrEmpty(newRequest.Parameters.Collateral))
            requestActivity.WorkflowVariables.Add(WorkflowVariables.Collateral, newRequest.Parameters.Collateral);

        if (request.Collateral is Collateral.AGAINST_MARGIN && isNewMarginAccount)
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
        if (request.Collateral is Collateral.AGAINST_DEPOSIT)
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


        requestActivity.IsTayseerSalaryException = (request.Collateral is Collateral.EXCEPTION or Collateral.AGAINST_SALARY);


        await _requestActivityAppService.LogRequestActivity(requestActivity!, searchExist: false, isNeedWorkflow: true);
    }
}


