using CreditCardsSystem.Data;
using CreditCardsSystem.Data.Models;
using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Interfaces.Workflow;
using CreditCardsSystem.Domain.Models.CardIssuance;
using CreditCardsSystem.Domain.Models.CardOperation;
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
using Collateral = CreditCardsSystem.Domain.Enums.Collateral;

namespace CreditCardsSystem.Application.CardIssuance.Builders;

[NonController]
public class ChargeCardBuilder : ICardBuilder
{
    private readonly IRequestActivityAppService _requestActivityAppService;
    private readonly IWorkflowCalculationService workflowCalculationService;
    private readonly IAuthManager authManager;
    #region Variables
    private readonly FdrDBContext _fdrDBContext;
    private readonly IRequestAppService _requestAppService;
    private readonly ICurrencyAppService _currencyAppService;
    private readonly IMemberShipAppService _memberShipAppService;
    private readonly IEmployeeAppService _employeeAppService;
    private readonly IPromotionsAppService _promotionsAppService;
    private readonly IAccountsAppService _accountsAppService;
    private readonly ICustomerProfileAppService _genericCustomerProfileAppService;
    private readonly ICardPaymentAppService _cardPaymentAppService;
    private readonly IDeliveryAppService _deliveryAppService;

    private readonly ICardDetailsAppService _cardDetailsAppService;
    private readonly IConfigurationAppService configurationAppService;
    private readonly IAuditLogger<ChargeCardBuilder> _auditLogger;
    private readonly ILogger<ChargeCardBuilder> _logger;
    private readonly HoldManagementServiceClient _holdManagementServiceClient;
    private RequestDto newRequest;
    private CardDefinitionDto? cardDefinition;
    private CardCurrencyDto cardCurrency;
    private List<ValidationError> validationErrors;
    private ChargeCardRequest request;
    public decimal NewCardRequestId { get; set; }
    private bool IsForeignCurrencyCard => cardCurrency.IsForeignCurrency;
    private bool IsMinorCustomer { get; set; }

    #endregion

    public ChargeCardBuilder(IWorkflowCalculationService workflowCalculationService,
                             IAuthManager authManager,
                             FdrDBContext fdrDBContext,
                             IIntegrationUtility integrationUtility,
                             IOptions<IntegrationOptions> options,
                             IRequestAppService requestAppService,
                             ICurrencyAppService currencyAppService,
                             IMemberShipAppService memberShipAppService,
                             IEmployeeAppService employeeAppService,
                             IPromotionsAppService promotionsAppService,
                             IAccountsAppService accountsAppService,
                             ICustomerProfileAppService genericCustomerProfileAppService,
                             ICardPaymentAppService cardPaymentAppService,
                             IRequestActivityAppService requestActivityAppService,
                             ICardDetailsAppService cardDetailsAppService,
                             IConfigurationAppService configurationAppService,
                             IAuditLogger<ChargeCardBuilder> auditLogger,
                             ILogger<ChargeCardBuilder> logger,
                             IDeliveryAppService deliveryAppService)
    {
        _cardDetailsAppService = cardDetailsAppService;
        this.workflowCalculationService = workflowCalculationService;
        this.authManager = authManager;
        _fdrDBContext = fdrDBContext;
        _requestAppService = requestAppService;
        _currencyAppService = currencyAppService;
        _memberShipAppService = memberShipAppService;
        _employeeAppService = employeeAppService;
        _promotionsAppService = promotionsAppService;
        _accountsAppService = accountsAppService;
        _cardPaymentAppService = cardPaymentAppService;
        _genericCustomerProfileAppService = genericCustomerProfileAppService;
        _requestActivityAppService = requestActivityAppService;
        _cardDetailsAppService = cardDetailsAppService;
        this.configurationAppService = configurationAppService;
        this._auditLogger = auditLogger;
        this._logger = logger;
        _deliveryAppService = deliveryAppService;
        _holdManagementServiceClient = integrationUtility.GetClient<HoldManagementServiceClient>
        (options.Value.Client, options.Value.Endpoints.HoldManagment, options.Value.BypassSslValidation);

        request = new();
        newRequest = new();
        validationErrors = new();
    }

    public ICardBuilder WithRequest(BaseCardRequest cardRequest)
    {
        request = cardRequest as ChargeCardRequest;
        return this;
    }
    public async Task Validate()
    {
        await request.ModelValidationAsync();
        cardDefinition = await _cardDetailsAppService.GetCardWithExtension(request!.ProductId);
        cardCurrency = await _currencyAppService.GetCardCurrency(request!.ProductId);

        if (request.ProductId == 0)
            throw new ApiException(message: "Invalid product Id!");

        if (await _requestAppService.HasPendingOrActiveCard(request.Customer.CivilId, request.ProductId))
            throw new ApiException(message: "Cannot issue the same card again!");

        if (IsForeignCurrencyCard)
            await ValidateDebitAccount();

        if (request!.CoBrand is not null)
            await CoBrandValidations();

        //uncomment the below validation if we need server side validation
        //await ValidateRequiredLimit(); 

        if (request.Collateral == Collateral.AGAINST_SALARY)
        {
            await ValidateCINET();
            await VerifySalary();
        }

        if (request.DeliveryOption is DeliveryOption.BRANCH)
        {
            if (request.DeliveryBranchId is null)
            {
                throw new ApiException(new() { new(nameof(request.CoBrand.Company.CardType), GlobalResources.PleaseSelectDeliveryBranch) });
            }
        }

        validationErrors.ThrowErrorsIfAny();

        #region local functions
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
        async Task ValidateCINET()
        {
            if (request.Customer.CinetId is null || request.Customer.CinetId < 0)
                validationErrors.Add(new(nameof(request.Customer.CinetId), "You must enter valid Cinet ID."));

            if (request.Customer.TotalCinet is null || request.Customer.TotalCinet < 0)
                validationErrors.Add(new(nameof(request.Customer.TotalCinet), "You must enter valid Total Cinet."));

            await Task.CompletedTask;
        }
        async Task VerifySalary()
        {
            //for Salary verification, if the card currency is USD then take the Salary debit account number
            string accountNumber = IsForeignCurrencyCard ? request?.CollateralAccountNumber ?? "" : request?.DebitAccountNumber ?? "";

            if (string.IsNullOrEmpty(accountNumber))
            {
                if (IsForeignCurrencyCard)
                    validationErrors.Add(new ValidationError(nameof(request.CollateralAccountNumber), "You must choose salary account"));
                else
                    validationErrors.Add(new ValidationError(nameof(request.DebitAccountNumber), "You must choose account"));

                return;
            }

            var verifySalaryResponse = await _accountsAppService.VerifySalary(accountNumber, civilId: request.Customer.CivilId);

            if (!verifySalaryResponse.IsSuccess)
                validationErrors.Add(new ValidationError(nameof(request.Customer.Salary), verifySalaryResponse.Message));

            if (!verifySalaryResponse.Data!.Verified)
                validationErrors.Add(new ValidationError(nameof(request.Customer.Salary), verifySalaryResponse.Data?.Description ?? ""));

            //if the salary (from phenix) greater than the verified salary then we need to take verified one
            request.Customer.Salary = request.Customer.Salary > verifySalaryResponse.Data?.Salary ? verifySalaryResponse.Data.Salary : request.Customer.Salary;
        }
        async Task ValidateDebitAccount()
        {
            if (string.IsNullOrEmpty(request.DebitAccountNumber))
                return;

            var validateSufficient = await _currencyAppService.ValidateSufficientFundForForeignCurrencyCards(request.ProductId, request.DebitAccountNumber);
            if (!validateSufficient.IsSuccess)
                validationErrors.AddRange(validateSufficient.ValidationErrors);

            await Task.CompletedTask; return;
        }
        #endregion
    }
    public async Task Prepare()
    {
        var customerProfile = ((await _genericCustomerProfileAppService.GetDetailedGenericCustomerProfile(new() { CivilId = request.Customer.CivilId }))?.Data) ??
           throw new ApiException(message: "invalid customer");


        IsMinorCustomer = Helpers.GetAgeByDateOfBirth(customerProfile.BirthDate!.Value) < 21;
        decimal reqId = await _requestAppService.GenerateNewRequestId(request.Customer.CivilId);
        Branch userBranch = await configurationAppService.GetUserBranch();
        request.BranchId = userBranch.BranchId;
        _ = int.TryParse(authManager.GetUser()?.KfhId, out int _userId);

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
            TellerId = Convert.ToDecimal(authManager.GetUser()!.KfhId),
            AcctNo = request.DebitAccountNumber,
            RequestedLimit = request.RequiredLimit,
            ApproveLimit = request.RequiredLimit,
            Parameters = new(),
            MurInstallments = request.Installments?.MurabahaInstallments,
            ReInstallments = request.Installments?.RealEstateInstallment
        };

        await BindRequestParameters();

        if (request.Collateral is Collateral.AGAINST_DEPOSIT)
            await PrepareDeposit();

        if (request.Collateral is Collateral.AGAINST_MARGIN)
            await PrepareMargin();

        #region local function

        async Task BindRequestParameters()
        {
            bool havingCobrand = request.IsValidCoBrand;

            //!= null && request.CoBrand?.Company.CardType > 0;
            newRequest.Parameters = new()
            {
                Employment = Convert.ToInt16(customerProfile.IsRetired).ToString(),
                CustomerClassCode = customerProfile.RimCode.ToString(),
                SellerGenderCode = (await _employeeAppService.ValidateSellerId(request?.SellerId?.ToString() ?? ""))?.Data?.Gender ?? "",
                IsVIP = request.Customer.IsVIP ? "1" : "0",
                IssuePinMailer = request.PinMailer ? "1" : "0",
                CardType = ProductTypes.ChargeCard.ToString(),
                ClubMembershipId = havingCobrand ? request.CoBrand!.MemberShipId?.ToString() : null,
                CompanyName = havingCobrand ? request.CoBrand!.Company.CompanyName.ToString() : null,
                ClubName = havingCobrand ? request.CoBrand!.Company.ClubName! : null,
                KFHStaffID = customerProfile.EmployeeNumber,
                MaxLimit = request.MaxLimit.ToString(),
                IsCBKRulesViolated = request.IsCBKRulesViolated.ToString(),
                TotalFixedDuties = request.TotalFixedDuties.ToString(),
                DeliveryOption = request.DeliveryOption.ToString(),
                DeliveryBranchId = request.DeliveryBranchId?.ToString()
            };

            if (request.SupplementaryCards?.Count > 0)
                newRequest.Parameters.IsSupplementaryOrPrimaryChargeCard = ChargeCardType.P.ToString();

            await BindCollateral();
            await BindPromotion();


            await Task.CompletedTask;
        }

        async Task BindCollateral()
        {
            //Try to set in client side
            newRequest.Parameters.Collateral = await GetCollateral();
            if (request.ActualCollateral is not null && request.Collateral != request.ActualCollateral)
            {
                newRequest.Parameters.ActualCollateral = request.ActualCollateral.ToString();
            }

            async Task<string?> GetCollateral()
            {
                if (cardDefinition!.Extension?.IsPrepaid == "1")
                    return IsForeignCurrencyCard ? Collateral.FOREIGN_CURRENCY_PREPAID_CARDS.ToString() : Collateral.PREPAID_CARDS.ToString();

                if (request.Collateral is null)
                    return Collateral.EXCEPTION.ToString();

                if (request.Collateral == Collateral.AGAINST_SALARY && request.IsCBKRulesViolated)
                    return Collateral.SALARY_AND_MARGIN.ToString();

                await Task.CompletedTask;

                return request.Collateral.ToString();
            }

            await Task.CompletedTask;
        }
        async Task BindPromotion()
        {
            if (request.PromotionModel?.PromotionId is null)
            {
                newRequest.Parameters.PCTFlag = GetDefaultPCT(newRequest!.Parameters.KFHStaffID);
                return;
            }

            var selectedCardPromotion = await _promotionsAppService.GetPromotionById(new()
            {
                AccountNumber = request.DebitAccountNumber,
                CivilId = request.Customer!.CivilId,
                ProductId = request.ProductId,
                PromotionId = request.PromotionModel.PromotionId,
                Collateral = request.Collateral
            });
            if (selectedCardPromotion == null) return;

            selectedCardPromotion.pctFlag ??= GetDefaultPCT(newRequest!.Parameters.KFHStaffID);

            newRequest.Parameters.SetPromotion(selectedCardPromotion);

            await Task.CompletedTask;
        }

        async Task PrepareDeposit()
        {
            var accounts = await _accountsAppService.GetAllAccounts(request.Customer.CivilId);

            _ = int.TryParse(request.DepositNumber, out int _holdId);


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
                    description: "Opening RIM account",// $"Opening margin account for {cardDefinition!.Name}",
                    faceAmount: 0));

                if (!newMarginAccountResponse.IsSuccess)
                    throw new ApiException(message: "Failed to Create the Margin Account");


                //TODO: Print Voucher
                //await _voucherAppService.CreateVoucher(new()
                //{
                //    AccountNo = debitAccount.Acct,
                //    AccountName = debitAccount.Title,
                //    CivilID = request.Customer.CivilId,
                //    IBAN = debitAccount.Iban,
                //    PrintDate = DateTime.Now,
                //    AccountCurrency = debitAccount.Currency,
                //});

                marginAccount.AccountNumber = newMarginAccountResponse.Data ?? "";
            }

            newRequest.Parameters.MarginAccountNumber = marginAccount.AccountNumber;

            //Transfer money in case of low balance
            if (marginAccount.HasInSuffienctBalance)
            {
                var transferResponse = await _cardPaymentAppService.TransferMonetary(new()
                {
                    DebitAccountNumber = request.DebitAccountNumber!,
                    MarginAccount = marginAccount,
                    ProductName = cardDefinition!.Name
                });

                if (!transferResponse.IsSuccess)
                    throw new ApiException(message: "Unable to transfer money to newly created margin account", insertSeriLog: true);

                newRequest.Parameters.MarginTransferReferenceNumber = transferResponse.Data?.ReferenceNumber ?? "";
                marginAccount.AvailableBalance = marginAccount.RequestedLimit;
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
            catch (System.Exception ex)
            {

                _logger.LogError(ex, "Unable to request delivery");
            }

            return newRequest.RequestId;
        }
        catch (System.Exception)
        {
            await transaction.RollbackAsync();
            await RollbackMoneyTransaction(request, newRequest);
            throw;
        }
    }

    public async Task InitiateWorkFlow()
    {
        //Insert Log activity and details
        bool isNewMarginAccount = string.IsNullOrEmpty(request.CollateralAccountNumber);
        var percentage = await workflowCalculationService.GetPercentage(NewCardRequestId);
        RequestActivityDto requestActivity = new()
        {
            RequestId = newRequest.RequestId,
            BranchId = newRequest.BranchId,
            CivilId = request.Customer.CivilId,
            IssuanceTypeId = (int)IssuanceTypes.CHARGE,
            CfuActivityId = (int)CFUActivity.Card_Request,
            WorkflowVariables = new() {
            { WorkflowVariables.ProductType, cardDefinition!.ProductType},
            { WorkflowVariables.Description,$"{cardDefinition!.Name} request for {newRequest.CivilId }" },
            { WorkflowVariables.Percentage, percentage},
            { WorkflowVariables.CardType, newRequest.CardType },
            { WorkflowVariables.IsMinorCustomer, IsMinorCustomer },
            { WorkflowVariables.MaxPercentage, request.MaxLimit },
            { WorkflowVariables.BcdFlag, newRequest.Parameters.BCDFlag },
            { WorkflowVariables.Collateral, newRequest.Parameters.Collateral }
        }
        };

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
        await _requestActivityAppService.LogRequestActivity(requestActivity!, searchExist: false, isNeedWorkflow: true);
    }


    #region Private Methods
    private async Task RollbackMoneyTransaction(ChargeCardRequest request, RequestDto newRequest)
    {
        if (request.Collateral == Collateral.AGAINST_MARGIN)
        {
            _ = await _cardPaymentAppService.ReverseMonetary(new()
            {
                DebitAccountNumber = request.DebitAccountNumber ?? "",
                ReferenceNumber = newRequest.Parameters.MarginTransferReferenceNumber,
                MarginAccount = new MarginAccount(request.RequiredLimit, "", 0)
            });
        }

        if (request.Collateral == Collateral.AGAINST_DEPOSIT)
        {
            _ = await _holdManagementServiceClient.removeHoldAsync(new()
            {
                accountNumber = request.CollateralAccountNumber ?? "",
                blockNumber = Convert.ToInt64(newRequest.Parameters.DepositNumber)
            });
        }
    }



    #endregion
}



