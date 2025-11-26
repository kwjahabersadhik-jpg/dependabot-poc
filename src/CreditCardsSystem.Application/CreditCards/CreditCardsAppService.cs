using CreditCardsSystem.Domain.Models.CreditCards;
using CreditCardTransactionInquiryServiceReference;
using Kfh.Aurora.Integration;

namespace CreditCardsSystem.Application.CreditCards;

public class CreditCardsAppService : ICreditCardsAppService, IAppService
{
    private readonly CreditCardInquiryServicesServiceClient _creditCardInquiryServiceClient;

    public CreditCardsAppService(IIntegrationUtility integrationUtility, IOptions<IntegrationOptions> options)
    {
        _creditCardInquiryServiceClient = integrationUtility.GetClient<CreditCardInquiryServicesServiceClient>(options.Value.Client, options.Value.Endpoints.CreditCardTransactionInquiry, options.Value.BypassSslValidation);

    }

    [HttpGet]
    public async Task<ApiResponseModel<List<CreditCardResponse>>> GetCreditCardsByCivilId(string civilId)
    {
        var response = new ApiResponseModel<List<CreditCardResponse>>();

        var result = (await _creditCardInquiryServiceClient.getCreditCardsAsync(new() { creditCardSearchDTO = new() { civilID = civilId } }))?.getCreditCardsResult;

        var creditCards = result?.AsQueryable().ProjectToType<CreditCardResponse>().ToList();
        return response.Success(creditCards);
    }

}
