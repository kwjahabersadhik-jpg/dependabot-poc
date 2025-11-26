using BankingCustomerProfileReference;
using CreditCardsSystem.Data;
using CreditCardsSystem.Data.Models;
using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Models.Card;
using CreditCardsSystem.Domain.Models.CardIssuance;
using CreditCardsSystem.Domain.Models.CoBrand;
using Kfh.Aurora.Integration;
using Kfh.Aurora.Organization;
using Microsoft.EntityFrameworkCore;

namespace CreditCardsSystem.Application.LookupService;

public class LookupAppService : BaseApiResponse, ILookupAppService, IAppService
{
    #region Variables
    private readonly FdrDBContext _fdrDBContext;
    private readonly IIntegrationUtility _integrationUtility;
    private readonly IOptions<IntegrationOptions> _options;
    private readonly IDistributedCache _cache;
    private readonly IOrganizationClient _organizationClient;

    private readonly BankingCustomerProfileServiceClient _bankingCustomerProfileServiceClient;
    #endregion

    #region Constructor
    public LookupAppService(ApplicationDbContext applicationDb, FdrDBContext fdrDBContext, IIntegrationUtility integrationUtility,
   IOptions<IntegrationOptions> options, IDistributedCache cache, IOrganizationClient organizationClient)
    {
        _fdrDBContext = fdrDBContext;
        _integrationUtility = integrationUtility;
        _options = options;
        _cache = cache;
        _bankingCustomerProfileServiceClient = _integrationUtility.GetClient<BankingCustomerProfileServiceClient>(_options.Value.Client, _options.Value.Endpoints.BankingCustomerProfile, _options.Value.BypassSslValidation);
        _organizationClient = organizationClient;
    }
    #endregion

    #region Public Methods

    [HttpGet]
    public async Task<ApiResponseModel<List<Branch>>> GetAllBranches()
    {
        var branches = (await _organizationClient.GetBranches())?.OrderBy(x => x.BranchId).ToList();
        return Success(branches ?? []);
    }


    [HttpGet]
    public async Task<ApiResponseModel<List<CompanyDto>>> GetAllCompanies()
    {
        var response = new ApiResponseModel<List<CompanyDto>>();

        var companies = await _fdrDBContext.Companies.AsNoTracking().ProjectToType<CompanyDto>().ToListAsync();

        return response.Success(companies);
    }

    [HttpGet]
    public async Task<ApiResponseModel<List<CardCurrencyDto>>> GetCardCurrencies()
    {
        var result = new ApiResponseModel<List<CardCurrencyDto>>();

        var cardCurrenciesFromCache = await _cache.GetJsonAsync<List<CardCurrencyDto>>("CardCurrencies");
        if (cardCurrenciesFromCache != null) { return result.Success(cardCurrenciesFromCache); }

        var currenciesResponse = await _fdrDBContext.CardCurrencies.AsNoTracking().Select(x => new CardCurrencyDto()
        {
            CurrencyId = x.CurrencyId,
            CurrencyIsoCode = x.CurrencyIsoCode,
            CurrencyOriginalId = x.Org,
            CurrencyShortName = x.CurrencyShortName
        }).ToListAsync();

        await _cache.SetJsonAsync("CardCurrencies", currenciesResponse, 60);
        return result.Success(currenciesResponse);
    }

    [HttpGet]
    public async Task<ApiResponseModel<List<CardDefinitionDto>>> GetAllProducts()
    {
        var result = new ApiResponseModel<List<CardDefinitionDto>>();
        var allProductsFromCache = await _cache.GetJsonAsync<List<CardDefinitionDto>>("AllProducts");
        if (allProductsFromCache != null) { return result.Success(allProductsFromCache); }

        var cardDefinitionResponse = await (from cardDef in _fdrDBContext.CardDefs.AsNoTracking()
                                            let currencyId = cardDef.CardDefExts.FirstOrDefault(x => x.Attribute.ToUpper() == "ORG")
                                            select new CardDefinitionDto
                                            {
                                                BinNo = cardDef.BinNo,
                                                ProductId = cardDef.CardType,
                                                Duality = cardDef.Duality,
                                                Fees = cardDef.Fees,
                                                Installments = cardDef.Installments,
                                                Islocked = cardDef.Islocked,
                                                MerchantAcct = cardDef.MerchantAcct,
                                                MonthlyMaxDue = cardDef.MonthlyMaxDue,
                                                Name = cardDef.Name,
                                                SystemNo = cardDef.SystemNo,
                                                MaxLimit = decimal.Parse(cardDef.MaxLimit ?? "0"), //setting 0 incase of null value and will be considering as prepaid card
                                                MinLimit = decimal.Parse(cardDef.MinLimit ?? "0"),
                                                CurrencyId = currencyId.Value
                                            }).ToListAsync();
        await _cache.SetJsonAsync("AllProducts", cardDefinitionResponse, 60);

        return result.Success(cardDefinitionResponse);
    }

    [HttpGet]
    public async Task<ApiResponseModel<List<AreaCodesDto>>> GetAreaCodes()
    {
        var response = new ApiResponseModel<List<AreaCodesDto>>();

        // Have to confirm with buiness whether area code needs to take from oracle or integration service

        //var areaCodes = await (from areacode in _fdrDBContext.AreaCodes.AsNoTracking()
        //                       select new AreaCodesDto
        //                       {
        //                           AreaId = areacode.AreaId,
        //                           AreaName = areacode.AreaName,
        //                           ProvinceId = areacode.ProvinceId ?? 0
        //                       }).ToListAsync();


        var bankingStaticData = await _bankingCustomerProfileServiceClient.getBankingStaticDataAsync(new getBankingStaticDataRequest() { dataTypeName = "Regions" });
        if (bankingStaticData == null) return response.Fail("Regions not found!");

        var bankingStaticDataResult = bankingStaticData.getBankingStaticDataResult;
        if (bankingStaticDataResult == null) return response.Fail("Regions not found!");

        var areaCodesFromCache = await _cache.GetJsonAsync<List<AreaCodesDto>>("AreaCodes");
        if (areaCodesFromCache != null) { return response.Success(areaCodesFromCache); }

        var areaCodes = (from areacode in bankingStaticDataResult.dataTypeDetailList
                         select new AreaCodesDto
                         {
                             AreaId = int.Parse(areacode.dataTypeID),
                             AreaName = areacode.dataNameEn
                         }).ToList();

        await _cache.SetJsonAsync("AreaCodes", areaCodes, 60);

        return response.Success(areaCodes);
    }

    [HttpGet]
    public async Task<ApiResponseModel<List<Relationship>>> GetRelationships()
    {
        var response = new ApiResponseModel<List<Relationship>>();
        try
        {

            var relationshipfromCache = await _cache.GetJsonAsync<List<Relationship>>("RelationShips");
            if (relationshipfromCache != null) { return response.Success(relationshipfromCache); }

            var fdrRelations = await _fdrDBContext.Relationships.AsNoTracking().ToListAsync();

            if (!fdrRelations.Any()) return response.Fail("Relationships not found!");

            await _cache.SetJsonAsync("RelationShips", fdrRelations, 60);
            return response.Success(fdrRelations);
        }
        catch (System.Exception e)
        {
            return response.Fail(e.Message);
        }


    }

    [HttpGet]
    public async Task<ApiResponseModel<CardStatusList>> GetCardStatus(StatusType type = StatusType.All)
    {
        CardStatusList statusList = new();

        if (type is StatusType.External or StatusType.All)
            statusList.ExternalStatus = await _fdrDBContext.ExternalStatuses.AsNoTracking().ProjectToType<CardStatusDto>().ToListAsync();

        if (type is StatusType.Internal or StatusType.All)
            statusList.InternalStatus = await _fdrDBContext.InternalStatuses.AsNoTracking().ProjectToType<CardStatusDto>().ToListAsync(); ;

        return new ApiResponseModel<CardStatusList>().Success(statusList);
    }


    #endregion

}
