using CreditCardsSystem.Data;
using CreditCardsSystem.Domain.Models.BCDPromotions.Requests;
using CreditCardsSystem.Domain.Shared.Enums;
using CreditCardsSystem.Domain.Shared.Interfaces;
using CreditCardsSystem.Domain.Shared.Models.BCDPromotions.EligiblePromotions;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;

namespace CreditCardsSystem.Application.BCDPromotions.EligiblePromotions;

public class EligiblePromotionsAppService : IEligiblePromotionsAppService, IAppService
{
    private readonly FdrDBContext _fdrDbContext;
    private readonly IRequestMaker<EligiblePromotionDto> _promotionRequestsAppService;
    private readonly ILogger<EligiblePromotionsAppService> _logger;
    private readonly string _promoConnectionString;


    public EligiblePromotionsAppService(FdrDBContext fdrDbContext, IConfiguration configuration,
        IRequestMaker<EligiblePromotionDto> promotionRequestsAppService,
        ILogger<EligiblePromotionsAppService> logger)
    {
        _fdrDbContext = fdrDbContext;
        _promotionRequestsAppService = promotionRequestsAppService;
        _logger = logger;
        _promoConnectionString = configuration.GetConnectionString("FdrOracleConnection")!;
    }

    [HttpGet]
    public async Task<ApiResponseModel<List<EligiblePromotionDto>>> GetEligibleProducts()
    {

        await using var conn = new OracleConnection(_promoConnectionString);

        var query = @"select p.PROMOTION_CARD_ID as PromotionCardID, p.PROMOTION_ID as PromotionID, p.CARD_TYPE as CardType, p.PCT_ID as PCTID
                            ,p.CollateralID,c.name as CardTypeName,col.DESCRIPTIONEN as CollateralName
                            ,pro.PROMOTION_NAME as PromotionName,pct.DESCRIPTION as PCTName,p.IsLocked
                             from PROMO.PROMOTION_CARD p
                            LEFT OUTER JOIN FDR.CARD_DEF c  ON p.CARD_TYPE = c.CARD_TYPE
                            LEFT OUTER JOIN PROMO.COLLATERAL col  ON p.COLLATERALID =col.COLLATERALID
                            inner JOIN PROMO.PROMOTION pro  ON p.PROMOTION_ID = pro.PROMOTION_ID
                            LEFT OUTER JOIN PROMO.PCT pct   ON p.PCT_ID =pct.PCT_ID
                            order by p.PROMOTION_ID, p.CARD_TYPE, p.PCT_ID";

        var result = (await conn.QueryAsync<EligiblePromotionDto>(query)).ToList();
        return new ApiResponseModel<List<EligiblePromotionDto>>().Success(result);
    }

    [HttpPost]
    public async Task<ApiResponseModel<AddRequestResponse>> AddRequest([FromBody] RequestDto<EligiblePromotionDto> request)
    {
        await using var transaction = await _fdrDbContext.Database.BeginTransactionAsync();
        try
        {

            if (request.ActivityType == (int)ActivityType.Add)
            {
                request.Title = "New Eligible Promotion";
                request.Description = $"New eligible promotion has been added for '{request.NewData.PromotionName}'";
            }

            if (request.ActivityType == (int)ActivityType.Edit)
            {
                request.Title = "Updated Eligible Promotion";
                request.Description = $"The eligible promotion for {request.NewData.PromotionName} has been updated";
            }

            if (request.ActivityType == (int)ActivityType.Delete)
            {
                request.Title = "Deleted Eligible Promotion";
                request.Description = $"The eligible promotion for {request.NewData.PromotionName} has been deleted";
            }

            await _promotionRequestsAppService.AddRequest(request);

            switch (request.ActivityType)
            {
                case (int)Domain.Shared.Enums.ActivityType.Edit:
                    await UpdateLockStatus(request.OldData.PromotionCardId);
                    break;

                case (int)Domain.Shared.Enums.ActivityType.Delete:
                    await UpdateLockStatus(request.NewData.PromotionCardId);
                    break;
            }

            await transaction.CommitAsync();
            return new ApiResponseModel<AddRequestResponse>().Success(null);

        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            _logger.LogError(e, nameof(AddRequest));
            return new ApiResponseModel<AddRequestResponse>().Fail("something went wrong during adding the request");

        }
    }

    [HttpPut]
    public async Task<ApiResponseModel<AddRequestResponse>> UpdateLockStatus(long id)
    {
        var result = await _fdrDbContext.PromotionCards
            .Where(p => p.PromotionCardId == id)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Islocked, p => true));

        return new ApiResponseModel<AddRequestResponse>() { IsSuccess = true };
    }

    [HttpPost]
    public async Task<bool> IsPromotionExist([FromBody] EligiblePromotionDto promotion)
    {
        var isPromotionExist = await _fdrDbContext.PromotionCards.AnyAsync(p => p.PromotionId == promotion.PromotionID &&
                                                                        p.PctId == promotion.PCTID &&
                                                                        p.Collateralid == promotion.CollateralID &&
                                                                        p.CardType == p.CardType &&
                                                                        p.PromotionCardId != promotion.PromotionCardId);

        return isPromotionExist;
    }


}
