using CreditCardsSystem.Domain.Interfaces;
using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.BCDPromotions.LoyaltyPoints;
using Refit;

namespace CreditCardsSystem.Domain.Shared.Interfaces;

public interface ILoyaltyPointsAppService : IRefitClient
{
    const string Controller = "/api/LoyaltyPoints/";

    [Get($"{Controller}{nameof(GetPoints)}")]
    Task<ApiResponseModel<PointDto>> GetPoints();

    [Post($"{Controller}{nameof(AcceptOrReject)}")]
    Task<ApiResponseModel<PointDto>> AcceptOrReject([Body] PointDto point, bool isAccept);

    [Post($"{Controller}{nameof(MakeRequest)}")]
    Task<ApiResponseModel<PointDto>> MakeRequest([Body] PointDto point);

}