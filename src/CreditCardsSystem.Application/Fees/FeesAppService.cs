using CreditCardsSystem.Domain.Models.Fees;
using Kfh.Aurora.Integration;
using ServiceFeesManagementReference;

namespace CreditCardsSystem.Application.Fees;
public class FeesAppService : IFeesAppService, IAppService
{
    private readonly ServiceFeesManagementClient _serviceFeesManagementClient;

    public FeesAppService(IIntegrationUtility integrationUtility, IOptions<IntegrationOptions> options)
    {
        _serviceFeesManagementClient = integrationUtility.GetClient<ServiceFeesManagementClient>(options.Value.Client, options.Value.Endpoints.ServiceFeesManagement, options.Value.BypassSslValidation);
    }
    [HttpPost]
    public async Task<ApiResponseModel<ServiceFeesResponse>> GetServiceFee([FromBody] ServiceFeesRequest request)
    {
        var response = new ApiResponseModel<ServiceFeesResponse>();
        var result = (await _serviceFeesManagementClient.getServiceFeesAsync(new()
        {
            getServiceFeesReqDTO = new()
            {
                serviceName = request.ServiceName,
                serviceAccountNo = request.DebitAccountNumber
            }
        }))?.getServiceFees;

        if (!string.IsNullOrEmpty(result?.errorDesc))
            return response.Fail(result.errorDesc);


        return response.Success(new()
        {
            VatPercentage = Convert.ToDecimal(result?.vatPercentage ?? 0),
            Fees = Convert.ToDecimal(result?.fees ?? 0),
            IsVatApplicable = result?.isVatApplicable ?? false,
        });
    }

    [HttpPost]
    public async Task<ApiResponseModel<ServiceFeesResponse>> PostServiceFee([FromBody] PostServiceFeesRequest request)
    {
        var response = new ApiResponseModel<ServiceFeesResponse>();
        var feeRequest = new postServiceFeesReqDTO()
        {
            serviceName = request.ServiceName,
            debitAccountNo = request.DebitAccountNumber
        };

        if (request.OverwriteFeesAmount is not null)
            feeRequest.overwriteFeesAmount = Convert.ToDouble(request.OverwriteFeesAmount);

        if (request.OriginalFeesAmount is not null)
            feeRequest.originalFeesAmount = Convert.ToDouble(request.OriginalFeesAmount);

        if (request.OverwriteReason is not null)
            feeRequest.overwriteReason = request.OverwriteReason;

        if (request.OverwriteFeesAmountSpecified is not null)
            feeRequest.overwriteFeesAmountSpecified = request.OverwriteFeesAmountSpecified ?? false;

        if (request.OriginalFeesAmountSpecified is not null)
            feeRequest.originalFeesAmountSpecified = request.OriginalFeesAmountSpecified ?? false;

        var result = (await _serviceFeesManagementClient.postServiceFeesAsync(new()
        {
            postServiceFeeReqDTO = feeRequest
        }))?.postServiceFees;

        if (result?.isSuccessful ?? false)
            return response.Success(new() { TransRefNumber = result.transRefno });

        return response.Fail(result?.errorDesc ?? string.Empty);
    }

}
