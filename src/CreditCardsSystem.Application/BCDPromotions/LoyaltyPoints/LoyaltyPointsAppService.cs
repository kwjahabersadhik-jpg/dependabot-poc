using CreditCardsSystem.Data;
using CreditCardsSystem.Domain.Models.BCDPromotions.LoyaltyPoints;
using CreditCardsSystem.Domain.Shared.Entities.PromoEntities;
using CreditCardsSystem.Domain.Shared.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CreditCardsSystem.Application.BCDPromotions.LoyaltyPoints;

public class LoyaltyPointsAppService : ILoyaltyPointsAppService, IAppService
{
    private readonly FdrDBContext _fdrDbContext;
    private readonly IHttpContextAccessor _contextAccessor;

    public LoyaltyPointsAppService(FdrDBContext fdrDbContext, IHttpContextAccessor contextAccessor)
    {
        _fdrDbContext = fdrDbContext;
        _contextAccessor = contextAccessor;
    }

    [HttpGet]
    public async Task<ApiResponseModel<PointDto>> GetPoints()
    {
        var point = (await _fdrDbContext.Loyaltypointssetups.FirstOrDefaultAsync() ?? new Loyaltypointssetup()).Adapt<PointDto>();
        return new ApiResponseModel<PointDto>().Success(point);
    }

    [HttpPost]
    public async Task<ApiResponseModel<PointDto>> MakeRequest([FromBody] PointDto point)
    {
        if (point.CostPerPointTemp == null || point.InternationalPointsTemp == null || point.LocalPointsTemp == null)
            return new ApiResponseModel<PointDto>().Fail("you don't have any pending request");

        var userId = _contextAccessor.HttpContext?.User?.Claims.Single(x => x.Type == "sub").Value;
        var dbPoint = new Loyaltypointssetup();

        if (point.Id > 0)
            dbPoint = await _fdrDbContext.Loyaltypointssetups.FirstOrDefaultAsync(p => p.Id == point.Id);
        else
            dbPoint = point.Adapt<Loyaltypointssetup>();

        dbPoint!.MakerId = Convert.ToDecimal(userId);
        dbPoint.MakedOn = DateTime.Now;
        dbPoint.CostPerPointTemp = point.CostPerPointTemp;
        dbPoint.InternationalPointsTemp = point.InternationalPointsTemp;
        dbPoint.LocalPointsTemp = point.LocalPointsTemp;

        if (point.Id <= 0)
            _fdrDbContext.Loyaltypointssetups.Add(dbPoint);

        await _fdrDbContext.SaveChangesAsync();


        return new ApiResponseModel<PointDto>().Success(point);
    }

    [HttpPost]
    public async Task<ApiResponseModel<PointDto>> AcceptOrReject([FromBody] PointDto point, bool isAccept)
    {
        if (point.CostPerPointTemp == null || point.InternationalPointsTemp == null || point.LocalPointsTemp == null)
            return new ApiResponseModel<PointDto>().Fail("you don't have any pending request");

        var userId = _contextAccessor.HttpContext?.User?.Claims.Single(x => x.Type == "sub").Value;
        var dbPoint = await _fdrDbContext.Loyaltypointssetups.FirstOrDefaultAsync(p => p.Id == point.Id);

        if (dbPoint!.MakerId == int.Parse(userId!))
            return new ApiResponseModel<PointDto>().Fail("The maker of the request can't be the checker");

        dbPoint!.CheckerId = Convert.ToDecimal(userId);
        dbPoint.CheckedOn = isAccept ? DateTime.Now : null;

        if (isAccept)
        {
            dbPoint.CostPerPoint = point.CostPerPointTemp;
            dbPoint.InternationalPoints = point.InternationalPointsTemp!.Value;
            dbPoint.LocalPoints = point.LocalPointsTemp!.Value;
        }
        else
        {
            dbPoint.CostPerPoint = point.CostPerPoint;
            dbPoint.InternationalPoints = point.InternationalPoints;
            dbPoint.LocalPoints = point.LocalPoints;
        }

        dbPoint.CostPerPointTemp = null;
        dbPoint.InternationalPointsTemp = null;
        dbPoint.LocalPointsTemp = null;

        await _fdrDbContext.SaveChangesAsync();

        return new ApiResponseModel<PointDto>().Success(point);
    }

}