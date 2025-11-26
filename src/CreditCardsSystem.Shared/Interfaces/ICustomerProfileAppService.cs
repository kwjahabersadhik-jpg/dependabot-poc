using CreditCardsSystem.Data.Models;
using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.Card;
using CreditCardsSystem.Domain.Models.Customer;
using Refit;


namespace CreditCardsSystem.Domain.Interfaces;

public interface ICustomerProfileAppService : IRefitClient
{
    const string Controller = "/api/CustomerProfile/";

    [Delete($"{Controller}{nameof(DeleteCustomerProfileInFdR)}")]
    Task<ApiResponseModel<Profile>> DeleteCustomerProfileInFdR(string civilId);

    [Post($"{Controller}{nameof(GetCustomerCards)}")]
    Task<ApiResponseModel<List<CreditCardDto>>> GetCustomerCards(CustomerProfileSearchCriteria searchCriteria);

    [Post($"{Controller}{nameof(GetCustomerCardsLite)}")]
    Task<ApiResponseModel<List<CreditCardLiteDto>>> GetCustomerCardsLite(CustomerProfileSearchCriteria? searchCriteria);

    [Get($"{Controller}{nameof(GetCreditCardBalances)}")]
    Task<CreditCardDto> GetCreditCardBalances(CreditCardDto card);

    [Get($"{Controller}{nameof(GetCustomerProfileFromFdRlocalDb)}")]
    Task<ApiResponseModel<ProfileDto>> GetCustomerProfileFromFdRlocalDb(string civilId);

    [Get($"{Controller}{nameof(GetBalanceStatusCardDetail)}")]
    Task<ApiResponseModel<BalanceCardStatusDetails>> GetBalanceStatusCardDetail(string cardNumber);

    Task<ApiResponseModel<Profile>> GetCustomerProfileFromFdRlocalDbByRequestId(decimal requestId);

    [Get($"{Controller}{nameof(GetCustomerProfile)}")]
    Task<ApiResponseModel<CustomerProfileDto>> GetCustomerProfile(string civilId);

    [Get($"{Controller}{nameof(GetLookupData)}")]
    Task<CustomerLookupData> GetLookupData();

    [Post($"{Controller}{nameof(CreateCustomerProfileInFdR)}")]
    Task<ApiResponseModel<Profile>> CreateCustomerProfileInFdR(ProfileDto profile);

    [Post($"{Controller}{nameof(SearchCustomer)}")]
    Task<CustomerProfileDto?> SearchCustomer(CustomerProfileSearchCriteria searchCriteria);


    [Get($"{Controller}{nameof(GetCustomerRelationManager)}")]
    Task<RelationManagerDto> GetCustomerRelationManager(int rimNo);

    Task<List<CustomerClassDto>> GetCustomerRimClass();





    [Post($"{Controller}{nameof(GetDetailedGenericCustomerProfile)}")]
    Task<ApiResponseModel<GenericCustomerProfileDto>> GetDetailedGenericCustomerProfile(ProfileSearchCriteria criteria);

    [Post($"{Controller}{nameof(GetGenericCustomerProfile)}")]
    Task<ApiResponseModel<CustomerProfileDto>> GetGenericCustomerProfile(CustomerProfileSearchCriteria searchCriteria);

    [Post($"{Controller}{nameof(GetCustomerProfileMinimal)}")]
    Task<ApiResponseModel<GenericCustomerProfileDto>> GetCustomerProfileMinimal(ProfileSearchCriteria criteria);

    Task<string?> GetCustomerNationality(string nationalityId);


    [Get($"{Controller}{nameof(GetCustomerCard)}")]
    Task<ApiResponseModel<CreditCardDto>> GetCustomerCard(string? requestId);


    [Get($"{Controller}{nameof(GetAllCards)}")]
    Task<ApiResponseModel<List<CreditCardDto>>> GetAllCards(CustomerProfileSearchCriteria? searchCriteria);
}