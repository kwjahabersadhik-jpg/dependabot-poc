using CreditCardsSystem.Data;
using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Models.CardIssuance;
using CreditCardsSystem.Domain.Shared.Models.Card;
using CreditCardUpdateServiceReference;
using CurrencyInformationServiceReference;
using CustomerAccountsServiceReference;
using Kfh.Aurora.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace CreditCardsSystem.Application.Currencies;

public class CurrencyAppService : ICurrencyAppService, IAppService
{
    #region Variables
    private readonly FdrDBContext _fdrDBContext;
    private readonly ILogger<CurrencyAppService> _logger;
    private readonly CreditCardUpdateServicesServiceClient _updateServicesServiceClient;
    private readonly CustomerAccountsServiceClient _customerAccountServiceClient;
    private readonly CurrencyInformationServiceClient _currencyInformationServiceClient;
    private readonly IDistributedCache _cache;
    #endregion

    #region Constructor
    public CurrencyAppService(FdrDBContext fdrDBContext, IIntegrationUtility integrationUtility, IOptions<IntegrationOptions> options, ILogger<CurrencyAppService> logger, IDistributedCache cache)
    {
        _fdrDBContext = fdrDBContext;
        _logger = logger;
        _updateServicesServiceClient = integrationUtility.GetClient<CreditCardUpdateServicesServiceClient>
            (options.Value.Client, options.Value.Endpoints.CreditCardUpdate, options.Value.BypassSslValidation);

        _customerAccountServiceClient = integrationUtility.GetClient<CustomerAccountsServiceClient>(options.Value.Client, options.Value.Endpoints.CustomerAccount, options.Value.BypassSslValidation);

        _currencyInformationServiceClient = integrationUtility.GetClient<CurrencyInformationServiceClient>(options.Value.Client, options.Value.Endpoints.CurrencyInformation, options.Value.BypassSslValidation);
        _cache = cache;
    }

    #endregion

    #region Public Methods

    [HttpGet]
    public async Task<ApiResponseModel<ValidateCurrencyResponse>> ValidateCurrencyRate(ValidateCurrencyRequest request)
    {
        var response = new ApiResponseModel<ValidateCurrencyResponse>();
        await request.ModelValidationAsync(nameof(ValidateCurrencyRate));


        var isValidCurrency = _fdrDBContext.CardCurrencies.AsNoTracking().Any(x => x.CurrencyIsoCode == request.ForeignCurrencyCode);
        if (!isValidCurrency)
            return response.Fail($"Invalid {request.ForeignCurrencyCode}");

        if (request.SourceAmount == 0)
        {
            var minimumBalanceForForeignCurrency = (await _fdrDBContext.ConfigParameters.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ParamName == ConfigurationBase.MinimumBalanceFC))?.ParamValue;
            if (minimumBalanceForForeignCurrency is null) return response.Fail($"Minimum balance couldn't find in configuration for {request.ForeignCurrencyCode}");

            if (decimal.TryParse(minimumBalanceForForeignCurrency, out decimal _sourceAmount))
                request.SourceAmount = _sourceAmount;
        }


        var currencyRateResult = (await _updateServicesServiceClient.validateCreditCardCurrencyRateAsync(new()
        {
            currencyRateRequest = new()
            {
                srcCcyCode = request.SourceCurrencyCode ?? ConfigurationBase.KuwaitCurrency,
                destCcyCode = request.ForeignCurrencyCode,
                srcAmount = Convert.ToDouble(request.SourceAmount),
                srcAmountSpecified = request.SourceAmount > 0,
                destAmount = Convert.ToDouble(request.DestinationAmount),
                destAmountSpecified = request.DestinationAmount > 0
            }
        })).validateCreditCardCurrencyRate;

        if (currencyRateResult is null || currencyRateResult.respCode != "0000") return response.Fail($"Couldn't find currency rate for {request.ForeignCurrencyCode}");

        //TODO : GetPrepaidCardLimit and calculate maxTransLimit = prepaidCardLimitDTO.maxLimit - prepaidCardLimitDTO.availableLimit

        return response.Success(new ValidateCurrencyResponse()
        {
            SourceCurrencyCode = request.SourceCurrencyCode,
            ForeignCurrencyCode = request.ForeignCurrencyCode,
            TransferRate = currencyRateResult.transRate,
            KdTransProfit = currencyRateResult.kdTransProfit,
            DestAmount = Convert.ToDecimal(currencyRateResult.destAmount),
            SrcAmount = Convert.ToDecimal(currencyRateResult.srcAmount)
        });
    }

    [HttpGet]
    public async Task<CardCurrencyDto> GetCardCurrency(int cardType)
    {
        var cardDef = await _fdrDBContext.CardDefs.Include(x => x.CardDefExts).FirstOrDefaultAsync(d => d.CardType == cardType);

        var org = $"{ConfigurationBase.CurrencyConfigCode}{(cardDef.Duality == 7 ? "_VISA" : "")}";

        string? currencyOrg = cardDef.CardDefExts.FirstOrDefault(x => x.CardType == cardType && x.Attribute.ToUpper() == org)?.Value
            ?? throw new ApiException(errors: null, message: "Invalid Currency");

        var cardCurrency = (await _fdrDBContext.CardCurrencies.AsNoTracking()
            .FirstOrDefaultAsync(cc => cc.Org == currencyOrg)
            ?? throw new ApiException(errors: null, message: "Invalid Currency"))?.Adapt<CardCurrencyDto>();

        cardCurrency!.IsForeignCurrency = cardCurrency.CurrencyIsoCode != ConfigurationBase.KuwaitCurrency;

        return cardCurrency;
    }

    [HttpGet]
    public async Task<CardCurrencyDto?> GetCardCurrencyByRequestId(decimal requestId)
    {
        var cardCurrency = await (from req in _fdrDBContext.Requests.AsNoTracking().Where(x => x.RequestId == requestId).Include(x => x.Parameters).AsNoTracking()
                                  join def in _fdrDBContext.CardDefExts.AsNoTracking() on req.CardType equals def.CardType
                                  where def.Attribute.ToUpper() == ConfigurationBase.CurrencyConfigCode
                                  join crncy in _fdrDBContext.CardCurrencies.AsNoTracking() on def.Value equals crncy.Org into _crncyDef
                                  from crncyDef in _crncyDef.DefaultIfEmpty()
                                  select new CardCurrencyDto
                                  {
                                      RequestId = req.RequestId,
                                      CardType = req.CardType,
                                      CurrencyOriginalId = def.Value,
                                      CurrencyIsoCode = crncyDef == null ? ConfigurationBase.KuwaitCurrency : crncyDef.CurrencyIsoCode,
                                      CurrencyDecimalPlaces = crncyDef.CurrencyDecimalPlaces,
                                      CurrencyId = crncyDef.CurrencyId,
                                      CurrencyShortName = crncyDef.CurrencyShortName,
                                      IsForeignCurrency = req.Parameters.Any(x => x.Parameter == "ISSUING_OPTION" && x.Value == Collateral.FOREIGN_CURRENCY_PREPAID_CARDS.ToString())
                                  }).FirstOrDefaultAsync();


        if (cardCurrency == null) return null;

        var corporateCardTypeIds = (await _fdrDBContext.ConfigParameters.AsNoTracking().FirstOrDefaultAsync(x => x.ParamName == ConfigurationBase.CorporateCardTypeIds))?.ParamValue.Split(",") ?? Array.Empty<string>();
        cardCurrency.IsCorporateCard = corporateCardTypeIds.Any(x => x == cardCurrency.CardType.ToString());


        return cardCurrency;
    }

    [NonAction]
    [HttpGet]
    public async Task<ValidateCurrencyResponse?> GetCurrencyRate(string currencyIsoCode)
    {
        var currencyResponse = await ValidateCurrencyRate(new() { ForeignCurrencyCode = currencyIsoCode });
        if (currencyResponse is null || !currencyResponse.IsSuccess)
            throw new ApiException(errors: null, message: "Error happened where calculating the markup charges amount، حدث خطأ عند حساب قيمة الرسوم");
        return currencyResponse?.Data;
    }

    [HttpPost]
    public async Task<ApiResponseModel<ValidateCurrencyResponse>> ValidateSufficientFundForForeignCurrencyCards(int cardType, string accountNumber)
    {
        var response = new ApiResponseModel<ValidateCurrencyResponse>();

        if (string.IsNullOrEmpty(accountNumber))
        {
            response.ValidationErrors.Add(new("accountNumber", "Invalid Account number"));
            return response.Fail("Invalid Account number");
        }

        //TODO: refactor this method by replacing cartType with card currencyISO code

        CardCurrencyDto cardCurrency = await GetCardCurrency(cardType);
        if (!cardCurrency.IsForeignCurrency)
            return response.Success(new());

        var minBalanceFc = Convert.ToDecimal((await _fdrDBContext.ConfigParameters.AsNoTracking().FirstOrDefaultAsync(x => x.ParamName == ConfigurationBase.MinimumBalanceFC))?.ParamValue);

        if (minBalanceFc <= 0)
            throw new ApiException(errors: null, message: $"{minBalanceFc} minimum balance");



        ValidateCurrencyResponse? currencyData = await GetCurrencyRate(cardCurrency.CurrencyIsoCode) ?? throw new ApiException(errors: null, message: "Invalid Currency Data");

        currencyData.AccountOpeningMinBalance = minBalanceFc + ((decimal)currencyData.KdTransProfit);

        AccountDetailsDTO account = ((await _customerAccountServiceClient.viewAccountDetailsAsync(new()
        {
            acct = accountNumber
        }))?.viewAccountDetailsResult) ?? throw new ApiException(errors: null, message: "Invalid Debit Account number");

        if (Convert.ToDecimal(account.availableBalance) < currencyData.AccountOpeningMinBalance)
            throw new ApiException(errors: null, message: $"Selected account balance is less than the minimum opening balance  - {currencyData.AccountOpeningMinBalance}");

        return response.Success(currencyData);
    }

    [NonAction]
    [HttpGet]
    public async Task<ForeignCurrencyResponse?> GetBuyRate(string currencyIsoCode)
    {
        if (string.IsNullOrEmpty(currencyIsoCode) || !Regex.Match(currencyIsoCode, @"[a-zA-Z]{1,3}").Success)
        {
            return null;
        }

        //if card type is charge card
        var foreignCurrencies = (await _currencyInformationServiceClient.getAllForeignCurrenciesAsync(new() { })).getAllForeignCurrenciesResult;
        if (!foreignCurrencies.Any())
            return null;

        var fxDetail = foreignCurrencies.FirstOrDefault(fc => fc.isoCode == currencyIsoCode);
        if (fxDetail == null) return null;


        return new ForeignCurrencyResponse()
        {
            BuyCashRate = Convert.ToDecimal(fxDetail?.buyCashRate),
            SellCashRate = Convert.ToDecimal(fxDetail?.sellCashRate)
        };
    }

    #endregion

}



