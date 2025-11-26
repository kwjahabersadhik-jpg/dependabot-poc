using AccountHoldMgmtServiceReference;
using BankingCustomerProfileReference;
using CreditCardsSystem.Data;
using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Models.Access;
using CreditCardsSystem.Domain.Models.Account;
using CreditCardsSystem.Domain.Models.CardIssuance;
using CreditCardsSystem.Domain.Shared.Interfaces;
using CreditCardsSystem.Domain.Shared.Models.Account;
using CustomerAccountsServiceReference;
using CustomerFinancialPositionServiceReference;
using CustomerStatementServiceReference;
using HrServiceReference;
using Kfh.Aurora.Auth;
using Kfh.Aurora.Integration;
using Kfh.Aurora.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace CreditCardsSystem.Application.Accounts;

public class AccountsAppService(IConfigParameterAppService configParameterService, ILogger<AccountsAppService> logger,
IAuditLogger<AccountsAppService> auditLogger, IAuthManager authManager, IIntegrationUtility integrationUtility,
IOptions<IntegrationOptions> options, ICardDetailsAppService cardDetailsAppService, FdrDBContext fdrDBContext,
ICurrencyAppService currencyAppService) : BaseApiResponse, IAccountsAppService, IAppService
{

    private readonly BankingCustomerProfileServiceClient _customerProfileServiceClient = integrationUtility.GetClient<BankingCustomerProfileServiceClient>(options.Value.Client, options.Value.Endpoints.BankingCustomerProfile, options.Value.BypassSslValidation);
    private readonly CustomerAccountsServiceClient _customerAccountsServiceClient = integrationUtility.GetClient<CustomerAccountsServiceClient>(options.Value.Client, options.Value.Endpoints.CustomerAccount, options.Value.BypassSslValidation);
    private readonly CustomerStatementServiceClient _customerStatementServiceClient = integrationUtility.GetClient<CustomerStatementServiceClient>(options.Value.Client, options.Value.Endpoints.CustomerStatement, options.Value.BypassSslValidation);
    private readonly CustomerFinancialPositionServiceClient _customerFinancialPositionServiceClient = integrationUtility.GetClient<CustomerFinancialPositionServiceClient>(options.Value.Client, options.Value.Endpoints.CustomerFinancialPosition, options.Value.BypassSslValidation);
    private readonly AccountHoldMgmtServiceClient _accountHoldMgmtServiceClient = integrationUtility.GetClient<AccountHoldMgmtServiceClient>(options.Value.Client, options.Value.Endpoints.AccountHold, options.Value.BypassSslValidation);
    private readonly HrServiceClient _hrServiceClient = integrationUtility.GetClient<HrServiceClient>(options.Value.Client, options.Value.Endpoints.Hr, options.Value.BypassSslValidation);
    private readonly IConfigParameterAppService _configParameterService = configParameterService;

    [HttpGet]
    public async Task<ApiResponseModel<HoldDetailsDTO>> GetHoldList(string accountNumber)
    {
        var accountList = (await _accountHoldMgmtServiceClient.viewHoldsListAsync(new() { acct = accountNumber }))?.viewHoldsListResult;

        if (accountList?.listOfHolds is null) return new ApiResponseModel<HoldDetailsDTO>();


        return Success(new HoldDetailsDTO()
        {
            NumberOfHolds = accountList.NumberOfHolds,
            TotalHoldAmount = accountList.TotalAmount,
            HoldDetailsList = accountList.listOfHolds.Select(x => new HoldDetailsListDto()
            {
                Amount = x.Amount,
                Description = x.Description,
                EmployeeId = x.EmployeeId,
                ExpiryDate = x.ExpiryDate,
                HoldId = x.HoldId,
                HoldType = x.HoldType,
                StartDate = x.StartDate,
                Tracer = x.Tracer
            }).ToList()
        });
    }


    [HttpGet]
    public async Task<ApiResponseModel<List<AccountDetailsDto>>> GetDebitAccounts(string civilId, string accountNumber = "")
    {
        var response = new ApiResponseModel<List<AccountDetailsDto>>();
        var DebitAccountsTypesResp = await _configParameterService.GetByKey("DebitAccount");// ConfigurationBase.DebitAccountTypes.Split(",");// ; // To be moved to credit card Admin configuration

        if (!DebitAccountsTypesResp.IsSuccess)
            throw new ApiException(message: "Unable to load debit account type, please check the config");

        var DebitAccountsTypes = DebitAccountsTypesResp.Data?.FirstOrDefault()?.ParamValue?.Split(",");
        //string bankAccountCode = request.AcctNo?.Substring(2, 3) ?? "";

        //var accountType = accountTypeConfigs?.FirstOrDefault(pc => pc.ParamValue.Split(",").Any(pv => pv.Equals(bankAccountCode, StringComparison.InvariantCultureIgnoreCase)));

        //if (accountType is not null)
        //    return accountType.ParamName.Replace(ConfigurationBase.AccountType, "");

        //if (Enum.TryParse(typeof(BankAccountType), bankAccountCode, out object? _bankAccountCode))
        //    return _bankAccountCode.ToString();
        //else
        //    return "";


        IEnumerable<AccountDetailsDto>? kwdDebitAccounts = await GetAllAccounts(civilId, onlyActive: false);

        kwdDebitAccounts = kwdDebitAccounts?.Where(x => x.AllowDebit && x.Currency == "KWD" && !x.IsJoint);

        if (DebitAccountsTypes is not null)
        {
            kwdDebitAccounts = kwdDebitAccounts?.Where(account => DebitAccountsTypes.Any(debitAccountType => debitAccountType == account.AcctType));
        }

        if (!string.IsNullOrEmpty(accountNumber))
        {
            kwdDebitAccounts = kwdDebitAccounts?.Where(x => x.Acct == accountNumber);
        }

        bool viewCurrentAccountBalance = authManager.HasPermission(Permissions.AccountsBalance.View());

        var fallDebitAccount = kwdDebitAccounts?.ToList();

        if (fallDebitAccount?.Count() > 0)
        {
            await ProtectAccountBalance(fallDebitAccount);
        }

        return response.Success(fallDebitAccount?.ToList());
    }
    private async Task ProtectAccountBalance(IEnumerable<AccountDetailsDto> accounts)
    {

        bool viewCurrentAccountBalance = authManager.HasPermission(Permissions.AccountsBalance.View());
        foreach (var item in accounts)
        {
            item.ViewCurrentAccountBalance = viewCurrentAccountBalance;
        }


        var viewStaffAccountsBalance = authManager.HasPermission(Permissions.AccountsStaffBalance.View());
        if (viewStaffAccountsBalance)
            return;

        try
        {
            // Check if User has permission to view employee account
            var accountsNumbers = (from acc in accounts select acc.Acct).ToList();

            // Get Employee No
            var isEmployee = await _hrServiceClient.getEmployeeNoAsync(new()
            {
                acctList = accountsNumbers.ToArray()
            });

            if ((string.IsNullOrEmpty(isEmployee.getEmployeeNoResult)))
                return;

            // Get Employee Info for salary account
            var employeeInfo = await _hrServiceClient.getEmployeeInfoAsync(new getEmployeeInfoRequest
            {
                empNo = isEmployee.getEmployeeNoResult
            });

            // If this is a staff
            if (employeeInfo.getEmployeeInfoResult is null)
                return;


            var salaryAccount = accounts.FirstOrDefault(x => x.Acct == employeeInfo.getEmployeeInfoResult.acctNo);

            if (salaryAccount != null && !viewStaffAccountsBalance)
            {
                salaryAccount.ViewCurrentAccountBalance = false;
                //salaryAccount.AvailableBalance = 0;
            }

        }
        catch (System.Exception ex)
        {
            foreach (var account in accounts)
            {
                account.ViewCurrentAccountBalance = false;
            }

            logger.LogError(ex, nameof(ProtectAccountBalance));
        }

    }

    [HttpGet]
    public async Task<ApiResponseModel<List<AccountDetailsDto>>> GetCorporateAccounts(string civilId)
    {
        var response = new ApiResponseModel<List<AccountDetailsDto>>();

        var debitAccountsResponse = await GetDebitAccounts(civilId);

        if (!debitAccountsResponse.IsSuccess)
            throw new ApiException(methodName: nameof(GetCorporateAccounts), message: "unable to find corporate debit account!");

        var corporateAccounts = debitAccountsResponse?.Data?.Where(x =>
        (x.ApplicationType.Equals("CK", StringComparison.InvariantCultureIgnoreCase) || x.ApplicationType.Equals("SV", StringComparison.InvariantCultureIgnoreCase))
        && (x.StatusEnglish != null && x.StatusEnglish.Equals("Active", StringComparison.InvariantCultureIgnoreCase))
       ).ToList();

        return response.Success(corporateAccounts);
    }


    [HttpGet]
    public async Task<ApiResponseModel<List<AccountDetailsDto>>> GetDebitAccountsForUSDCard(string civilId)
    {
        var response = new ApiResponseModel<List<AccountDetailsDto>>();

        IEnumerable<AccountDetailsDto>? usdDebitAccounts = await GetAllAccounts(civilId);

        usdDebitAccounts = usdDebitAccounts?.Where(ac => ac.Currency == ConfigurationBase.USDollerCurrency);

        //TODO: want to understand what is class code 2 & 122
        usdDebitAccounts = usdDebitAccounts?.Where(ac => (ac.AcctType == "101" && ac.ClassCode == 2) || ((ac.AcctType == "106" || ac.AcctType == "136") && ac.ClassCode == 122));

        return response.Success(usdDebitAccounts?.ToList());
    }

    [HttpGet]
    public async Task<ApiResponseModel<List<AccountDetailsDto>>> GetSalaryAccountsForUSDCard(string civilId)
    {
        var response = new ApiResponseModel<List<AccountDetailsDto>>();

        IEnumerable<AccountDetailsDto>? kwdSalaryDebitAccounts = await GetAllAccounts(civilId);

        kwdSalaryDebitAccounts = kwdSalaryDebitAccounts?.Where(ac => ac.Currency == ConfigurationBase.KuwaitCurrency);

        return response.Success(kwdSalaryDebitAccounts?.ToList());
    }

    [HttpGet]
    public async Task<ApiResponseModel<List<AccountDetailsDto>>> GetMarginAccounts(string civilId, string accountNumber = "")
    {
        var response = new ApiResponseModel<List<AccountDetailsDto>>();

        var allAccounts = await GetAllAccounts(civilId);

        var marginAccounts = allAccounts?.Where(ac => ac.AcctType.Trim() == ConfigurationBase.MarginAccountType);
        if (!string.IsNullOrEmpty(accountNumber))
        {
            marginAccounts = marginAccounts?.Where(x => x.Acct == accountNumber);
        }

        return response.Success(marginAccounts?.ToList());
    }


    [HttpGet]
    public async Task<ApiResponseModel<List<AccountDetailsDto>>> GetDepositAccounts(string civilId, string accountNumber = "")
    {
        var response = new ApiResponseModel<List<AccountDetailsDto>>();

        var allAccounts = await GetAllAccounts(civilId);

        var depositAccounts = allAccounts?.Where(ac => ac.Currency == ConfigurationBase.KuwaitCurrency && ac.ApplicationType.Trim() == ConfigurationBase.DepositAccountApplicationType);
        if (!string.IsNullOrEmpty(accountNumber))
        {
            depositAccounts = depositAccounts?.Where(x => x.Acct == accountNumber);
        }

        return response.Success(depositAccounts?.ToList());
    }

    [HttpGet]
    public async Task<ApiResponseModel<List<AccountDetailsDto>>> GetDepositAccountsForUSDCard(string civilId)
    {
        var response = new ApiResponseModel<List<AccountDetailsDto>>();

        var allAccounts = await GetAllAccounts(civilId);
        var classCodes = ConfigurationBase.DepositAccountClassCodeUSD.Split(',').Select(x => int.TryParse(x, out int _classcode) ? _classcode : 0);

        var depositAccounts = allAccounts?.Where(ac => ac.Currency == ConfigurationBase.USDollerCurrency && ac.AcctType.Trim() == ConfigurationBase.DepositAccountTypeUSD
        && classCodes.Any(classCode => classCode == ac.ClassCode));

        return response.Success(depositAccounts?.ToList());
    }


    [HttpGet]
    public async Task<ApiResponseModel<List<AccountDetailsDto>>> GetDebitAccountsByAccountNumber(string accountNumber)
    {

        if (string.IsNullOrEmpty(accountNumber))
            throw new ApiException(message: "Invalid account number");

        var response = new ApiResponseModel<List<AccountDetailsDto>>();

        var debitAccounts = (await _customerAccountsServiceClient.viewAccountsListByAccountAsync(new()
        {
            acct = accountNumber
        }))?.viewAccountsListByAccountResult.AsQueryable().ProjectToType<AccountDetailsDto>();

        debitAccounts = debitAccounts?.Where(x => x.AllowDebit && x.Currency == "KWD" && !x.IsJoint);

        var acccounts = debitAccounts?.ToList();

        bool viewCurrentAccountBalance = authManager.HasPermission(Permissions.AccountsBalance.View());

        if (acccounts?.Count() > 0)
        {
            await ProtectAccountBalance(acccounts);
        }

        return response.Success(acccounts);
    }

    [HttpGet]
    public async Task<ApiResponseModel<VerifySalaryResponse>> VerifySalary(string accountNumber, string civilId, int tolerance = ConfigurationBase.Tolerance)
    {

        //calling generic profile to get salary amount
        var result = (await _customerProfileServiceClient.viewDetailedGenericCustomerProfileAsync(
        new viewDetailedGenericCustomerProfileRequest
        {
            civilID = civilId,
            accountNumber = accountNumber
        })).viewDetailedGenericCustomerProfileResult;


        _ = decimal.TryParse(result.professionAndFinancialInfo.monthlyIncome, out decimal _monthlyIncome);

        var salaryResponse = (await _customerStatementServiceClient.verifySalaryAsync(new()
        {
            acctNo = accountNumber,
            salary = Convert.ToDouble(_monthlyIncome),
            tolerance = tolerance
        }))?.verifySalaryResult.Adapt<VerifySalaryResponse>();

        if (salaryResponse == null)
            return Failure<VerifySalaryResponse>("Unable to verify ");

        return Success(salaryResponse);
    }

    [HttpGet]
    public async Task<Dictionary<long, string>> GetHoldStatus(int[] holdIds)
    {
        Dictionary<long, string> holdStatuses = new();
        var tasks = holdIds.Select(x => _accountHoldMgmtServiceClient.viewAllHoldDetailsAsync(new() { holdId = x }));

        Task.WaitAll(tasks.ToArray());

        foreach (var task in tasks)
        {
            var hold = (await task)?.viewAllHoldDetailsResult;

            if (hold != null)
                holdStatuses[hold.HoldId] = hold?.Status.Trim() ?? "";
        }

        return holdStatuses;
    }

    [HttpGet]
    public async Task<ApiResponseModel<FinancialPositionResponse>> GetFinancialPosition(string civilId, Collateral collateral = Collateral.AGAINST_SALARY, string accountNumber = "")
    {
        var response = new ApiResponseModel<FinancialPositionResponse>();

        var customerDuties = (await _customerFinancialPositionServiceClient.getCustomerDutiesAsync(new() { civilID = civilId }))?.getCustomerDutiesResult;

        if (customerDuties == null)
            return response.Fail("no records found");


        IEnumerable<AccountDetailsDto>? accountList = Enumerable.Empty<AccountDetailsDto>().AsQueryable();

        if (collateral == Collateral.AGAINST_MARGIN)
        {
            accountList = await GetAllAccounts(civilId);
        }

        var usdRate = await currencyAppService.GetBuyRate(ConfigurationBase.USDollerCurrency);

        var spw = new Stopwatch();
        spw.Start();

        IQueryable<CreditCardApplication> creditCardApplications = (from application in customerDuties!.creditCardApplications.Where(x => x.cardStatus != ((int)CreditCardStatus.Lost).ToString() && x.cardStatus != ((int)CreditCardStatus.Stolen).ToString())
                                                                    join cardDef in fdrDBContext.CardDefs.AsNoTracking().Include(x => x.CardDefExts) on Convert.ToInt32(application.cardType) equals cardDef.CardType
                                                                    join eligblity in fdrDBContext.CardtypeEligibilityMatixes.AsNoTracking() on Convert.ToInt32(application.cardType) equals eligblity.CardType
                                                                    let productType = Helpers.GetProductType(cardDef.Duality, Convert.ToDecimal(cardDef.MinLimit), Convert.ToDecimal(cardDef.MaxLimit))
                                                                    where productType != ProductTypes.PrePaid && eligblity.IsCorporate == false
                                                                    join request in fdrDBContext.Requests.AsNoTracking().Where(x => x.CivilId == civilId).Include(rq => rq.Parameters).AsNoTracking()
                                                                    on new { CardType = application.cardType, CardNo = application.creditCardNumber, CardStatus = application.cardStatus } equals new { CardType = request.CardTypeString, CardNo = request.CardNo, CardStatus = request.ReqStatusString } into requestApplicationGroup
                                                                    from reqAppGroup in requestApplicationGroup.DefaultIfEmpty()
                                                                    let parameter = cardDetailsAppService.GetRequestParameter(reqAppGroup).Result
                                                                    let payeeTypeId = parameter != null ? Helpers.GetPayeeTypeID(cardDef.CardType, parameter.IsSupplementaryOrPrimaryChargeCard) : null
                                                                    let cardLimit = reqAppGroup?.CardType == 26 ? Math.Floor(Convert.ToDecimal(application.limit) * usdRate!.BuyCashRate ?? 0) : Math.Floor(Convert.ToDecimal(application.limit))
                                                                    select new CreditCardApplication
                                                                    {
                                                                        ProductType = productType,
                                                                        UpgradeMatrix = cardDef.CardDefExts.FirstOrDefault(x => x.Attribute == "UPGRADE_DOWNGRADE_TO_CARD_TYPE")?.Value,
                                                                        CardStatus = Convert.ToInt16(application.cardStatus),
                                                                        CreditCardNumber = application.creditCardNumber,
                                                                        AccountNumber = application.accountNumber,
                                                                        MurabahaInstallments = application.murabahaInstallments,
                                                                        ReInstallments = application.reInstallments,
                                                                        Expiry = application.expiry,
                                                                        ProductId = Convert.ToInt32(application.cardType),
                                                                        ProductName = cardDef.Name,
                                                                        RequestId = reqAppGroup?.RequestId ?? 0,
                                                                        HoldId = ((reqAppGroup?.DepositNo ?? "") == "0" && parameter != null) ? Convert.ToInt64(parameter.DepositNumber!) : Convert.ToInt64(reqAppGroup?.DepositNo ?? "0"),
                                                                        DepositAmount = ((reqAppGroup?.DepositAmount ?? 0) == 0 && parameter != null) ? Convert.ToInt16(parameter.DepositAmount!) : reqAppGroup?.DepositAmount ?? 0,
                                                                        HoldStatus = "", // need to call seperate service for it
                                                                        DepositAccount = parameter?.DepositAccountNumber ?? "",
                                                                        CardCollateral = parameter?.Collateral,
                                                                        MarginAccount = parameter?.MarginAccountNumber ?? "",
                                                                        MarginBalance = collateral != Collateral.AGAINST_MARGIN ? 0 : (parameter == null ? 0 : accountList?.FirstOrDefault(x => x.Acct == parameter.MarginAccountNumber)?.AvailableBalance ?? 0),
                                                                        ///calculating card limit for usd card
                                                                        CardLimit = cardLimit,
                                                                        MinimumCardLimit = GetMinCardLimit(cardDef.Duality, cardDef.Installments, cardLimit),
                                                                        CardCategoryParameter = parameter?.IsSupplementaryOrPrimaryChargeCard ?? "",
                                                                        CardCategoryType = payeeTypeId != null ? CardCategoryType.Primary : CardCategoryType.Normal
                                                                    }).AsQueryable();


        creditCardApplications = creditCardApplications.Where(req => req.CardCategoryParameter != "S");

        if (collateral == Collateral.AGAINST_DEPOSIT || collateral == Collateral.AGAINST_DEPOSIT_USD)
        {
            creditCardApplications = creditCardApplications.Where(req => req.DepositAccount == accountNumber);
        }

        if (collateral == Collateral.AGAINST_MARGIN)
        {
            creditCardApplications = creditCardApplications.Where(req => req.CardCollateral == collateral.ToString());
        }


        var tradingApplications = customerDuties.tradingApplications.Select(x => new TradingApplication { InvoiceNumber = x.invoiceNumber, InstallmentAmount = Convert.ToDecimal(x.installmentAmount) });

        var realEstateApplication = customerDuties.realEstateApplications.Select(x => new RealEstateApplication { RealEstateType = x.realEstateType, InstallmentAmount = Convert.ToDecimal(x.installmentAmount) });

        spw.Stop();

        FinancialPositionResponse financialPosition = new()
        {
            CreditCardApplications = creditCardApplications.ToList(),
            TradingApplications = tradingApplications.ToList(),
            RealEstateApplications = realEstateApplication.ToList(),
            UsdRate = usdRate,
            TimeTaken = spw.ElapsedMilliseconds
        };




        static decimal GetMinCardLimit(int duality, decimal? installments, decimal limit)
        {
            if (duality != 7) return limit;

            if (installments == 12)
                return limit / ConfigurationBase.T12PLF;

            return limit / ConfigurationBase.T3PLF;
        }


        //calculate limit for kwd and usd 
        //get usd buy rate

        //if(collateral is Collateral.AGAINST_DEPOSIT or Collateral.AGAINST_DEPOSIT_USD && depositAccount !=accountNumber)
        //{
        //    //continue;
        //}

        //if (collateral is Collateral.AGAINST_MARGIN && collateral != cardCollateral)
        //{
        //    //continue;
        //}


        //Check deposit[usd] account if collateral is again deposit
        //Check margin account if collateral is again margin


        return response.Success(financialPosition);
    }





    [NonAction]
    public async Task<ApiResponseModel<string>> CreateCustomerAccount(CreateCustomerAccountRequest request)
    {
        ApiResponseModel<string> response = new();

        var result = (await _customerAccountsServiceClient.createCustomerAccountAsync(request.Adapt<createCustomerAccountRequest>()))?.createCustomerAccountResult;

        if (!result.isSuccessful)
            return response.Fail(result.description);

        //Account Number
        return response.Success(result.description);
    }

    [NonAction]
    public async Task<List<AccountDetailsDto>?> GetAllAccounts(string civilId, bool onlyActive = true)
    {
        var allDebitAccount = (await _customerAccountsServiceClient.viewAccountsListByCivilIdAsync(new viewAccountsListByCivilIdRequest()
        {
            civilId = civilId
        }))?.viewAccountsListByCivilIdResult?.AsQueryable().ProjectToType<AccountDetailsDto>();


        if (onlyActive)
            allDebitAccount = allDebitAccount?.Where(x => (x.StatusEnglish != null && x.StatusEnglish!.ToUpper() == "ACTIVE"));


        bool viewCurrentAccountBalance = authManager.HasPermission(Permissions.AccountsBalance.View());

        var fallDebitAccount = allDebitAccount?.ToList();

        if (fallDebitAccount?.Count() > 0)
        {
            await ProtectAccountBalance(fallDebitAccount);
        }

        return fallDebitAccount;
    }

    [NonAction]
    public async Task<ApiResponseModel<string>> GetAccountNumberByRequestId(string requestId)
    {
        ApiResponseModel<string> response = new();
        return response.Success((await fdrDBContext.Requests.FirstOrDefaultAsync(x => x.RequestId == Convert.ToDecimal(requestId)))?.AcctNo);
    }
}

