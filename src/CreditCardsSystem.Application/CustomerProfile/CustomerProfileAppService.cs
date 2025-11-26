using BankingCustomerProfileReference;
using CreditCardsSystem.Data;
using CreditCardsSystem.Data.Models;
using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Models.Access;
using CreditCardsSystem.Domain.Models.Card;
using CreditCardsSystem.Domain.Models.Customer;
using CreditCardsSystem.Utility.Crypto;
using CreditCardsSystem.Utility.Extensions;
using CreditCardTransactionInquiryServiceReference;
using CustomerAccountsServiceReference;
using HrServiceReference;
using Kfh.Aurora.Auth;
using Kfh.Aurora.Common.Shared.Interfaces.Customer;
using Kfh.Aurora.Integration;
using Kfh.Aurora.Logging;
using Kfh.Aurora.Organization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using Exception = System.Exception;


namespace CreditCardsSystem.Application.CustomerProfile;

public class CustomerProfileAppService : BaseApiResponse, ICustomerProfileAppService, IAppService
{
    private readonly IDistributedCache _cache;
    private readonly FdrDBContext _fdrDBContext;
    private readonly BankingCustomerProfileServiceClient _customerProfileServiceClient;
    private readonly CreditCardInquiryServicesServiceClient _cardInquiryServicesServiceClient;
    private readonly CustomerAccountsServiceClient _customerAccountsServiceClient;
    private readonly HrServiceClient _hrServiceClient;
    private readonly IOrganizationClient _organizationClient;
    private readonly ILookupAppService _lookupAppService;
    private readonly IConfigurationAppService _configurationAppService;

    private readonly ILogger<CustomerProfileAppService> _logger;
    private readonly IAuditLogger<CustomerProfileAppService> _auditLogger;
    private readonly IAuthManager _authManager;
    private readonly ICustomerProfileCommonApi _customerProfileCommonApi;
    public CustomerProfileAppService(
        IOptions<IntegrationOptions> options,
        IDistributedCache cache,
        IIntegrationUtility integrationUtility,
        FdrDBContext fdrDBContext,
        ILogger<CustomerProfileAppService> logger
        , IOrganizationClient organizationClient,
        ILookupAppService lookupAppService,
        IAuditLogger<CustomerProfileAppService> auditLogger,
        IConfigurationAppService configurationAppService,
        IAuthManager authManager,
        ICustomerProfileCommonApi customerProfileCommonApi)
    {
        _customerProfileServiceClient = integrationUtility.GetClient<BankingCustomerProfileServiceClient>(options.Value.Client, options.Value.Endpoints.BankingCustomerProfile, options.Value.BypassSslValidation);
        _cardInquiryServicesServiceClient = integrationUtility.GetClient<CreditCardInquiryServicesServiceClient>(options.Value.Client, options.Value.Endpoints.CreditCardTransactionInquiry, options.Value.BypassSslValidation);
        _customerAccountsServiceClient = integrationUtility.GetClient<CustomerAccountsServiceClient>(options.Value.Client, options.Value.Endpoints.CustomerAccount, options.Value.BypassSslValidation);
        _hrServiceClient = integrationUtility.GetClient<HrServiceClient>(options.Value.Client, options.Value.Endpoints.Hr, options.Value.BypassSslValidation);

        _organizationClient = organizationClient;
        _lookupAppService = lookupAppService;
        _auditLogger = auditLogger;
        _configurationAppService = configurationAppService;
        _authManager = authManager;
        _cache = cache;
        _fdrDBContext = fdrDBContext;
        _logger = logger;
        this._customerProfileCommonApi = customerProfileCommonApi;
    }


    #region Public Methods

    [HttpPost]
    public async Task<ApiResponseModel<List<CreditCardLiteDto>>> GetCustomerCardsLite([FromBody] CustomerProfileSearchCriteria? searchCriteria)
    {
        var response = new ApiResponseModel<List<CreditCardLiteDto>>();

        if (searchCriteria == null)
            return response.Fail("Please enter search criteria");


        return response.Success(await GetCustomerCardsLite(searchCriteria.CivilId));

    }

    [HttpPost]
    public async Task<ApiResponseModel<List<CreditCardDto>>> GetCustomerCards([FromBody] CustomerProfileSearchCriteria? searchCriteria)
    {
        if (searchCriteria == null)
            return new ApiResponseModel<List<CreditCardDto>>().Fail("Please enter search criteria");

        return new ApiResponseModel<List<CreditCardDto>>().Success(await GetCustomerCards(searchCriteria.CivilId));
    }

    [HttpPost]
    public async Task<ApiResponseModel<List<CreditCardDto>>> GetAllCards([FromBody] CustomerProfileSearchCriteria? searchCriteria)
    {
        if (searchCriteria == null)
            return new ApiResponseModel<List<CreditCardDto>>().Fail("Please enter search criteria");

        return new ApiResponseModel<List<CreditCardDto>>().Success(await GetAllCards(searchCriteria.CivilId));
    }


    [HttpGet]
    public async Task<ApiResponseModel<CreditCardDto>> GetCustomerCard(string? requestId)
    {
        if (requestId == null)
            return new ApiResponseModel<CreditCardDto>().Fail("Please enter search criteria");

        if (!decimal.TryParse(requestId, out decimal _requestId))
            return new ApiResponseModel<CreditCardDto>().Fail("Invalid  requestId");

        return new ApiResponseModel<CreditCardDto>().Success(await GetCustomerCard(_requestId));
    }

    [HttpGet]
    public async Task<ApiResponseModel<BalanceCardStatusDetails>> GetBalanceStatusCardDetail(string cardNumber)
    {
        BalanceCardStatusDetails result = new();
        try
        {
            var card = await _cardInquiryServicesServiceClient.getBalanceStatusCardDetailAsync(new() { cardNo = cardNumber });

            return Success(card.getBalanceStatusCardDetailResult.Adapt<BalanceCardStatusDetails>());
        }

        catch (System.Exception ex)
        {
            result.IsCardNotFound = ex.Message.ToLower().Contains("credit card was not found");
            if (!result.IsCardNotFound)
                throw;

            return new ApiResponseModel<BalanceCardStatusDetails>().Fail(GlobalResources.CardNumberNotFoundInRemoteHost);
        }
    }

    [HttpGet]
    public async Task<CreditCardDto> GetCreditCardBalances([FromBody] CreditCardDto card)
    {
        try
        {
            BalanceCardStatusDetails? creditCardDetail = null;

            if (CardTypesEligibleForFdBalanceRequest(card.CardNumber, card.StatusId))
            {
                creditCardDetail = (await GetBalanceStatusCardDetail(card.CardNumber))?.Data;
            }

            var products = await GetCardsProducts();

            card.AvailableLimit = creditCardDetail?.AvailableLimit;
            card.BalanceAmount = GetCardBalance(creditCardDetail, Int32.Parse(card.CardType), card.ApprovedLimit, products);
            card.OverDueAmount = creditCardDetail?.DelinquentAmount;

            return card;
        }
        catch (System.Exception ex)
        {
            _logger.LogError(nameof(GetCreditCardBalances), ex);
            throw;
        }
    }


    async Task<List<CreditCardProductsDto>> GetCardsProducts()
    {
        var cached = await _cache.GetJsonAsync<List<CreditCardProductsDto>>("cardProducts");
        if (cached != null)
        {
            return cached;
        }

        var allProducts = await _cardInquiryServicesServiceClient.getAllProductsAsync(new getAllProductsRequest());

        var products = allProducts.getAllProductsResult.Select(y => (creditCardProductsDTO)y).ToList();

        var cards = products.Select(item => new CreditCardProductsDto
        {
            CardType = int.Parse(item.cardType),
            Name = item.name,
            ArabicName = item.arabicName,
            MaxLimit = Convert.ToDecimal(item.maxLimit),
            MinLimit = Convert.ToDecimal(item.minLimit),
        }
        ).ToList();

        await _cache.SetJsonAsync("cardProducts", cards, 60);
        return cards;
    }

    [HttpPost]
    public async Task<ApiResponseModel<Profile>> CreateCustomerProfileInFdR([FromBody] ProfileDto profile)
    {
        if (!_authManager.HasPermission(Permissions.CustomerProfile.Edit()))
            return Failure<Profile>(GlobalResources.UnAuthorized);

        var profileEntity = profile.Adapt<Profile>();
        var customerProfile = _fdrDBContext.Profiles.AsNoTracking().FirstOrDefault(x => x.CivilId == profile.CivilId);

        if (customerProfile == null)
            await _fdrDBContext.Profiles.AddAsync(profileEntity);
        else
            _fdrDBContext.Profiles.Update(profileEntity);

        await _fdrDBContext.SaveChangesAsync();

        return Success<Profile>(new());
    }

    [HttpDelete]
    public async Task<ApiResponseModel<Profile>> DeleteCustomerProfileInFdR(string civilId)
    {
        if (!_authManager.HasPermission(Permissions.CustomerProfile.Delete()))
            return Failure<Profile>(GlobalResources.UnAuthorized);

        var customerProfile = _fdrDBContext.Profiles.FirstOrDefault(x => x.CivilId == civilId);

        if (customerProfile == null)
            return Failure<Profile>("Profile not found to delete!");

        if (_fdrDBContext.Requests.Any(x => x.CivilId == civilId))
            return Failure<Profile>("Cannot delete profile as cards issued against it.");

        _fdrDBContext.Profiles.Remove(customerProfile);

        await _fdrDBContext.SaveChangesAsync();

        return Success<Profile>(new(), message: $"Profile has been deleted (civilId: {civilId} ) successfully! ");
    }


    [HttpGet]
    public async Task<ApiResponseModel<ProfileDto>> GetCustomerProfileFromFdRlocalDb(string civilId)
    {
        var customerProfile = await _fdrDBContext.Profiles.AsNoTracking().FirstOrDefaultAsync(x => x.CivilId == civilId);// ?? throw new ApiException(message: "Customer does not have profile locally");
        return Success(customerProfile!.Adapt<ProfileDto>());
    }

    [NonAction]
    public async Task<ApiResponseModel<Profile>> GetCustomerProfileFromFdRlocalDbByRequestId(decimal requestId)
    {
        var response = new ApiResponseModel<Profile>();

        Request? request = await _fdrDBContext.Requests.AsNoTracking().FirstOrDefaultAsync(x => x.RequestId == requestId);

        Profile? customerProfile = new() { CivilId = request!.CivilId };

        if (request is not null)
            customerProfile = await _fdrDBContext.Profiles.AsNoTracking().FirstOrDefaultAsync(x => x.CivilId == request.CivilId);

        return response.Success(customerProfile);
    }

    [HttpGet]
    public async Task<ApiResponseModel<CustomerProfileDto>> GetCustomerProfile(string civilId)
    {
        var response = new ApiResponseModel<CustomerProfileDto>();

        var customerProfileResult = await _customerProfileServiceClient.viewBankingCustomerProfileAsync(new() { civilID = civilId });

        if (customerProfileResult?.viewBankingCustomerProfileResult is null)
            return response.Fail("Unable to fetch customer profile");

        var customerProfile = customerProfileResult.viewBankingCustomerProfileResult;

        return response.Success(new()
        {
            FirstName = customerProfile.first_name,
            LastName = customerProfile.first_name,
            FirstNameArabic = customerProfile.first_name,
            EmployeeNumber = customerProfile.employer,
            RimCode = customerProfile.rimCode
        });
    }



    [HttpGet]
    public async Task<CustomerLookupData> GetLookupData()
    {
        var cached = await _cache.GetJsonAsync<CustomerLookupData>(nameof(CustomerLookupData));
        if (cached != null)
        {
            return cached;
        }

        var lookupData = (await _customerProfileServiceClient.getCustomerProfileLookupDataAsync(new()))?.getCustomerProfileLookupDataResult?.Adapt<CustomerLookupData>();

        if (lookupData is null)
            return new();

        lookupData.GenderLookupData = Enum.GetNames(typeof(Gender)).Select(x => new KFHItem()
        {
            Key = (int)(Enum.Parse(typeof(Gender), x)),
            Value = x.ToString()
        }).ToList();

        lookupData.MaritalStatuslLookupData = Enum.GetNames(typeof(MarriageStatus)).Select(x => new KFHItem()
        {
            Key = (int)(Enum.Parse(typeof(MarriageStatus), x)),
            Value = x.ToString()
        }).ToList();

        lookupData.Residence = Enum.GetNames(typeof(Residency)).Select(x => new KFHItem()
        {
            Key = (int)(Enum.Parse(typeof(Residency), x)),
            Value = x.ToString()
        }).ToList();

        lookupData.AreaCode = (await _lookupAppService.GetAreaCodes())?.Data?.Select(x => new KFHItem() { Key = x.AreaId, Value = x.AreaName }).ToList();

        if (lookupData is not null)
            await _cache.SetJsonAsync(nameof(CustomerLookupData), lookupData, 60);

        return lookupData ?? new();
    }

    [HttpGet]
    public async Task<RelationManagerDto> GetCustomerRelationManager(int rimNo)
    {
        var response = await _customerProfileServiceClient.getCorporateRelationManagerAsync(
            new getCorporateRelationManagerRequest
            {
                rim = rimNo.ToString()
            });

        var responseData = response.getCorporateRelationManagerResult;
        if (responseData == null)
        {
            return new RelationManagerDto();
        }

        return new RelationManagerDto()
        {
            Name = responseData.name,
            UserName = responseData.userName,
            EmployeeId = responseData.employeeId,
            EmployeeNumber = responseData.employeeNumber,
            ArabicName = responseData.arabicName,
            EnglishName = responseData.englishName,
            OfficePhone = responseData.officePhone,
            OfficeLocation = responseData.officeLocation,
            Email = responseData.email,
        };
    }



    [HttpPost]
    public async Task<ApiResponseModel<CustomerProfileDto>> GetGenericCustomerProfile([FromBody] CustomerProfileSearchCriteria? searchCriteria)
    {
        var response = new ApiResponseModel<CustomerProfileDto>();

        if (searchCriteria == null)
            return response.Fail("Please enter search criteria");

        var customerProfileResult = (await _customerProfileServiceClient.viewGenericCustomerProfileNotFilteredAsync(new viewGenericCustomerProfileNotFilteredRequest
        {
            civilID = searchCriteria.CivilId,
            rim = searchCriteria.RimNumber,
            accountNumber = searchCriteria.AccountNumber
        }))?.viewGenericCustomerProfileNotFilteredResult;

        if (customerProfileResult is null || customerProfileResult?.rimCode == null)
            return response.Fail("Customer profile not found");

        var customerRimClass = await GetCustomerRimClass();

        var rimCode = Convert.ToInt32(customerProfileResult.rimCode);
        var rimClass = customerRimClass.Single(x => x.ClassCode == rimCode).ClassDescriptionEn;

        var customerProfile = new CustomerProfileDto
        {
            CivilId = customerProfileResult.tin,
            DateOfBirth = customerProfileResult.birthDateSpecified ? customerProfileResult.birthDate : null,
            FirstName = customerProfileResult.firstName,
            FirstNameArabic = customerProfileResult.otherLangFirstName,
            LastName = customerProfileResult.lastName,
            LastNameArabic = customerProfileResult.otherLangLastName,
            RimNumber = customerProfileResult.rimNo,
            RimStatus = customerProfileResult.rimStatus,
            RimCode = customerProfileResult.rimCode,
            CustomerType = customerProfileResult.customerType,
            Gender = customerProfileResult.sex,
            RimClass = rimClass,
            PhoneNumber = customerProfileResult.customerAddressList[0]?.phone3,
            CustomerAddresses = customerProfileResult.customerAddressList?.Select(x => new CustomerAddressDto
            {
                AddressId = x.addressID,
                AddressTypeId = x.addressTypeID,
                AddressLine1 = x.addressLine1,
                AddressLine2 = x.addressLine2,
                AddressLine3 = x.addressLine3,
                PhoneNumber1 = x.phone1,
                PhoneNumber2 = x.phone2,
                PhoneNumber3 = x.phone3,
                PhoneNumber1Extension = x.phone1Ext,
                PhoneNumber2Extension = x.phone2Ext,
                PhoneNumber3Extension = x.phone3Ext,
                EmailAddress = x.emailAddress1,
                FaxNumber = x.faxPhone,
                CityId = x.cityId,
                CityName = x.cityName,
                CountryCode = x.countryCode,
                District = x.district,
                Region = x.region,
                State = x.state,
                ZipCode = x.zip
            }).ToList()
        };

        //var rimDetails = await _context.CustomerClasses.SingleOrDefaultAsync(x => x.ClassCode == rimCode);
        //if (rimDetails != null)
        //{
        //    customerProfile.ImageUrl = $"/api/customerClassImages/{rimDetails.Id}";
        //}

        /// Check is employee
        var accountsNumbers = await GetAccountsNumbers(customerProfile.CivilId);
        if (accountsNumbers.Count > 0)
        {
            var employee = await CheckCustomerIsEmployee(accountsNumbers);
            customerProfile.EmployeeNumber = employee.EmployeeNumber;
            customerProfile.IsEmployee = employee.IsEmployee;
        }

        return response.Success(customerProfile);
    }

    [HttpPost]
    public async Task<ApiResponseModel<GenericCustomerProfileDto>> GetCustomerProfileMinimal([FromBody] ProfileSearchCriteria criteria)
    {
        try
        {
            var response = new ApiResponseModel<GenericCustomerProfileDto>();


            if (criteria.CivilId is null && decimal.TryParse(criteria.RequestId, out decimal requestId))
            {
                var request = await _fdrDBContext.Requests.AsNoTracking().FirstOrDefaultAsync(x => x.RequestId == requestId);
                criteria.CivilId = request?.CivilId;
            }


            var result = (await _customerProfileServiceClient.viewDetailedGenericCustomerProfileAsync(
                new viewDetailedGenericCustomerProfileRequest
                {
                    civilID = criteria.CivilId,
                    accountNumber = criteria.AccountNo
                })).viewDetailedGenericCustomerProfileResult;

            if (result == null)
                return Failure<GenericCustomerProfileDto>("Error in Phoenix Customer profile");

            _ = decimal.TryParse(result.professionAndFinancialInfo.monthlyIncome, out decimal _monthlyIncome);
            _ = decimal.TryParse(result.professionAndFinancialInfo.otherIncome, out decimal _otherIncome);
            _ = int.TryParse(result.personalInfo.accOpeningReasonID, out int _openingReasonId);
            var bioStatus = await _customerProfileCommonApi.GetBiometricStatus(criteria.CivilId);
            
            
            //if (!_authManager.HasPermission(Permissions.StaffSalary.View()))
            //{
            //    _monthlyIncome = 0;
            //    _otherIncome = 0;
            //}

            var profile = new GenericCustomerProfileDto
            {
                IsEmployee = !string.IsNullOrEmpty(result.employer),
                ArabicRimType = result.arabicRimType,
                CivilId = result.tin,
                RimNo = result.rimNo,
                NationalityId = result.nationalityID,
                RimStatus = result.rimStatus,
                RimClass = result.englishRimClass,
                RimCode = int.Parse(result.rimCode),
                FirstName = result.otherLangFirstName,
                LastName = result.otherLangLastName,
                ArabicName = $"{result.firstName} {result.lastName}",
                TitleDescription = result.titleDesc,
                TitleId = result.titleId,
                PhoneNumber = result.customerAddressList[0]?.phone3,
                HomePhoneNumber = result.customerAddressList[0]?.phone1,
                BirthDate = result.birthDate,
                CustomerType = result.customerType.Trim(),
                CIDExpiryDate = result.civilIDExpiry,
                FATCAStatus = result.fatcaStatus,
                FATCAw8 = result.formW8,
                FATCAw9 = result.formW9,
                CrsStatusId = result.crsStatus,
                CrsStatusDesc = GetCrsStatus(result.crsStatus),
                CrsClassificationId = result.crsClassification,
                CrsClassificationDesc = GetCrsClass(result.crsClassification),
                SICCode = !string.IsNullOrEmpty(result.sicCode) ? int.Parse(result.sicCode) : 0,
                MobileNumber = result.customerAddressList[0].phone3,
                Income = _monthlyIncome,
                OtherIncome = _otherIncome,
                IncomeSource = result.professionAndFinancialInfo.incomeSource,
                disabilityType = result.specialNeed,
                PEP = result.politicalAndDiplomaticInfo.politicalOrDiplomaticPerson,
                Gender = result.sex,
                MaritalStatus = result.maritalStatus,
                PositionID = result.professionAndFinancialInfo.positionId,
                EmployerName = result.professionAndFinancialInfo.employerName,
                EmployeeDesc = result.professionAndFinancialInfo.employerName,
                WorkAddress = result.professionAndFinancialInfo.officeAddress,
                DealingWithBankReasonID = result.personalInfo.accOpeningReasonID,
                TransactionType = result.professionAndFinancialInfo.transType,
                EmployeePositionDesc = result.professionAndFinancialInfo.positionId,
                SpecialNeed = result.specialNeed,
                OpeningReasonId = !string.IsNullOrEmpty(result.personalInfo.accOpeningReasonID) ? _openingReasonId : 0,
                CustomerAddresses = result.customerAddressList?.Select(x => new CustomerAddressDto
                {
                    AddressId = x.addressID,
                    AddressTypeId = x.addressTypeID,
                    AddressLine1 = x.addressLine1,
                    AddressLine2 = x.addressLine2,
                    AddressLine3 = x.addressLine3,
                    PhoneNumber1 = x.phone1,
                    PhoneNumber2 = x.phone2,
                    PhoneNumber3 = x.phone3,
                    FaxNumber = x.faxPhone,
                    EmailAddress = x.emailAddress1,
                    PostBoxNumber = x.postBoxNo,
                    BlockNumber = x.blockID,
                    House = x.home,
                    Street = x.street,
                    ZipCode = x.zip,
                    Region = x.region,
                    RegionId = x.regionID,
                    FlatNumber = x.flat
                }).ToList(),
                Occupation = result.sicCode,
                IsRetired = (result.sicCode == "" || result.customerType != "Personal") ? false : ConfigurationBase.RetiredOccupationID.Split(",").Any(x => x == result.sicCode),
                IsKFHCustomer = result.rimNo != 0,
                EducationId = result.educationID,
                Residency = result.personalInfo.residency,
                IsPendingBioMetric = bioStatus.ShouldStop,
                IsKYCExpired = bioStatus.KycExpired
            };

            //commented as you don't need them any more , the common packages handle these properties 
            //var blackListResponse = CheckIsBlackList(criteria.CivilId!);
            //var checkIsVIPCustomer = CheckIsVIPCustomer(profile.RimNo);

            var customerNationality = GetCustomerNationality(profile.NationalityId);
            await Task.WhenAll(customerNationality);

            //commented as you don't need them any more , the common packages handle these properties 
            //profile.Blacklist = blackListResponse.Result;
            //profile.VIP = checkIsVIPCustomer.Result;

            // Get nationality
            profile.NationalityEnglish = customerNationality.Result;
            return Success(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(GetCustomerProfileMinimal));

            throw;
        }
    }

    [HttpPost]
    public async Task<ApiResponseModel<GenericCustomerProfileDto>> GetDetailedGenericCustomerProfile([FromBody] ProfileSearchCriteria criteria)
    {
        try
        {
            var response = new ApiResponseModel<GenericCustomerProfileDto>();

            //var cacheName = $"Profile-{(!string.IsNullOrEmpty(criteria.CivilId) ? criteria.CivilId : criteria.AccountNo)}";

            //var cachedProfile = (await _cache.GetJsonAsync<GenericCustomerProfileDto>(cacheName));
            //if (cachedProfile != null)
            //    return response.Success(cachedProfile);

            var result = (await _customerProfileServiceClient.viewDetailedGenericCustomerProfileAsync(
                new viewDetailedGenericCustomerProfileRequest
                {
                    civilID = criteria.CivilId,
                    accountNumber = criteria.AccountNo
                })).viewDetailedGenericCustomerProfileResult;

            if (result == null)
                return response.Fail("Error in Phoenix Customer profile");

            _ = decimal.TryParse(result.professionAndFinancialInfo.monthlyIncome, out decimal _monthlyIncome);
            _ = decimal.TryParse(result.professionAndFinancialInfo.otherIncome, out decimal _otherIncome);
            _ = int.TryParse(result.personalInfo.accOpeningReasonID, out int _openingReasonId);


            //if (!_authManager.HasPermission(Permissions.StaffSalary.View()))
            //{
            //    _monthlyIncome = 0;
            //    _otherIncome = 0;
            //}

            var profile = new GenericCustomerProfileDto
            {
                ArabicRimType = result.arabicRimType,
                CivilId = result.tin,
                RimNo = result.rimNo,
                NationalityId = result.nationalityID,
                RimStatus = result.rimStatus,
                RimClass = result.englishRimClass,
                RimCode = int.Parse(result.rimCode),
                FirstName = result.otherLangFirstName,
                LastName = result.otherLangLastName,
                ArabicName = $"{result.firstName} {result.lastName}",
                TitleDescription = result.titleDesc,
                PhoneNumber = result.customerAddressList[0]?.phone3,
                HomePhoneNumber = result.customerAddressList[0]?.phone1,
                BirthDate = result.birthDate,
                CustomerType = result.customerType.Trim(),
                CIDExpiryDate = result.civilIDExpiry,
                FATCAStatus = result.fatcaStatus,
                FATCAw8 = result.formW8,
                FATCAw9 = result.formW9,
                CrsStatusId = result.crsStatus,
                CrsStatusDesc = GetCrsStatus(result.crsStatus),
                CrsClassificationId = result.crsClassification,
                CrsClassificationDesc = GetCrsClass(result.crsClassification),
                SICCode = !string.IsNullOrEmpty(result.sicCode) ? int.Parse(result.sicCode) : 0,
                MobileNumber = result.customerAddressList[0].phone3,
                Income = _monthlyIncome,
                OtherIncome = _otherIncome,
                IncomeSource = result.professionAndFinancialInfo.incomeSource,
                disabilityType = result.specialNeed,
                PEP = result.politicalAndDiplomaticInfo.politicalOrDiplomaticPerson,
                Gender = result.sex,
                MaritalStatus = result.maritalStatus,
                PositionID = result.professionAndFinancialInfo.positionId,
                EmployerName = result.professionAndFinancialInfo.employerName,
                EmployeeDesc = result.professionAndFinancialInfo.employerName,
                WorkAddress = result.professionAndFinancialInfo.officeAddress,
                DealingWithBankReasonID = result.personalInfo.accOpeningReasonID,
                TransactionType = result.professionAndFinancialInfo.transType,
                EmployeePositionDesc = result.professionAndFinancialInfo.positionId,
                SpecialNeed = result.specialNeed,
                OpeningReasonId = !string.IsNullOrEmpty(result.personalInfo.accOpeningReasonID) ? _openingReasonId : 0,
                CustomerAddresses = result.customerAddressList?.Select(x => new CustomerAddressDto
                {
                    AddressId = x.addressID,
                    AddressTypeId = x.addressTypeID,
                    AddressLine1 = x.addressLine1,
                    AddressLine2 = x.addressLine2,
                    AddressLine3 = x.addressLine3,
                    PhoneNumber1 = x.phone1,
                    PhoneNumber2 = x.phone2,
                    PhoneNumber3 = x.phone3,
                    FaxNumber = x.faxPhone,
                    EmailAddress = x.emailAddress1,
                    PostBoxNumber = x.postBoxNo,
                    BlockNumber = x.blockID,
                    House = x.home,
                    Street = x.street,
                    ZipCode = x.zip,
                    Region = x.region,
                    RegionId = x.regionID,
                    FlatNumber = x.flat
                }).ToList(),
                Occupation = result.sicCode,
                IsRetired = (result.sicCode == "" || result.customerType != "Personal") ? false : ConfigurationBase.RetiredOccupationID.Split(",").Any(x => x == result.sicCode),
                IsKFHCustomer = result.rimNo != 0,
                EducationId = result.educationID,
                Residency = result.personalInfo.residency
            };

            //var rimDetails = await _context.CustomerClasses.SingleOrDefaultAsync(x => x.ClassCode == profile.RimCode);
            //if (rimDetails != null)
            //{
            //    profile.ImageUrl = $"/api/customerClassImages/{rimDetails.Id}";
            //}



            // Check black list 
            var kycResponse = GetKycData(profile.CivilId);
            var blackListResponse = CheckIsBlackList(profile.CivilId);
            var customerPosition = GetCustomerPosition(profile.EmployeePositionDesc!);
            var customerNationality = GetCustomerNationality(profile.NationalityId);

            var dealingReason = GetDealingReason(profile.OpeningReasonId.ToString());
            var accountsNumbers = GetAccountsNumbers(profile.CivilId);
            var checkIsVIPCustomer = CheckIsVIPCustomer(profile.RimNo);

            await Task.WhenAll(kycResponse,
           blackListResponse,
           customerPosition,
           customerNationality,
           dealingReason,
           accountsNumbers,
           checkIsVIPCustomer);

            // Check black list 
            profile.Blacklist = blackListResponse.Result;

            // Get kyc data
            profile.KYCAlerts = kycResponse.Result.Status;
            profile.LastKYCDate = kycResponse.Result.KycDate;

            profile.EmployeePositionDesc = customerPosition.Result;
            // Get position
            profile.Position = await GetCustomerPosition(result.professionAndFinancialInfo.positionId);

            // Get dealing reason
            profile.DealingWithBankReason = await GetDealingReason(result.personalInfo.accOpeningReasonID);

            // Get nationality
            profile.NationalityEnglish = customerNationality.Result;

            // Check is employee

            if (accountsNumbers.Result.Count > 0)
            {
                var employee = await CheckCustomerIsEmployee(accountsNumbers.Result);
                profile.EmployeeNumber = employee.EmployeeNumber;
                profile.IsEmployee = employee.IsEmployee;
            }


            profile.VIP = checkIsVIPCustomer.Result;

            //await _cache.SetJsonAsync(cacheName, profile, 60);





            return response.Success(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(GetDetailedGenericCustomerProfile));

            throw;
        }
    }


    async Task<bool> CheckIsVIPCustomer(int rim)
    {
        var response = await _customerProfileServiceClient.checkVVIPCustomerStatusAsync(
            new checkVVIPCustomerStatusRequest
            {

                rimNo = rim
            });
        return response.checkVVIPCustomerStatusResult;
    }
    #endregion

    #region Private Methods

    [NonAction]
    public async Task<string?> GetCustomerNationality(string nationalityId)
    {
        try
        {
            if (!string.IsNullOrEmpty(nationalityId))
            {
                var nationalityResponse = await _customerProfileServiceClient.getBankingStaticDataAsync(new()
                {
                    dataTypeName = "Nationality"
                });

                if (nationalityResponse.getBankingStaticDataResult != null)
                {
                    var nationality = nationalityResponse.getBankingStaticDataResult.dataTypeDetailList.FirstOrDefault(x =>
                        x.dataTypeID == nationalityId);

                    return nationality?.dataNameEn;

                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(GetCustomerPosition));
        }

        return "";
    }
    //private async Task<List<customerClassDTO>> GetCustomerRimClass()
    //{
    //    var cached = await _cache.GetJsonAsync<List<customerClassDTO>>("customerRimClass");
    //    if (cached is { Count: > 0 })
    //    {
    //        return cached;
    //    }

    //    //TODO: Review this
    //    var classes = await _customerProfileServiceClient.getCustomerClassAsync(new getCustomerClassRequest());
    //    var result = classes.getCustomerClassResult.ToList();

    //    await _cache.SetJsonAsync("customerRimClass", result, 60);
    //    return result;
    //}

    //private bool ValidateUserPermission()
    //{
    //    var auth = _authManager.HasPermission(Permissions.CustomerSalary.View());

    //    if (auth)
    //        return true;

    //    return false;
    //}

    private static string GetCrsStatus(int crsStatus)
    {
        return crsStatus switch
        {
            1 => "CRS Reportable",
            2 => "CRS Indicia Detected",
            3 => "Non CRS Reportable",
            0 or > 3 => "Other",
            _ => ""
        };
    }

    private static string GetCrsClass(int crsClass)
    {
        return crsClass switch
        {
            1 => "FI - Managed Investment entity in a Non-ParticipatingCountry",
            2 => "FI - Other Investment Entity",
            3 => "FI - Other Financial Institution",
            4 => "Active NFE - a_Listed_corporation or it is related entity",
            5 => "Active NFE - a Governmental Entity or a Central Bank",
            6 => "Active NFE - An International Organization",
            7 => "Active NFE - Other than above ( 4 to 6)",
            8 => "Passive NFE",
            0 or > 8 => "Other",
            _ => ""
        };
    }

    private async Task<GenericCustomerKyc> GetKycData(string civilId)
    {
        try
        {
            var kycResponse = await _customerAccountsServiceClient.checkKYCAsync(new()
            {
                civilId = civilId
            });

            if (kycResponse.checkKYC != null)
            {
                return new()
                {
                    Status = kycResponse.checkKYC.status,
                    KycDate = GetDateTime(kycResponse.checkKYC.kycDate)
                };
            }
        }

        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(GetKycData));
        }

        return new();
    }

    private async Task<bool> CheckIsBlackList(string civilId)
    {
        try
        {
            var blackListResponse = await _customerProfileServiceClient.isCustomerBlackListedAsync(new()
            {
                civilID = civilId
            });
            return blackListResponse.isCustomerBlackListedResult;
        }

        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(CheckIsBlackList));
        }

        return false;
    }

    private async Task<string?> GetCustomerPosition(string positionId)
    {
        try
        {
            var positionResponse = await _customerProfileServiceClient.getBankingStaticDataAsync(new()
            {
                dataTypeName = "CustomerPositions"
            });

            if (positionResponse.getBankingStaticDataResult != null)
            {
                var position = positionResponse.getBankingStaticDataResult.dataTypeDetailList.FirstOrDefault(x =>
                    x.dataTypeID == positionId);
                return position?.dataNameEn;

            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(GetCustomerPosition));
        }

        return "";
    }

    private async Task<string?> GetDealingReason(string openingReasonId)
    {
        try
        {
            var reasonDealingWithBankResponse = await _customerProfileServiceClient.getBankingStaticDataAsync(new()
            {
                dataTypeName = "DealingReasons"
            });

            if (reasonDealingWithBankResponse != null)
            {
                var reason = reasonDealingWithBankResponse.getBankingStaticDataResult.dataTypeDetailList.FirstOrDefault(
                    x => x.dataTypeID == openingReasonId);
                return reason?.dataNameEn;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(GetDealingReason));
        }

        return "";
    }

    private async Task<List<string>> GetAccountsNumbers(string civilId)
    {
        var accountsNumbers = new List<string>();

        try
        {
            var accounts = await _customerAccountsServiceClient.viewAccountsListByCivilIdAsync(new()
            {
                civilId = civilId
            });

            var accountsList = accounts.viewAccountsListByCivilIdResult.Select(y => (AccountDetailsDTO)y).ToList();
            foreach (var account in accountsList)
            {
                accountsNumbers.Add(account.acct);
            }

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(GetAccountsNumbers));
        }

        return accountsNumbers;
    }

    private async Task<GenericCustomerEmployee> CheckCustomerIsEmployee(List<string> accountsNumbers)
    {
        try
        {
            var employeeNumberResponse = await _hrServiceClient.getEmployeeNoAsync(new()
            {
                acctList = accountsNumbers.ToArray()
            });

            if (!string.IsNullOrEmpty(employeeNumberResponse.getEmployeeNoResult))
            {
                return new()
                {
                    EmployeeNumber = employeeNumberResponse.getEmployeeNoResult,
                    IsEmployee = true
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(CheckCustomerIsEmployee));
        }

        return new();
    }

    private static DateTime? GetDateTime(string s)
    {
        if (string.IsNullOrEmpty(s)) return null;

        return DateTime.Parse(s);
    }


    private static async Task<(string productName, CardCategoryType cardCategory, string mem, string collateral)> GetCreditCardProductName(CardDefinition product, IEnumerable<RequestParameter> rps)
    {
        var sp = rps.FirstOrDefault(x => x.Parameter == "IsSupplementaryOrPrimaryChargeCard");
        var mb = rps.FirstOrDefault(x => x.Parameter == "CLUB_MEMBERSHIP_ID");
        var ct = rps.FirstOrDefault(x => x.Parameter == "ISSUING_OPTION");

        var collateral = ct?.Value;

        if (Enum.TryParse(typeof(Collateral), collateral, out object? _collateralEnum))
        {
            collateral = ((Collateral)_collateralEnum).GetDescription();
        }


        string cardType = sp?.Value ?? "N";

        string? productNameWithType = product.Name;

        if (decimal.TryParse(product.MinLimit, out decimal _minLimit) && decimal.TryParse(product.MaxLimit, out decimal _maxLimit))
            productNameWithType = $"{productNameWithType} -  {Helpers.GetProductType(product.Duality, _minLimit, _maxLimit)}";

        var rr = cardType switch
        {
            "S" => ($"{productNameWithType} (Supplementary)", CardCategoryType.Supplementary),
            "P" => ($"{productNameWithType} (Primary)", CardCategoryType.Primary),
            "N" => ($"{productNameWithType}", CardCategoryType.Normal),
            _ => ($"{productNameWithType}", CardCategoryType.Normal)
        };


        return await Task.FromResult((rr.Item1, rr.Item2, mb?.Value ?? "", collateral ?? ""));

    }

    private async Task<List<CreditCardDto>> GetCustomerCards(string? civilId)
    {

        if (civilId == null) return [];

        var allowedCardTypes = (await _configurationAppService.GetValue(ConfigurationBase.SO_AllowedCardTypes))?.Split(",") ?? Array.Empty<string>();

        var branches = await _cache.GetJsonAsync<List<Branch>>("branches");
        if (branches is null)
        {
            branches = (await _organizationClient.GetBranches()).Select(x => new Branch()
            {
                BranchId = x.BranchId,
                Name = Regex.Replace(x.Name, @"[^A-Za-z]", " ").Trim()
            }).ToList();
            await _cache.SetJsonAsync("branches", branches, 60);
        }


        var currencies = await _fdrDBContext.CardCurrencies.AsNoTracking().ToListAsync();
        bool canViewCardNumber = _authManager.HasPermission(Permissions.CreditCardsNumber.View());
        var allRequests = await (from request in _fdrDBContext.Requests.AsNoTracking()
                        //.Include(x => x.Parameters).AsNoTracking()
                        .Where(x => x.CivilId == civilId).OrderByDescending(x => x.ReqDate)
                                 join reqStatus in _fdrDBContext.RequestStatuses.AsNoTracking() on request.ReqStatus equals reqStatus.StatusId
                                 select new CreditCardDto
                                 {
                                     CivilId = request.CivilId,
                                     MobileNumber = request.Mobile,
                                     RequestId = request.RequestId,
                                     CardNumber = request.CardNo ?? "",
                                     CardType = request.CardType.ToString(),
                                     AccountNumber = request.AcctNo!,
                                     BranchId = request.BranchId,
                                     CardLimit = request.ApproveLimit ?? 0,
                                     ExpirationDate = request.Expiry == null || (request.Expiry != null && request.Expiry!.Trim() == "0000") ? null : DateTime.ParseExact(Convert.ToDecimal(request.Expiry).ToString("0000"), ConfigurationBase.ExpiryDateFormat, CultureInfo.InvariantCulture).AddMonths(1).AddDays(-1),
                                     OpenedDate = (DateTime)request.ApproveDate!,
                                     StatusId = request.ReqStatus,
                                     Status = reqStatus.EnglishDescription,
                                     HoldAmount = request.DepositAmount,
                                     ApprovedLimit = request.ApproveLimit ?? 0,
                                     IsAllowStandingOrder = allowedCardTypes.Any(so => so == request.CardType.ToString()),
                                     IsAUB = request.IsAUB
                                 }).ToListAsync();


        allRequests.ForEach(async ar =>
        {
            var product = await _fdrDBContext.CardDefs.AsNoTracking().Include(c => c.CardDefExts).FirstOrDefaultAsync(cd => cd.CardType.ToString() == ar.CardType);
            var CurrencyORG = product?.CardDefExts.FirstOrDefault(cde => cde.Attribute.ToUpper() == "ORG");

            var requestParameters = _fdrDBContext.RequestParameters.AsNoTracking().Where(rp => rp.ReqId == ar.RequestId);
            var productAttribute = await GetCreditCardProductName(product!, requestParameters);

            if (Convert.ToBoolean(ar.IsAUB))
            {
                ar.AUBCardNumber = (await _fdrDBContext.AubCardMappings.AsNoTracking().FirstOrDefaultAsync(aub => aub.KfhCardNo == ar.CardNumber && aub.IsRenewed == false))?.AubCardNo ?? null;
                if (!string.IsNullOrEmpty(ar.AUBCardNumber))
                    ar.AUBCardNumberDto = ar.AUBCardNumber.SaltThis();
            }


            ar.MemberShipId = productAttribute.mem;
            ar.Collateral = productAttribute.collateral;
            ar.CurrencyISO = CurrencyORG?.Value ?? "";
            ar.ProductType = productAttribute.productName;
            ar.CardCategory = productAttribute.cardCategory;
            ar.CbkClass = product?.Duality == 7 ? "Installment" : "";
            ar.BranchName = branches.FirstOrDefault(b => b.BranchId == ar.BranchId)?.Name ?? "";
            ar.CurrencyISO = currencies.FirstOrDefault(cur => cur.Org == ar.CurrencyISO)?.CurrencyIsoCode ?? "";
            ar.CardNumberDto = ar.CardNumber.SaltThis();
        });

        return allRequests;
    }

    private async Task<List<CreditCardDto>> GetAllCards(string? civilId)
    {

        if (civilId == null) return [];

        bool isCorporateCivilId = civilId.Length < 12;


        var allowedCardTypes = (await _configurationAppService.GetValue(ConfigurationBase.SO_AllowedCardTypes))?.Split(",") ?? Array.Empty<string>();

        var branches = await _cache.GetJsonAsync<List<Branch>>("branches");
        if (branches is null)
        {
            branches = (await _organizationClient.GetBranches()).Select(x => new Branch()
            {
                BranchId = x.BranchId,
                Name = Regex.Replace(x.Name, @"[^A-Za-z]", " ").Trim()
            }).ToList();
            await _cache.SetJsonAsync("branches", branches, 60);
        }


        var currencies = await _fdrDBContext.CardCurrencies.AsNoTracking().ToListAsync();

        bool canViewCardNumber = _authManager.HasPermission(Permissions.CreditCardsNumber.View());

        var allRequests = await (from request in _fdrDBContext.Requests.AsNoTracking()
                                 join rp in _fdrDBContext.RequestParameters on request.RequestId equals rp.ReqId
                                 where ((isCorporateCivilId && rp.Parameter == "corporate_civil_id" && rp.Value == civilId) ||
                                 request.CivilId == civilId)
                                 join reqStatus in _fdrDBContext.RequestStatuses.AsNoTracking() on request.ReqStatus equals reqStatus.StatusId
                                 select new CreditCardDto
                                 {
                                     CivilId = request.CivilId,
                                     MobileNumber = request.Mobile,
                                     RequestId = request.RequestId,
                                     CardNumber = request.CardNo ?? "",
                                     CardType = request.CardType.ToString(),
                                     AccountNumber = request.AcctNo!,
                                     BranchId = request.BranchId,
                                     CardLimit = request.ApproveLimit ?? 0,
                                     ExpirationDate = request.Expiry == null || (request.Expiry != null && request.Expiry!.Trim() == "0000") ? null : DateTime.ParseExact(Convert.ToDecimal(request.Expiry).ToString("0000"), ConfigurationBase.ExpiryDateFormat, CultureInfo.InvariantCulture).AddMonths(1).AddDays(-1),
                                     OpenedDate = (DateTime)request.ApproveDate!,
                                     StatusId = request.ReqStatus,
                                     Status = reqStatus.EnglishDescription,
                                     HoldAmount = request.DepositAmount,
                                     ApprovedLimit = request.ApproveLimit ?? 0,
                                     IsAllowStandingOrder = allowedCardTypes.Any(so => so == request.CardType.ToString()),
                                     IsAUB = request.IsAUB
                                 }).AsSplitQuery().Distinct().OrderByDescending(x => x.OpenedDate).ToListAsync();


        allRequests.ForEach(async ar =>
        {
            var product = await _fdrDBContext.CardDefs.AsNoTracking().Include(c => c.CardDefExts).FirstOrDefaultAsync(cd => cd.CardType.ToString() == ar.CardType);
            var CurrencyORG = product?.CardDefExts.FirstOrDefault(cde => cde.Attribute.ToUpper() == "ORG");

            var requestParameters = _fdrDBContext.RequestParameters.AsNoTracking().Where(rp => rp.ReqId == ar.RequestId);
            var productAttribute = await GetCreditCardProductName(product!, requestParameters);

            if (Convert.ToBoolean(ar.IsAUB))
            {
                ar.AUBCardNumber = (await _fdrDBContext.AubCardMappings.AsNoTracking().FirstOrDefaultAsync(aub => aub.KfhCardNo == ar.CardNumber && aub.IsRenewed == false))?.AubCardNo ?? null;
                if (!string.IsNullOrEmpty(ar.AUBCardNumber))
                    ar.AUBCardNumberDto = ar.AUBCardNumber.SaltThis();
            }


            ar.MemberShipId = productAttribute.mem;
            ar.Collateral = productAttribute.collateral;
            ar.CurrencyISO = CurrencyORG?.Value ?? "";
            ar.ProductType = productAttribute.productName;
            ar.CardCategory = productAttribute.cardCategory;
            ar.CbkClass = product?.Duality == 7 ? "Installment" : "";
            ar.BranchName = branches.FirstOrDefault(b => b.BranchId == ar.BranchId)?.Name ?? "";
            ar.CurrencyISO = currencies.FirstOrDefault(cur => cur.Org == ar.CurrencyISO)?.CurrencyIsoCode ?? "";
            ar.CardNumberDto = ar.CardNumber.SaltThis();
        });

        return allRequests;
    }
    private async Task<CreditCardDto> GetCustomerCard(decimal? requestId)
    {

        //TODO: re-write

        var allowedCardTypes = (await _configurationAppService.GetValue(ConfigurationBase.SO_AllowedCardTypes))?.Split(",") ?? Array.Empty<string>();
        var branches = (await _organizationClient.GetBranches()).Select(x => new Branch() { BranchId = x.BranchId, Name = Regex.Replace(x.Name, @"[^A-Za-z]", " ").Trim() });
        var currencies = await _fdrDBContext.CardCurrencies.AsNoTracking().ToListAsync();
        bool canViewCardNumber = _authManager.HasPermission(Permissions.CreditCardsNumber.View());
        var cardRequest = await (from request in _fdrDBContext.Requests.AsNoTracking()
                        //.Include(x => x.Parameters).AsNoTracking()
                        .Where(x => x.RequestId == requestId).OrderByDescending(x => x.ReqDate)
                                 join reqStatus in _fdrDBContext.RequestStatuses.AsNoTracking() on request.ReqStatus equals reqStatus.StatusId
                                 select new CreditCardDto
                                 {
                                     RequestId = request.RequestId,
                                     CardNumber = request.CardNo ?? "",
                                     CardNumberDto = canViewCardNumber ? request.CardNo ?? "" : request.CardNo.Masked(6, 6),
                                     CardType = request.CardType.ToString(),
                                     AccountNumber = request.AcctNo!,
                                     BranchId = request.BranchId,
                                     CardLimit = request.ApproveLimit ?? 0,
                                     ExpirationDate = request.Expiry == null || (request.Expiry != null && request.Expiry!.Trim() == "0000") ? null : DateTime.ParseExact(Convert.ToDecimal(request.Expiry).ToString("0000"), ConfigurationBase.ExpiryDateFormat, CultureInfo.InvariantCulture).AddMonths(1).AddDays(-1),
                                     OpenedDate = (DateTime)request.ApproveDate!,
                                     StatusId = request.ReqStatus,
                                     Status = reqStatus.EnglishDescription,
                                     HoldAmount = request.DepositAmount,
                                     ApprovedLimit = request.ApproveLimit ?? 0,
                                     IsAllowStandingOrder = allowedCardTypes.Any(so => so == request.CardType.ToString()),
                                     IsAUB = request.IsAUB
                                 }).FirstOrDefaultAsync();




        var product = await _fdrDBContext.CardDefs.AsNoTracking().FirstOrDefaultAsync(cd => cd.CardType.ToString() == cardRequest.CardType);
        var requestParameters = _fdrDBContext.RequestParameters.AsNoTracking().Where(rp => rp.ReqId == cardRequest.RequestId);
        var productAttribute = await GetCreditCardProductName(product, requestParameters);
        var CurrencyORG = await _fdrDBContext.CardDefExts.AsNoTracking().FirstOrDefaultAsync(cde => cde.CardType.ToString() == cardRequest.CardType && cde.Attribute.ToUpper() == "ORG");

        if (Convert.ToBoolean(cardRequest.IsAUB))
        {
            cardRequest.AUBCardNumber = (await _fdrDBContext.AubCardMappings.AsNoTracking().FirstOrDefaultAsync(aub => aub.KfhCardNo == cardRequest.CardNumber && aub.IsRenewed == false))?.AubCardNo ?? null;
            cardRequest.AUBCardNumberDto = canViewCardNumber ? cardRequest.AUBCardNumber ?? "" : cardRequest.AUBCardNumber.Masked(6, 6);
        }


        cardRequest.MemberShipId = productAttribute.mem;
        cardRequest.Collateral = productAttribute.collateral;
        cardRequest.CurrencyISO = CurrencyORG?.Value ?? "";
        cardRequest.ProductType = productAttribute.productName;
        cardRequest.CardCategory = productAttribute.cardCategory;
        cardRequest.CbkClass = product?.Duality == 7 ? "Installment" : "";
        cardRequest.BranchName = branches.FirstOrDefault(b => b.BranchId == cardRequest.BranchId)?.Name ?? "";
        cardRequest.CurrencyISO = currencies.FirstOrDefault(cur => cur.Org == cardRequest.CurrencyISO)?.CurrencyIsoCode ?? "";


        return cardRequest;
    }

    private async Task<List<CreditCardLiteDto>> GetCustomerCardsLite(string? civilId)
    {
        var allowedCardTypes = (await _configurationAppService.GetValue(ConfigurationBase.SO_AllowedCardTypes))?.Split(",") ?? Array.Empty<string>();

        var branches = (await _organizationClient.GetBranches()).Select(x => new Branch() { BranchId = x.BranchId, Name = Regex.Replace(x.Name, @"[^A-Za-z]", " ").Trim() });
        var currencies = await _fdrDBContext.CardCurrencies.AsNoTracking().ToListAsync();
        bool canViewCardNumber = _authManager.HasPermission(Permissions.CreditCardsNumber.View());

        var allRequests = await (from request in _fdrDBContext.Requests.AsNoTracking()
                        .Include(x => x.Parameters).AsNoTracking()
                        .Where(x => x.CivilId == civilId).OrderByDescending(x => x.ReqDate)
                                 join reqStatus in _fdrDBContext.RequestStatuses.AsNoTracking() on request.ReqStatus equals reqStatus.StatusId
                                 select new CreditCardLiteDto
                                 {
                                     RequestId = request.RequestId,
                                     CardNumber = request.CardNo ?? "",
                                     CardType = request.CardType,
                                     CivilId = request.CivilId,
                                     ExpirationDate = request.Expiry == null || (request.Expiry != null && request.Expiry!.Trim() == "0000") ? null : DateTime.ParseExact(Convert.ToDecimal(request.Expiry).ToString("0000"), ConfigurationBase.ExpiryDateFormat, CultureInfo.InvariantCulture).AddMonths(1).AddDays(-1),
                                     OpenedDate = (DateTime)request.ApproveDate!,
                                     StatusId = request.ReqStatus,
                                     Status = reqStatus.EnglishDescription,
                                     IsAUB = request.IsAUB
                                 }).ToListAsync();


        allRequests.ForEach(async ar =>
        {
            var product = await _fdrDBContext.CardDefs.AsNoTracking().FirstOrDefaultAsync(cd => cd.CardType == ar.CardType);
            var requestParameters = _fdrDBContext.RequestParameters.AsNoTracking().Where(rp => rp.ReqId == ar.RequestId);
            var productAttribute = await GetCreditCardProductName(product, requestParameters);
            var CurrencyORG = await _fdrDBContext.CardDefExts.AsNoTracking().FirstOrDefaultAsync(cde => cde.CardType == ar.CardType && cde.Attribute.ToUpper() == "ORG");
            if (Convert.ToBoolean(ar.IsAUB))
            {
                var aubMapping = await _fdrDBContext.AubCardMappings.AsNoTracking().FirstOrDefaultAsync(aub => aub.KfhCardNo == ar.CardNumber && aub.IsRenewed == false);
                if (aubMapping is not null)
                {
                    ar.AUBCardNumber = aubMapping.AubCardNo;
                    ar.AUBCardNumberDto = aubMapping.AubCardNo?.SaltThis();
                }
            }

            ar.Collateral = productAttribute.collateral;
            ar.CurrencyISO = CurrencyORG?.Value ?? "";
            ar.ProductType = productAttribute.productName;
            ar.CardCategory = productAttribute.cardCategory;
            ar.CurrencyISO = currencies.FirstOrDefault(cur => cur.Org == ar.CurrencyISO)?.CurrencyIsoCode ?? "";
            ar.CardNumberDto = ar.CardNumber.SaltThis();
        });

        return allRequests;
    }



    private bool CardTypesEligibleForFdBalanceRequest(string cardNumber, int cardStatus)
    {
        return cardStatus is (int)CreditCardStatus.Active
            or (int)CreditCardStatus.Approved
            or (int)CreditCardStatus.TemporaryClosed
            or (int)CreditCardStatus.CardUpgradeStarted
            && !string.IsNullOrEmpty(cardNumber);
    }

    private decimal? GetCardBalance(BalanceCardStatusDetails? balanceDetails, int cardType, decimal approvedLimit, List<CreditCardProductsDto> products)
    {
        if (balanceDetails == null)
            return null;

        if (IsTayseerOrChargeCard(products, cardType))
        {
            return approvedLimit - balanceDetails.AvailableLimit;
        }

        return 0;
    }
    private bool IsTayseerOrChargeCard(List<CreditCardProductsDto> products, int cardType)
    {
        var card = products.FirstOrDefault(x => x.CardType == cardType && x.MinLimit > 0 && x.MaxLimit > 0);
        return card != null;
    }
    private static (string productName, CardCategoryType cardCategory) GetCreditCardProductName(CardDefinition product, string cardType)
    {
        if (string.IsNullOrEmpty(cardType))
            cardType = "N";

        string? productNameWithType = product.Name;

        if (decimal.TryParse(product.MinLimit, out decimal _minLimit) && decimal.TryParse(product.MaxLimit, out decimal _maxLimit))
            productNameWithType = $"{productNameWithType} -  {Helpers.GetProductType(product.Duality, _minLimit, _maxLimit)}";

        return cardType switch
        {
            "S" => ($"{productNameWithType} (Supplementary)", CardCategoryType.Supplementary),
            "P" => ($"{productNameWithType} (Primary)", CardCategoryType.Primary),
            "N" => ($"{productNameWithType}", CardCategoryType.Normal),
            _ => ($"{productNameWithType}", CardCategoryType.Normal)
        };
    }

    //private async Task<List<string>> GetAccountsNumbers(string civilId)
    //{
    //    var accountsNumbers = new List<string>();
    //    try
    //    {
    //        var accounts = await _customerAccountsServiceClient.viewAccountsListByCivilIdAsync(new()
    //        {
    //            civilId = civilId
    //        });

    //        var accountsList = accounts.viewAccountsListByCivilIdResult.Select(y => (AccountDetailsDTO)y).ToList();
    //        foreach (var account in accountsList)
    //        {
    //            accountsNumbers.Add(account.acct);
    //        }
    //    }
    //    catch (System.Exception ex)
    //    {
    //        _logger.LogError(ex, nameof(GetAccountsNumbers));
    //    }

    //    return accountsNumbers;
    //}

    //private async Task<GenericCustomerEmployee> CheckCustomerIsEmployee(List<string> accountsNumbers)
    //{
    //    try
    //    {
    //        var employeeNumberResponse = await _hrServiceClient.getEmployeeNoAsync(new()
    //        {
    //            acctList = accountsNumbers.ToArray()
    //        });

    //        if (!string.IsNullOrEmpty(employeeNumberResponse.getEmployeeNoResult))
    //        {
    //            return new()
    //            {
    //                EmployeeNumber = employeeNumberResponse.getEmployeeNoResult,
    //                IsEmployee = true
    //            };
    //        }
    //    }
    //    catch (System.Exception ex)
    //    {
    //        _logger.LogError(ex, nameof(CheckCustomerIsEmployee));
    //    }

    //    return new();
    //}

    [HttpPost]
    public async Task<CustomerProfileDto?> SearchCustomer([FromBody] CustomerProfileSearchCriteria? searchCriteria)
    {
        try
        {
            if (searchCriteria == null)
                return null;

            _auditLogger.Log.Information("Getting customer profile for search criteria: {@searchCriteria}",
                searchCriteria);
            var response = await _customerProfileServiceClient.viewBankingCustomerProfileAsync(
                new viewBankingCustomerProfileRequest
                {
                    civilID = searchCriteria.CivilId,
                    rim = searchCriteria.RimNumber,
                    accountNumber = searchCriteria.AccountNumber,
                    commercialReg = searchCriteria.CompanyRegistration
                });

            if (response is null)
                return null;

            var profileResult = response.viewBankingCustomerProfileResult;
            var customerRimClass = await GetCustomerRimClass();

            var rimCode = Convert.ToInt32(profileResult.rimCode);
            var rimClass = customerRimClass.SingleOrDefault(x => x.ClassCode == rimCode)
                ?.ClassDescriptionEn;

            var profile = new CustomerProfileDto
            {
                CivilId = profileResult.tin,
                DateOfBirth = profileResult.birth_dtSpecified ? profileResult.birth_dt.Date : null,
                FirstName = profileResult.first_name,
                LastName = profileResult.last_name,
                RimNumber = profileResult.rim_no,
                CustomerType = profileResult.customer_type,
                RimClass = rimClass ?? string.Empty,
                RimCode = profileResult.rimCode
            };

            return profile;
        }
        catch (System.Exception e)
        {
            return null;
        }
    }

    [NonAction]
    public async Task<List<CustomerClassDto>> GetCustomerRimClass()
    {
        var cached = await _cache.GetJsonAsync<List<CustomerClassDto>>("customerRimClass");
        if (cached is { Count: > 0 })
        {
            return cached;
        }

        var classes = await _customerProfileServiceClient.getCustomerClassAsync(new getCustomerClassRequest());
        var result = classes.getCustomerClassResult
            .Select(x => new CustomerClassDto()
            {
                ClassCode = x.customerClassCode,
                ClassDescriptionEn = x.customerClassDescription,
                ClassDescriptionAr = x.customerClassDescription,
                RimType = (RimTypes)Enum.Parse(typeof(RimTypes), x.rimType)
            }).ToList();

        await _cache.SetJsonAsync("customerRimClass", result, 60);
        return result;
    }

    #endregion
}
