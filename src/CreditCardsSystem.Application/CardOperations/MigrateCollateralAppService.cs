using CreditCardsSystem.Data;
using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Models.Access;
using CreditCardsSystem.Domain.Models.Card;
using CreditCardsSystem.Domain.Models.CardIssuance;
using CreditCardsSystem.Domain.Models.CardOperation;
using CreditCardsSystem.Domain.Models.Workflow;
using CreditCardsSystem.Domain.Shared.Models.Account;
using CreditCardsSystem.Domain.Shared.Models.Reports;
using CreditCardsSystem.Domain.Shared.Models.RequestActivity;
using HoldManagementServiceReference;
using Kfh.Aurora.Auth;
using Kfh.Aurora.Integration;
using Kfh.Aurora.Organization;
using Microsoft.EntityFrameworkCore;

namespace CreditCardsSystem.Application.CardOperations;
public class MigrateCollateralAppService : BaseApiResponse, IMigrateCollateralAppService, IAppService
{
    #region Private Fields

    private readonly HoldManagementServiceClient _holdManagementServiceClient;
    private readonly ICardDetailsAppService _cardDetailsAppService;
    private readonly IRequestActivityAppService _requestActivityAppService;
    private readonly IRequestAppService _requestAppService;
    private readonly IAccountsAppService _accountsAppService;
    private readonly ICardPaymentAppService _cardPaymentAppService;
    private readonly IOrganizationClient _organizationClient;
    private readonly FdrDBContext _fdrDBContext;
    private readonly IAuthManager authManager;
    private readonly ICustomerProfileAppService _genericCustomerProfileAppService;
    private readonly IUserPreferencesClient _userPreferencesClient;

    private readonly IConfigurationAppService configurationAppService;

    #endregion
    public MigrateCollateralAppService(IIntegrationUtility integrationUtility, IOptions<IntegrationOptions> options, ICardDetailsAppService cardDetailsAppService, IRequestActivityAppService requestActivityAppService, IRequestAppService requestAppService, IAccountsAppService accountsAppService, ICardPaymentAppService cardPaymentAppService, IOrganizationClient organizationClient, FdrDBContext fdrDBContext, IAuthManager authManager, ICustomerProfileAppService genericCustomerProfileAppService, IUserPreferencesClient userPreferencesClient, IConfigurationAppService configurationAppService)
    {
        _holdManagementServiceClient = integrationUtility.GetClient<HoldManagementServiceClient>(options.Value.Client, options.Value.Endpoints.HoldManagment, options.Value.BypassSslValidation);
        _cardDetailsAppService = cardDetailsAppService;
        _requestActivityAppService = requestActivityAppService;
        _requestAppService = requestAppService;
        _accountsAppService = accountsAppService;
        _cardPaymentAppService = cardPaymentAppService;
        _organizationClient = organizationClient;
        _fdrDBContext = fdrDBContext;
        this.authManager = authManager;
        _genericCustomerProfileAppService = genericCustomerProfileAppService;
        _userPreferencesClient = userPreferencesClient;
        this.configurationAppService = configurationAppService;
    }

    #region Migrate Collateral
    [HttpPost]
    public async Task<ApiResponseModel<MigrateCollateralResponse>> RequestMigrateCollateral([FromBody] MigrateCollateralRequest request)
    {

        if (!authManager.HasPermission(Permissions.MigrateCollateral.Request()))
            return Failure<MigrateCollateralResponse>(GlobalResources.NotAuthorized);

        var Card = ((await _cardDetailsAppService.GetCardInfo(request.RequestId))?.Data) ?? throw new ApiException(message: "invalid card");
        var customerProfile = ((await _genericCustomerProfileAppService.GetDetailedGenericCustomerProfile(new() { CivilId = Card.CivilId }))?.Data) ?? throw new ApiException(message: "invalid customer");

        var accountDetail = await GetAccountDetail(request, Card);
        var CfuActivity = request.Collateral == Collateral.AGAINST_MARGIN ? CFUActivity.MIGRATE_COLLATERAL_MARGIN : CFUActivity.MIGRATE_COLLATERAL_DEPOSIT;

        _ = int.TryParse(authManager.GetUser()?.KfhId, out int _userId);
        Branch? userBranch = await configurationAppService.GetUserBranch();
        MarginAccount marginAccount = new(Card!.ApproveLimit, accountDetail!.Acct, accountDetail!.AvailableBalance);

        if (request.Collateral != Collateral.AGAINST_MARGIN && request.Collateral != Collateral.AGAINST_DEPOSIT)
            return Failure<MigrateCollateralResponse>(message: "Invalid Collateral");


        if (request.Collateral == Collateral.AGAINST_MARGIN)
            await MigrateToMarginAccount();

        if (request.Collateral == Collateral.AGAINST_DEPOSIT)
            await MigrateToDepositAccount();

        await LogRequestActivity();
        return Success(new MigrateCollateralResponse(), message: GlobalResources.WaitingForApproval);


        #region local functions

        async Task MigrateToDepositAccount()
        {
            if (accountDetail!.AvailableBalance < Card.ApproveLimit)
                throw new ApiException(new() { new(nameof(Card.ApproveLimit), "Deposit account does not have enough balance to create hold for card limit") });

            string description = Card.ProductName + "-" + Card.ApproveLimit;

            var newHold = (await _holdManagementServiceClient.addHoldAsync(new()
            {
                acctNo = accountDetail.Acct,
                amount = (double)Card.ApproveLimit,
                currency = accountDetail.Currency,
                holdExpiryDate = new DateTime(2075, 12, 31),
                description = description,
                userId = Convert.ToInt32(authManager.GetUser()?.KfhId)
            })).addHoldResult;

            marginAccount.HoldId = newHold.HoldId.ToString();
            marginAccount.ReferenceNumber = newHold.ReferenceNo.ToString();
            marginAccount.AccountNumber = marginAccount.DebitMarginAccountNumber;
        }

        async Task MigrateToMarginAccount()
        {

            var newMarginAccountResponse = await _accountsAppService.CreateCustomerAccount(new(
                empID: ConfigurationBase.CreateCustomerAccountEmpId,
                    branchNo: accountDetail?.BranchId?.ToString() ?? "",
                    acctType: ConfigurationBase.MarginAccountType,
                    acctClassCode: 1,// there is only one class code for 118 account which is 1
                    rimNo: customerProfile.RimNo,
                    title1: accountDetail?.Title1 ?? "",
                    title2: accountDetail?.Title2 ?? "",
                    description: $"Opening RIM account",
                    faceAmount: 0));


            if (!newMarginAccountResponse.IsSuccess)
                throw new ApiException(message: "Failed to Create the Margin Account");

            marginAccount.AccountNumber = newMarginAccountResponse.Data ?? "";

            //Transfer money in case of low balance
            if (marginAccount.HasInSuffienctBalance)
            {
                var transferResponse = await _cardPaymentAppService.TransferMonetary(new()
                {
                    DebitAccountNumber = accountDetail.Acct!,
                    MarginAccount = marginAccount,
                    ProductName = "Tayseer A M  " + Card.RequestId
                });

                if (!transferResponse.IsSuccess)
                    throw new ApiException(message: "Money Transfer Failed");

                marginAccount.ReferenceNumber = transferResponse.Data?.ReferenceNumber ?? "";
            }
        }

        async Task RollbackMoneyTransaction()
        {
            if (request.Collateral == Collateral.AGAINST_MARGIN)
            {
                _ = await _cardPaymentAppService.ReverseMonetary(new()
                {
                    DebitAccountNumber = accountDetail!.Acct,
                    ReferenceNumber = marginAccount.ReferenceNumber,
                    MarginAccount = new MarginAccount(Card!.ApproveLimit, "", 0)
                });
            }

            if (request.Collateral == Collateral.AGAINST_DEPOSIT)
            {
                _ = await _holdManagementServiceClient.removeHoldAsync(new()
                {
                    accountNumber = accountDetail!.Acct,
                    blockNumber = Convert.ToInt64(marginAccount.HoldId)
                });
            }
        }

        async Task LogRequestActivity()
        {
            var requestActivityDto = new RequestActivityDto()
            {
                CivilId = Card.CivilId!,
                RequestActivityStatusId = (int)RequestActivityStatus.Pending,
                IssuanceTypeId = (int)Card.IssuanceType,
                RequestId = Card.RequestId,
                CardNumber = Card.CardNumber,
                CfuActivityId = (int)CfuActivity,
                WorkflowVariables = new() {
                    { "Description", $"Request Migrate Collateral for {Card.CivilId} to DebitAccountNumber {request.DebitAccountNumber}" },
            { WorkflowVariables.CurrentCollateral, Card.Collateral??""},
            { WorkflowVariables.TargetCollateral, request.Collateral!.ToString()!},
            { WorkflowVariables.PendingCollateralMigration, "1"},
            { WorkflowVariables.MigratedById,  authManager.GetUser()!.KfhId},
            { WorkflowVariables.MigratedByName, authManager.GetUser()!.Name},
            { WorkflowVariables.MigratorBranchId,  userBranch!.BranchId.ToString()},
            { WorkflowVariables.MigratorBranchName, userBranch!.Name},
            { WorkflowVariables.MarginReferenceNumber, marginAccount.ReferenceNumber },
            { WorkflowVariables.CardNumber, Card.CardNumberDto??"" }
            },
                Details = new(){
            { ReportingConstants.KEY_CURRENT_ISSUING_OPTION, Card.Collateral??""},
            { ReportingConstants.KEY_TARGET_ISSUING_OPTION, request.Collateral!.ToString()!},
            { ReportingConstants.KEY_PENDING_COLLATERAL_MIGRATION, "1"},
            { ReportingConstants.KEY_MIGRATED_BY_ID,  authManager.GetUser()!.KfhId},
            { ReportingConstants.KEY_MIGRATED_BY_NAME, authManager.GetUser()!.Name},
            { ReportingConstants.KEY_MIGRATOR_BRANCH_ID,  userBranch!.BranchId.ToString()},
            { ReportingConstants.KEY_MIGRATOR_BRANCH_NAME, userBranch!.Name},
            { ReportingConstants.MARGIN_REFERENCE_NO, marginAccount.ReferenceNumber }
            }
            };

            if (CfuActivity is CFUActivity.MIGRATE_COLLATERAL_DEPOSIT)
            {
                requestActivityDto.Details.Add(ReportingConstants.DEPOSIT_ACCOUNT_NO, marginAccount.AccountNumber);
                requestActivityDto.Details.Add(ReportingConstants.DEPOSIT_AMOUNT, marginAccount.RequestedLimit.ToString() ?? "");
                requestActivityDto.Details.Add(ReportingConstants.DEPOSIT_NUMBER, marginAccount.HoldId);


                requestActivityDto.WorkflowVariables.Add(WorkflowVariables.DepositAccountNumber, marginAccount.AccountNumber);
                requestActivityDto.WorkflowVariables.Add(WorkflowVariables.DepositAmount, marginAccount.RequestedLimit.ToString() ?? "");
                requestActivityDto.WorkflowVariables.Add(WorkflowVariables.DepositNumber, marginAccount.HoldId);
            }

            if (CfuActivity is CFUActivity.MIGRATE_COLLATERAL_MARGIN)
            {
                requestActivityDto.Details.Add(ReportingConstants.MARGIN_ACCOUNT_NO, marginAccount.AccountNumber);
                requestActivityDto.Details.Add(ReportingConstants.MARGIN_AMOUNT, marginAccount.RequestedLimit.ToString() ?? "");

                requestActivityDto.WorkflowVariables.Add(WorkflowVariables.MarginAccountNumber, marginAccount.AccountNumber);
                requestActivityDto.WorkflowVariables.Add(WorkflowVariables.MarginAmount, marginAccount.RequestedLimit.ToString() ?? "");
            }

            await _requestActivityAppService.LogRequestActivity(requestActivityDto, isNeedWorkflow: true);
        }

        async Task<AccountDetailsDto?> GetAccountDetail(MigrateCollateralRequest request, CardDetailsResponse Card)
        {
            var accountDetailResponse = request.Collateral == Collateral.AGAINST_MARGIN ?
                await _accountsAppService.GetDebitAccounts(Card.CivilId!, request.CollateralAccountNumber) :
                await _accountsAppService.GetDepositAccounts(Card.CivilId!, request.CollateralAccountNumber);


            if (!accountDetailResponse.IsSuccess)
                throw new ApiException(message: "accounts not found");

            var accountDetail = accountDetailResponse.Data!.FirstOrDefault();
            return accountDetail;
        }
        #endregion
    }

    [HttpPost]
    public async Task<ApiResponseModel<ProcessResponse>> ProcessMigrateCollateral([FromBody] ProcessMigrateCollateralRequest request)
    {
        if (!authManager.HasPermission(Permissions.MigrateCollateral.EnigmaApprove()))
            return Failure<ProcessResponse>(GlobalResources.NotAuthorized);

        var requestActivity = (await _requestActivityAppService.GetRequestActivityById(request.RequestActivityId)).Data ?? throw new ApiException(message: "Unable to find Request");
        var cardInfo = (await _cardDetailsAppService.GetCardInfo(requestActivity.RequestId))?.Data ?? throw new ApiException(message: "Invalid request Id");

        if (request.ActionType == ActionType.Rejected)
            return await RejectRequestActivity(request.ReasonForRejection, cardInfo.IssuanceType, cardInfo.CivilId, requestActivity.RequestActivityId);

        return await Approve();

        async Task<ApiResponseModel<ProcessResponse>> Approve()
        {
            _ = Enum.TryParse(requestActivity.Details[ReportingConstants.KEY_TARGET_ISSUING_OPTION], out Collateral targetCollateral);

            RequestParameterDto parameterToAdd = new() { Collateral = targetCollateral.ToString() };
            RequestParameterDto parameterToRemove = new();

            //Update request data for deposit amount
            if (targetCollateral is Collateral.AGAINST_DEPOSIT)
            {
                var cardRequest = await _fdrDBContext.Requests.FirstOrDefaultAsync(x => x.RequestId == request.RequestId);
                cardRequest.DepositAmount = Convert.ToInt32(requestActivity.Details[ReportingConstants.DEPOSIT_AMOUNT]);
                cardRequest.DepositNo = requestActivity.Details[ReportingConstants.DEPOSIT_NUMBER];
                await _fdrDBContext.SaveChangesAsync();

                //Adding deposit request parameter
                parameterToAdd.DepositAmount = cardRequest.DepositAmount.ToString();
                parameterToAdd.DepositNumber = cardRequest.DepositNo.ToString();

                if (requestActivity.Details.ContainsKey(ReportingConstants.MARGIN_REFERENCE_NO))
                    parameterToAdd.DepositReferenceNumber = requestActivity.Details[ReportingConstants.MARGIN_REFERENCE_NO];

                //Removing margin request parameter while changing to deposit
                parameterToRemove.MarginAccountNumber = "-1";
                parameterToRemove.MarginAmount = "-1";
                parameterToRemove.MarginTransferReferenceNumber = "-1";
            }
            else
            {
                //Adding margin request parameter
                parameterToAdd.MarginAmount = requestActivity.Details[ReportingConstants.MARGIN_AMOUNT];
                parameterToAdd.MarginAccountNumber = requestActivity.Details[ReportingConstants.MARGIN_ACCOUNT_NO];

                if (requestActivity.Details.ContainsKey(ReportingConstants.MARGIN_REFERENCE_NO))
                    parameterToAdd.MarginTransferReferenceNumber = requestActivity.Details[ReportingConstants.MARGIN_REFERENCE_NO];

                //Removing deposit request parameter while changing to margin
                parameterToRemove.DepositNumber = "-1";
                parameterToRemove.DepositAmount = "-1";
                parameterToRemove.DepositReferenceNumber = "-1";
            }

            await _requestAppService.RemoveRequestParameters(parameterToRemove, requestActivity.RequestId);
            await _requestAppService.AddRequestParameters(parameterToAdd, requestActivity.RequestId, deleteBeforeInsert: true);

            await _requestActivityAppService.UpdateRequestActivityStatus(new() { IssuanceTypeId = (int)cardInfo.ProductType, CivilId = cardInfo.CivilId, RequestActivityId = (int)request.RequestActivityId, RequestActivityStatusId = (int)RequestActivityStatus.Approved });

            return Success(new ProcessResponse(), message: "Successfully Approved!");
        }
    }
    private async Task<ApiResponseModel<ProcessResponse>> RejectRequestActivity(string reasonForRejection, IssuanceTypes issuanceType, string civilId, decimal requestActivityId, RequestActivityStatus status = RequestActivityStatus.Rejected)
    {
        await _requestActivityAppService.UpdateRequestActivityStatus(new()
        {
            ReasonForRejection = reasonForRejection,
            IssuanceTypeId = (int)issuanceType,
            CivilId = civilId,
            RequestActivityId = requestActivityId,
            RequestActivityStatusId = (int)status
        });
        return Success(new ProcessResponse() { CardNumber = "" }, message: "Successfully Rejected");
    }

    #endregion



}
