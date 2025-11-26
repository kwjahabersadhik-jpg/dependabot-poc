using CreditCardsSystem.Data;
using CreditCardsSystem.Domain.Models.Access;
using CreditCardsSystem.Domain.Shared.Interfaces;
using CreditCardsSystem.Domain.Shared.Models.BCDPromotions.Beneficiaries;
using Kfh.Aurora.Auth;
using Microsoft.EntityFrameworkCore;

namespace CreditCardsSystem.Application.BCDPromotions.Beneficiaries;

public class BeneficiaryAppService : IBeneficiaryAppService, IAppService
{
    private readonly FdrDBContext _fdrDbContext;
    private readonly IPromotionsAppService _promotionsAppService;
    private readonly IAuthManager _authManager;

    public BeneficiaryAppService(FdrDBContext fdrDbContext, IPromotionsAppService promotionsAppService, IAuthManager authManager)
    {
        _fdrDbContext = fdrDbContext;
        _promotionsAppService = promotionsAppService;
        _authManager = authManager;
    }

    [HttpGet]
    public async Task<ApiResponseModel<List<BeneficiaryDto>>> Get(string civilId)
    {
        var hasViewPermission = _authManager.HasPermission(Permissions.PromotionsBeneficiaries.View());
        if (!hasViewPermission)
            return new ApiResponseModel<List<BeneficiaryDto>>().Fail("you don't have this permission to view the beneficiaries");

        var promotions = await _promotionsAppService.GetPromotions();

        var beneficiaries = _fdrDbContext.PromotionBeneficiaries
            .Where(b => b.CivilId == civilId)
            .Select(b => new BeneficiaryDto
            {
                PromotionId = b.PromotionId,
                ApplicationDate = b.ApplicationDate,
                CardNo = b.CardNo,
                CivilId = b.CivilId,
                Remarks = b.Remarks
            })
            .OrderBy(b => b.PromotionId)
            .ToList();

        beneficiaries.ForEach(b => b.PromotionName = promotions.Data!.FirstOrDefault(p => p.PromotionId == b.PromotionId)!.PromotionName);

        return await Task.FromResult(new ApiResponseModel<List<BeneficiaryDto>>().Success(beneficiaries));
    }

    [HttpPost]
    public async Task<ApiResponseModel<object>> Delete([FromBody] BeneficiaryDto beneficiaryDto)
    {
        var hasDeletePermission = _authManager.HasPermission(Permissions.PromotionsBeneficiaries.Delete());
        if (!hasDeletePermission)
            return new ApiResponseModel<object>().Fail("you don't have this permission to delete this beneficiary");

        var beneficiary = await _fdrDbContext.PromotionBeneficiaries
                                                .FirstOrDefaultAsync(b => b.PromotionId == beneficiaryDto.PromotionId && b.CivilId == beneficiaryDto.CivilId);

        _fdrDbContext.PromotionBeneficiaries.Remove(beneficiary!);
        await _fdrDbContext.SaveChangesAsync();
        return new ApiResponseModel<object>().Success(new object());
    }

}