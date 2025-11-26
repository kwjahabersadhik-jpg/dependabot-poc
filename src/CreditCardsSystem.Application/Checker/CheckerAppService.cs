using CreditCardsSystem.Domain.Models.CardOperation;
using Kfh.Aurora.Auth;
using Kfh.Aurora.Organization;
using Refit;

namespace CreditCardsSystem.Application.Checker;

public class CheckerAppService : BaseApiResponse, ICheckerAppService, IAppService
{
    private readonly ICustomerProfileAppService customerProfileAppService;
    private readonly IAuthManager authManager;
    private readonly IOrganizationClient organizationClient;

    public CheckerAppService(ICustomerProfileAppService customerProfileAppService, IAuthManager authManager, IOrganizationClient organizationClient)
    {
        this.customerProfileAppService = customerProfileAppService;
        this.authManager = authManager;
        this.organizationClient = organizationClient;
    }

    [HttpPost]
    public async Task<ApiResponseModel<ProcessResponse>> GetApproverList(string customerCivilId)
    {

        var profile = await customerProfileAppService.GetCustomerProfile(customerCivilId);
        if (!profile.IsSuccessWithData)
            return Failure<ProcessResponse>(message: "Invalid Customer Id");

        DateTime today = DateTime.Today;
        int age = today.Year - profile.Data!.DateOfBirth!.Value.Year;

        if (profile.Data!.DateOfBirth!.Value > today.AddYears(-age))
            age--;

        bool isMinor = age < 21;

        ///TODO: need to call organization client to fetch user by branchwise

        return Success<ProcessResponse>(new());
    }
}

public interface ICheckerAppService : IRefitClient
{
    const string Controller = "/api/Checker/";

    [Post($"{Controller}{nameof(GetApproverList)}")]
    Task<ApiResponseModel<ProcessResponse>> GetApproverList(string customerCivilId);
}
