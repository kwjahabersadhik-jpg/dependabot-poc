using CreditCardsSystem.Data;
using CreditCardsSystem.Domain.Models.BCDPromotions.Requests;
using CreditCardsSystem.Domain.Shared.Interfaces;
using CreditCardsSystem.Domain.Shared.Models.BCDPromotions.Collaterals;
using CreditCardsSystem.Domain.Shared.Models.BCDPromotions.EligiblePromotions;

namespace CreditCardsSystem.Application.BCDPromotions.Collaterals
{
    public class CollateralAppService : IAppService, ICollateralAppService
    {
        private readonly FdrDBContext _fdrDbContext;

        public CollateralAppService(FdrDBContext fdrDbContext)
        {
            _fdrDbContext = fdrDbContext;
        }

        [HttpGet]
        public async Task<ApiResponseModel<List<CollateralDto>>> GetCollaterls()
        {
            var collaterlas = _fdrDbContext.Collaterals.Select(c => new CollateralDto
            {
                Collateralid = c.Collateralid,
                Descriptionen = c.Descriptionen,
                Descriptionar = c.Descriptionar,
                Issuingoption = c.Issuingoption
            }).ToList();

            return await Task.FromResult(new ApiResponseModel<List<CollateralDto>>().Success(collaterlas));
        }

        [HttpPost]
        public Task<ApiResponseModel<AddRequestResponse>> AddRequest(RequestDto<EligiblePromotionDto> request)
        {
            throw new NotImplementedException();
        }

        [HttpPut]
        public Task<ApiResponseModel<AddRequestResponse>> UpdateLockStatus(long id)
        {
            throw new NotImplementedException();
        }
    }
}
