using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Models.CardIssuance;
using CreditCardsSystem.Domain.Models.Employee;
using HrServiceReference;
using Kfh.Aurora.Integration;
using Kfh.Aurora.Organization;
using UserDto = CreditCardsSystem.Domain.Shared.Models.Account.UserDto;

namespace CreditCardsSystem.Application.EmployeeService;

public class EmployeeAppService : BaseApiResponse, IEmployeeAppService, IAppService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IOrganizationClient _organizationClient;
    private readonly HrServiceClient _hrServiceClient;

    //public User CurrentUser { get; set; } = null!;
    public EmployeeAppService(IHttpContextAccessor httpContextAccessor, IOrganizationClient organizationClient, IOptions<IntegrationOptions> options, IIntegrationUtility integrationUtility)
    {
        _httpContextAccessor = httpContextAccessor;
        _organizationClient = organizationClient;

        _hrServiceClient = integrationUtility.GetClient<HrServiceClient>
        (options.Value.Client, options.Value.Endpoints.Hr, options.Value.BypassSslValidation);
    }

    [HttpGet]
    public async Task<UserDto?> GetCurrentLoggedInUser(decimal? kfhId)
    {
        bool isHavingSub = _httpContextAccessor.HttpContext.User.Claims.Any(x => x.Type == "sub");
        var userId = isHavingSub ? _httpContextAccessor.HttpContext.User.Claims.Single(x => x.Type == "sub").Value : kfhId?.ToString();

        if (string.IsNullOrEmpty(userId))
            throw new ApiException(message: "Invalid User");

        return (await _organizationClient.GetUser(userId))?.Adapt<UserDto>();
    }

    [HttpGet]
    public async Task<ApiResponseModel<EmployeeInfo>> GetEmployeeNumberByAccountNumber(string accountNumber)
    {
        var employeeNumber = (await _hrServiceClient.getEmployeeNoByAcctAsync(new() { acctNo = accountNumber }))?.getEmployeeNoByAcctResult;

        if (string.IsNullOrEmpty(employeeNumber))
            return Failure<EmployeeInfo>(message: "This is not an employee account");

        ValidateSellerIdResponse? employeeInfo = new();
        try
        {
            employeeInfo = (await ValidateSellerId(employeeNumber))?.Data;
        }
        catch (System.Exception)
        {

        }

        return Success<EmployeeInfo>(new()
        {
            NameAr = employeeInfo?.NameAr ?? "",
            Gender = employeeInfo?.Gender ?? "",
            EmployeeNumber = employeeNumber
        });
    }

    [HttpGet]
    public async Task<ApiResponseModel<ValidateSellerIdResponse>> ValidateSellerId(string sellerId)
    {
        var response = new ApiResponseModel<ValidateSellerIdResponse>();
        User? ADUserInfo = null;
        try
        {
            ADUserInfo = await _organizationClient.GetUser(sellerId);

            if (ADUserInfo is null)
                return response.Fail("Invalid SellerId");
        }
        catch (System.Exception)
        {
            return response.Fail("Invalid SellerId");
        }


        int? gender = null!;
        string? employeeNameAr = null!;
        string? message = "Done!";
        try
        {
            var employeeInfo = (await _hrServiceClient.getEmployeeInfoAsync(new getEmployeeInfoRequest() { empNo = sellerId }))?.getEmployeeInfoResult;
            //if (employeeInfo is null)
            //    return response.Fail("Invalid SellerId");

            if (employeeInfo is not null)
            {
                if (employeeInfo.gender == "ذكر" || employeeInfo.gender.Equals("M", StringComparison.InvariantCultureIgnoreCase))
                    gender = 1;
                else if (employeeInfo.gender == "أنثى" || employeeInfo.gender.Equals("F", StringComparison.InvariantCultureIgnoreCase))
                    gender = 2;

                employeeNameAr = employeeInfo?.name ?? string.Empty;
            }
        }
        catch
        {
            message = "Unable to get employee arabic name & gender!";
        }
        return response.Success(new ValidateSellerIdResponse()
        {
            EmpNo = ADUserInfo.KfhId,
            NameAr = employeeNameAr,
            NameEn = ADUserInfo.Name,
            Gender = gender?.ToString(),
        }, message: message);

    }

}
