using AdoNetCore.AseClient;
using BankingCustomerProfileReference;
using CreditCardsSystem.Data;
using CreditCardsSystem.Domain.Models.BCDPromotions.Groups;
using CreditCardsSystem.Domain.Models.BCDPromotions.Requests;
using CreditCardsSystem.Domain.Models.CardIssuance;
using CreditCardsSystem.Domain.Shared.Enums;
using CreditCardsSystem.Domain.Shared.Interfaces;
using CurrencyInformationServiceReference;
using Dapper;
using Kfh.Aurora.Integration;
using Kfh.Aurora.Organization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using static CreditCardsSystem.Domain.Models.BCDPromotions.Groups.GroupAttributeLookupDto;
using Attribute = CreditCardsSystem.Domain.Models.BCDPromotions.Groups.Attribute;

namespace CreditCardsSystem.Application.BCDPromotions.Groups;

public class GroupsAppService : IGroupsAppService, IAppService
{
    private readonly IRequestMaker<PromotionGroupDto> _promotionRequestsAppService;
    private readonly IRequestMaker<GroupAttributeDto> _groupAttributeRequestsAppService;
    private readonly ILogger<PromotionGroupDto> _logger;
    private readonly IOrganizationClient _organizationClient;
    private readonly FdrDBContext _fdrDbContext;
    private readonly string _connectionString;
    private readonly BankingCustomerProfileServiceClient _customerProfileServiceClient;
    private readonly CurrencyInformationServiceClient _currencyInformationServiceClient;

    public GroupsAppService(IRequestMaker<PromotionGroupDto> promotionRequestsAppService,
        IRequestMaker<GroupAttributeDto> groupAttributeRequestsAppService,
        ILogger<PromotionGroupDto> logger, IConfiguration configuration, IOrganizationClient organizationClient,
        FdrDBContext fdrDbContext, IOptions<IntegrationOptions> options, IIntegrationUtility integrationUtility)
    {
        _promotionRequestsAppService = promotionRequestsAppService;
        _groupAttributeRequestsAppService = groupAttributeRequestsAppService;
        _logger = logger;
        _organizationClient = organizationClient;
        _fdrDbContext = fdrDbContext;
        _currencyInformationServiceClient = integrationUtility.GetClient<CurrencyInformationServiceClient>(options.Value.Client, options.Value.Endpoints.CurrencyInformation, options.Value.BypassSslValidation);
        _customerProfileServiceClient = integrationUtility.GetClient<BankingCustomerProfileServiceClient>(options.Value.Client, options.Value.Endpoints.BankingCustomerProfile, options.Value.BypassSslValidation);
        _connectionString = configuration.GetConnectionString("PhoenixConnection")!;
    }
    [HttpGet]
    public async Task<ApiResponseModel<List<PromotionGroupDto>>> GetGroups()
    {
        var groups = await _fdrDbContext.PromotionGroups
        .Join(_fdrDbContext.Promotions, g => g.PromotionId, p => p.PromotionId, (group, promotion) =>
        new PromotionGroupDto()
        {
            PromotionId = promotion.PromotionId,
            PromotionName = promotion.PromotionName,
            GroupID = group.GroupId,
            Description = group.Description,
            Status = group.Status,
            IsLocked = group.Islocked
        }).OrderBy(g => g.PromotionId)
                  .ThenBy(g => g.Description)
                  .ToListAsync();

        return new ApiResponseModel<List<PromotionGroupDto>>().Success(groups);
    }

    [HttpGet]
    public async Task<ApiResponseModel<List<GroupAttributeDto>>> GetGroupsWithAttributes()
    {
        var attributes = await _fdrDbContext.GroupAttributes.Select(a => new GroupAttributeDto
        {
            GroupID = a.GroupId,
            AttributeID = a.AttributeId,
            AttributeType = a.AttributeType,
            AttributeValue = a.AttributeValue,
            IsLocked = a.Islocked

        }).ToListAsync();

        var groups = (await GetGroups()).Data ?? new();

        var groupsAttributes = groups.Join(attributes, g => g.GroupID, a => a.GroupID, (group, attribute) =>
            new GroupAttributeDto()
            {
                GroupID = group.GroupID,
                BackupGroupId = group.GroupID,
                GroupName = group.Description!,
                IsLocked = attribute.IsLocked,
                AttributeID = attribute.AttributeID,
                BackupAttributeId = attribute.AttributeID,
                AttributeType = attribute.AttributeType,
                AttributeValue = attribute.AttributeValue
            })
            .OrderByDescending(a => a.GroupID)
            .ToList();


        return new ApiResponseModel<List<GroupAttributeDto>>().Success(groupsAttributes);

    }

    [HttpGet]
    public async Task<List<GroupAttributeLookupDto>> GetAttributeLookupByType(GroupAttributeType type)
    {
        return type switch
        {
            GroupAttributeType.Nationality => await GetNationality(),
            GroupAttributeType.ApplType => await GetApplType(),
            GroupAttributeType.AcctType => await GetAcctType(),
            GroupAttributeType.CardType => await GetCardTypes(),
            GroupAttributeType.Status => await GetStatus(),
            GroupAttributeType.BranchNo => await GetBranchNo(),
            GroupAttributeType.Currency => await GetCurrency(),
            GroupAttributeType.CustomerClass => await GetCustomerClass(),
            GroupAttributeType.CustomerType => await GetCustomerType(),
            GroupAttributeType.Religion => await GetReligions(),
            GroupAttributeType.Gender => await GetGender(),
            GroupAttributeType.Joint => await GetJoint(),
            GroupAttributeType.RimType => await GetRimTypes(),
            GroupAttributeType.Location => await GetLocations(),
            GroupAttributeType.NotSet => new List<GroupAttributeLookupDto>(),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    [HttpGet]
    public async Task<List<GroupAttributeLookupDto>> GetAllAttributesLookups()
    {
        var attributes = await Task.WhenAll(
            GetNationality(),
                        GetApplType(),
                        GetAcctType(),
                        GetCardTypes(),
                        GetStatus(),
                        GetBranchNo(),
                        GetCurrency(),
                        GetCustomerClass(),
                        GetCustomerType(),
                        GetReligions(),
                        GetGender(),
                        GetJoint(),
                        GetRimTypes(),
                        GetLocations());

        var result = attributes.SelectMany(a => a).ToList();
        return result;
    }

    [HttpGet]
    public async Task<List<Attribute>> GetAttributesList()
    {

        var attributes = new List<Attribute>()
        {
            new() { DisplayedName = "Customer Rim Type" , Type = "rim_type" , Id = (int)GroupAttributeLookupDto.GroupAttributeType.RimType },
            new() { DisplayedName = "Customer Nationality" , Type = "race_id" , Id = (int)GroupAttributeLookupDto.GroupAttributeType.Nationality },
            new() { DisplayedName = "Customer Religion", Type = "risk_code" , Id = (int)GroupAttributeLookupDto.GroupAttributeType.Religion },
            new() { DisplayedName = "Prerequisite Card", Type = "Prerequisite_Card" , Id = (int)GroupAttributeLookupDto.GroupAttributeType.CardType },
            new() { DisplayedName = "Account Status", Type = "status" , Id = (int)GroupAttributeLookupDto.GroupAttributeType.Status },
            new() { DisplayedName = "Account Application Type", Type = "appl_type" , Id = (int)GroupAttributeLookupDto.GroupAttributeType.ApplType },
            new() { DisplayedName = "Customer Sex", Type = "sex" , Id = (int)GroupAttributeLookupDto.GroupAttributeType.Gender },
            new() { DisplayedName = "Joint Account", Type = "joint" , Id = (int)GroupAttributeLookupDto.GroupAttributeType.Joint },
            new() { DisplayedName = "Account Type", Type = "acct_type" , Id = (int)GroupAttributeLookupDto.GroupAttributeType.AcctType },
            new() { DisplayedName = "Account Branch",  Type = "branch_no" , Id = (int)GroupAttributeLookupDto.GroupAttributeType.BranchNo },
            new() { DisplayedName = "Account Currency", Type = "crncy_id" , Id = (int)GroupAttributeLookupDto.GroupAttributeType.Currency },
            new() { DisplayedName = "Customer Class", Type = "class_code" , Id = (int)GroupAttributeLookupDto.GroupAttributeType.CustomerClass },
            new() { DisplayedName = "Customer Type", Type = "sic_code" , Id = (int)GroupAttributeLookupDto.GroupAttributeType.CustomerType },
            new() { DisplayedName = "User Branch", Type = "User_Branch" , Id = (int)GroupAttributeLookupDto.GroupAttributeType.Location },
            new() { DisplayedName = "Supplementary Charge Card", Type = "Supplementary_Charge_Card" , Id = (int)GroupAttributeLookupDto.GroupAttributeType.NotSet },
            new() { DisplayedName = "Min. Age" , Type = "min_age" ,  Id = (int)GroupAttributeLookupDto.GroupAttributeType.NotSet },
            new() { DisplayedName = "Max. Age", Type = "max_age" , Id = (int)GroupAttributeLookupDto.GroupAttributeType.NotSet },
            new() { DisplayedName = "Min. Account Open Date", Type = "min_open_date" , Id = (int)GroupAttributeLookupDto.GroupAttributeType.NotSet },
            new() { DisplayedName = "Max. Account Open Date", Type = "max_open_date" , Id = (int)GroupAttributeLookupDto.GroupAttributeType.NotSet },
            new() { DisplayedName = "Account Open Date Period", Type = "open_date_period" , Id = (int)GroupAttributeLookupDto.GroupAttributeType.NotSet },
            new() { DisplayedName = "Min.Account Total Balance", Type = "min_total_balance" , Id = (int)GroupAttributeLookupDto.GroupAttributeType.NotSet },
            new() { DisplayedName = "Max.Account Total Balance",  Type = "max_total_balance" , Id = (int)GroupAttributeLookupDto.GroupAttributeType.NotSet },
            new() { DisplayedName = "Min.Account Available Balance" , Type = "min_available_balance" , Id = (int)GroupAttributeLookupDto.GroupAttributeType.NotSet },
            new() { DisplayedName = "Max.Account Available Balance" , Type = "max_available_balance" , Id = (int)GroupAttributeLookupDto.GroupAttributeType.NotSet },
            new() { DisplayedName = "BCD Flag", Type = "BCD_FLAG", Id = (int)GroupAttributeLookupDto.GroupAttributeType.NotSet }
        };
        return await Task.FromResult(attributes);

    }

    [HttpPost]
    public async Task<ApiResponseModel<AddRequestResponse>> AddGroupRequest([FromBody] RequestDto<PromotionGroupDto> request)
    {
        await using var transaction = await _fdrDbContext.Database.BeginTransactionAsync();
        try
        {

            if (request.ActivityType == (int)ActivityType.Add)
            {
                request.Title = "New Promotion Group";
                request.Description = $"New promotion group has been added for '{request.NewData.PromotionName}'";
            }

            if (request.ActivityType == (int)ActivityType.Edit)
            {
                request.Title = "Updated Promotion Group";
                request.Description = $"The promotion group for '{request.NewData.PromotionName}' has been updated";
            }

            if (request.ActivityType == (int)ActivityType.Delete)
            {
                request.Title = "Deleted Promotion Group";
                request.Description = $"The promotion group for '{request.NewData.PromotionName}' has been deleted";
            }

            await _promotionRequestsAppService.AddRequest(request);

            switch (request.ActivityType)
            {
                case (int)Domain.Shared.Enums.ActivityType.Edit:
                    await UpdateLockStatus(request.OldData.GroupID);
                    break;

                case (int)Domain.Shared.Enums.ActivityType.Delete:
                    await UpdateLockStatus(request.NewData.GroupID);
                    break;
            }

            await transaction.CommitAsync();
            return new ApiResponseModel<AddRequestResponse>().Success(null);

        }
        catch (System.Exception e)
        {
            await transaction.RollbackAsync();
            _logger.LogError(e, nameof(AddGroupRequest));
            return new ApiResponseModel<AddRequestResponse>().Fail("something went wrong during adding the request");

        }
    }

    [HttpPost]
    public async Task<ApiResponseModel<AddRequestResponse>> AddGroupAttributeRequest([FromBody] RequestDto<GroupAttributeDto> request)
    {
        await using var transaction = await _fdrDbContext.Database.BeginTransactionAsync();
        try
        {

            if (request.ActivityType == (int)ActivityType.Add)
            {
                request.Title = "New Promotion Group";
                request.Description = $"New promotion group has been added for group '{request.NewData.GroupName}'";
            }

            if (request.ActivityType == (int)ActivityType.Edit)
            {
                request.Title = "Updated Promotion Group";
                request.Description = $"The promotion group for '{request.NewData.GroupName}' has been updated";
            }

            if (request.ActivityType == (int)ActivityType.Delete)
            {
                request.Title = "Deleted Promotion Group";
                request.Description = $"The promotion group for '{request.NewData.GroupName}' has been deleted";
            }

            await _groupAttributeRequestsAppService.AddRequest(request);

            switch (request.ActivityType)
            {
                case (int)Domain.Shared.Enums.ActivityType.Edit:
                    await UpdateAttributeLockStatus(request.OldData.AttributeID);
                    break;

                case (int)Domain.Shared.Enums.ActivityType.Delete:
                    await UpdateAttributeLockStatus(request.NewData.AttributeID);
                    break;
            }

            await transaction.CommitAsync();
            return new ApiResponseModel<AddRequestResponse>().Success(null);

        }
        catch (System.Exception e)
        {
            await transaction.RollbackAsync();
            _logger.LogError(e, nameof(AddGroupRequest));
            return new ApiResponseModel<AddRequestResponse>().Fail("something went wrong during adding the request");

        }
    }

    [HttpPost]
    public async Task<bool> ValidateGroupDesc([FromBody] PromotionGroupDto group)
    {
        var isDescExist = await _fdrDbContext.PromotionGroups.AnyAsync(g => g.PromotionId == group.PromotionId &&
                                                                              g.Description!.Trim().ToLower() == group.Description.Trim().ToLower() &&
                                                                              g.GroupId != group.GroupID);
        return isDescExist;
    }

    [HttpPost]
    public async Task<bool> ValidateGroupAttribute([FromBody] GroupAttributeDto groupAttribute)
    {
        var isAttributeExist = await _fdrDbContext.GroupAttributes.AnyAsync(g => g.GroupId == groupAttribute.GroupID &&
                                                                         g.AttributeType.Trim() == groupAttribute.AttributeType.Trim() &&
                                                                         g.AttributeId != groupAttribute.AttributeID);
        return isAttributeExist;
    }

    [HttpGet]
    public async Task<List<GroupAttributeLookupDto>> GetCustomerClass()
    {
        var classes = await GetLookUpDataByType("RimClasses");

        var allAttribute = new GroupAttributeLookupDto() { Attribute = "All", Value = "0" };
        classes.Add(allAttribute);

        classes.ForEach(v => v.AttributeType = GroupAttributeType.CustomerClass);
        return classes;
    }

    [HttpGet]
    public async Task<List<GroupAttributeLookupDto>> GetLocations()
    {
        var lst = new List<GroupAttributeLookupDto>();
        var usersBranches = (await _organizationClient.GetBranches()).Where(b => b.BranchId != 0).ToList();

        var allAttribute = new GroupAttributeLookupDto() { Attribute = "All", Value = "0" };
        lst.Add(allAttribute);

        foreach (var branch in usersBranches)
        {
            var locationNameEn = GetLocationNameEn(branch.Name);
            var attribute = new GroupAttributeLookupDto() { Attribute = locationNameEn, Value = branch.BranchId.ToString() };
            lst.Add(attribute);
        }

        lst.ForEach(x => x.AttributeType = GroupAttributeLookupDto.GroupAttributeType.Location);
        return lst;
    }

    [HttpPut]
    public async Task<ApiResponseModel<AddRequestResponse>> UpdateLockStatus(long id)
    {
        var result = await _fdrDbContext.PromotionGroups
            .Where(g => g.GroupId == id)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Islocked, p => true));

        return new ApiResponseModel<AddRequestResponse>() { IsSuccess = true };
    }

    [HttpGet]
    public async Task<ApiResponseModel<AddRequestResponse>> UpdateAttributeLockStatus(long id)
    {
        var result = await _fdrDbContext.GroupAttributes
            .Where(g => g.AttributeId == id)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Islocked, p => true));

        return new ApiResponseModel<AddRequestResponse>() { IsSuccess = true };
    }

    //***************************************private methods*****************************

    private async Task<List<GroupAttributeLookupDto>> GetLookUpDataByType(string type)
    {
        var lookup = new List<GroupAttributeLookupDto>();

        var response = (await _customerProfileServiceClient.getBankingStaticDataAsync(new getBankingStaticDataRequest
        {
            dataTypeName = type

        })).getBankingStaticDataResult;

        if (response != null!)
        {
            lookup = response.dataTypeDetailList.Select(d => new GroupAttributeLookupDto
            {
                Attribute = d.dataNameEn,
                Value = d.dataTypeID
            }).ToList();
        }

        return lookup;

    }

    private async Task<List<GroupAttributeLookupDto>> GetLookUpDataByType(GroupAttributeType type, string query)
    {
        var connection = new AseConnection(_connectionString);
        connection.Open();
        var result = (await connection.QueryAsync<GroupAttributeLookupDto>(query)).ToList();
        result.ForEach(x => x.AttributeType = type);
        return result;
    }

    private async Task<List<GroupAttributeLookupDto>> GetNationality()
    {
        var lookup = await GetLookUpDataByType("Nationality");
        lookup.ForEach(v => v.AttributeType = GroupAttributeType.Nationality);
        return lookup;
    }

    private async Task<List<GroupAttributeLookupDto>> GetRimTypes()
    {
        var lst = new List<GroupAttributeLookupDto>(new List<GroupAttributeLookupDto>
        {
            new(){ Attribute = "Personal", Value = "Personal" },
            new(){ Attribute = "Non Personal", Value = "NonPersonal" }
        });

        lst.ForEach(x => x.AttributeType = GroupAttributeLookupDto.GroupAttributeType.RimType);
        return await Task.FromResult(lst);
    }

    private async Task<List<GroupAttributeLookupDto>> GetReligions()
    {
        //const string query = """
        //                     select risk_code as VALUE, rtrim(ltrim(description)) as ATTRIBUTE
        //                                                                 from phoenix..ad_gb_risk where status = 'Active' order by ATTRIBUTE
        //                     """;


        //return await GetLookUpDataByType(GroupAttributeType.Religion, query);

        var lookup = await GetLookUpDataByType("Religions");
        lookup.ForEach(v => v.AttributeType = GroupAttributeType.Religion);
        return lookup;
    }

    private async Task<List<GroupAttributeLookupDto>> GetCustomerType()
    {
        //const string query = """
        //                     select sic_code as VALUE, rtrim(ltrim(description)) as ATTRIBUTE
        //                                                                 from phoenix..ad_gb_sic where status = 'Active' order by ATTRIBUTE
        //                     """;
        //return await GetLookUpDataByType(GroupAttributeType.CustomerType, query);

        var lookup = await GetLookUpDataByType("CustomerTypes");
        lookup.ForEach(v => v.AttributeType = GroupAttributeType.CustomerType);
        return lookup;

    }

    private string GetLocationNameEn(string locationName)
    {
        string locationNameEn = locationName;
        string location = Regex.Replace(locationName, @"[^\u001F-\u007F]", string.Empty);
        locationNameEn = Regex.Replace(location, @"[\u0030-\u0039]", string.Empty);
        return locationNameEn.Trim();
    }

    private async Task<List<GroupAttributeLookupDto>> GetGender()
    {
        var lst = new List<GroupAttributeLookupDto>(new List<GroupAttributeLookupDto>
        {
            new() { Attribute = "Male", Value = "M" },
            new() { Attribute = "Female", Value = "F" }
        });

        lst.ForEach(x => x.AttributeType = GroupAttributeLookupDto.GroupAttributeType.Gender);
        return await Task.FromResult(lst);
    }

    private async Task<List<GroupAttributeLookupDto>> GetApplType()
    {
        const string query = """
                             select rtrim(ltrim(appl_type)) as VALUE, rtrim(ltrim(description)) as ATTRIBUTE
                                                                         from phoenix..ad_gb_appl_type  order by ATTRIBUTE
                             """;
        return await GetLookUpDataByType(GroupAttributeType.ApplType, query);
    }

    private async Task<List<GroupAttributeLookupDto>> GetCardTypes()
    {
        var lst = new List<GroupAttributeLookupDto>();
        var cardsTypes = (await _fdrDbContext.CardDefs.OrderBy(c => c.Name).ToListAsync()).Adapt<List<CardDefinitionDto>>();

        var cardTypesLookup = cardsTypes.Select(c => new GroupAttributeLookupDto
        {
            Attribute = c.Name,
            Value = c.CardType.ToString()
        });
        lst.AddRange(cardTypesLookup);
        lst.ForEach(x => x.AttributeType = GroupAttributeLookupDto.GroupAttributeType.CardType);
        return lst;
    }

    private async Task<List<GroupAttributeLookupDto>> GetAcctType()
    {
        //const string query = """
        //                     select rtrim(ltrim(acct_type)) as VALUE, rtrim(ltrim(description)) as ATTRIBUTE
        //                                                                 from phoenix..ad_gb_acct_type where status = 'Active'  order by ATTRIBUTE
        //                     """;
        //return await GetLookUpDataByType(GroupAttributeType.AcctType, query);

        var lookup = await GetLookUpDataByType("AccountTypes");
        lookup.ForEach(v => v.AttributeType = GroupAttributeType.AcctType);
        return lookup;

    }

    private async Task<List<GroupAttributeLookupDto>> GetCurrency()
    {
        //const string query = """
        //                     select rtrim(ltrim(iso_code)) as VALUE, rtrim(ltrim(description)) as ATTRIBUTE
        //                                                                 from phoenix..ad_gb_crncy where status = 'Active'  order by ATTRIBUTE
        //                     """;
        //return await GetLookUpDataByType(GroupAttributeType.Currency, query);

        var response = (await _currencyInformationServiceClient.getAllForeignCurrenciesAsync(new getAllForeignCurrenciesRequest()))
                                         .getAllForeignCurrenciesResult;

        var lookup = new List<GroupAttributeLookupDto>();
        if (response != null)
        {
            lookup = response.Select(c => new GroupAttributeLookupDto
            {
                Attribute = c.nameEnglish,
                AttributeType = GroupAttributeType.Currency,
                Value = c.isoCode
            }).ToList();
        }

        return lookup;
    }

    private async Task<List<GroupAttributeLookupDto>> GetBranchNo()
    {
        //const string query = """
        //                     select branch_no as VALUE, rtrim(ltrim(name_1)) as ATTRIBUTE
        //                                                                 from phoenix..ad_gb_branch where status = 'Active'  order by ATTRIBUTE
        //                     """;
        //return await GetLookUpDataByType(GroupAttributeType.BranchNo, query);

        var lookup = await GetLookUpDataByType("KFHBranches");
        lookup.ForEach(v => v.AttributeType = GroupAttributeType.BranchNo);
        return lookup;

    }

    private async Task<List<GroupAttributeLookupDto>> GetStatus()
    {
        var lst = new List<GroupAttributeLookupDto>
        {
            new() { Attribute = "Active", Value = "Active" },
            new() { Attribute = "RenewPending", Value = "RenewPending" },
            new() { Attribute = "Dormant", Value = "Dormant" },
            new() { Attribute = "Locked", Value = "Locked" },
            new() { Attribute = "Restricted", Value = "Restricted" },
            new() { Attribute = "Closed", Value = "Closed" },
            new() { Attribute = "Escheated", Value = "Escheated" }
        };

        lst.ForEach(x => x.AttributeType = GroupAttributeLookupDto.GroupAttributeType.Status);
        return await Task.FromResult(lst);
    }

    private async Task<List<GroupAttributeLookupDto>> GetJoint()
    {
        var lst = new List<GroupAttributeLookupDto>
        {
            new() { Attribute = "true", Value = "True" },
            new() { Attribute = "false", Value = "False" }
        };

        lst.ForEach(x => x.AttributeType = GroupAttributeLookupDto.GroupAttributeType.Joint);
        return await Task.FromResult(lst);

    }

}