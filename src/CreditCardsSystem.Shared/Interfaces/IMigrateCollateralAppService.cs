using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.CardOperation;
using Refit;

namespace CreditCardsSystem.Domain.Interfaces;

public interface IMigrateCollateralAppService : IRefitClient
{
    const string Controller = "/api/MigrateCollateral/";

    [Post($"{Controller}{nameof(ProcessMigrateCollateral)}")]

    Task<ApiResponseModel<ProcessResponse>> ProcessMigrateCollateral(ProcessMigrateCollateralRequest request);


    [Post($"{Controller}{nameof(RequestMigrateCollateral)}")]
    Task<ApiResponseModel<MigrateCollateralResponse>> RequestMigrateCollateral(MigrateCollateralRequest request);

}
