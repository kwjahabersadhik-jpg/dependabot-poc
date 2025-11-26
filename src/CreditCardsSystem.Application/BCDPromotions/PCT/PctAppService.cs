using CreditCardsSystem.Data;
using CreditCardsSystem.Domain.Models.BCDPromotions.PCTs;
using CreditCardsSystem.Domain.Models.BCDPromotions.Requests;
using CreditCardsSystem.Domain.Shared.Enums;
using CreditCardsSystem.Domain.Shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CreditCardsSystem.Application.BCDPromotions.PCT;

public class PctAppService : IAppService, IPctAppService
{
    private readonly FdrDBContext _fdrDbContext;
    private readonly IRequestMaker<PctDto> _promotionRequestsAppService;
    private readonly ILogger<PctAppService> _logger;
    private readonly IServicesAppService _serviceAppService;


    public PctAppService(FdrDBContext fdrDbContext,
        IRequestMaker<PctDto> promotionRequestsAppService,
        ILogger<PctAppService> logger, IServicesAppService serviceAppService)
    {
        _fdrDbContext = fdrDbContext;
        _promotionRequestsAppService = promotionRequestsAppService;
        _logger = logger;
        _serviceAppService = serviceAppService;
    }


    [HttpGet]
    public async Task<ApiResponseModel<List<PctDto>>> GetPcts()
    {
        var services = await _serviceAppService.GetServices();
        var pcts = (await _fdrDbContext.Pcts.ToListAsync()).Adapt<List<PctDto>>().ToList();

        pcts.ForEach(p => p.ServiceNo = services.Data!.FirstOrDefault(s => s.ServiceId == p.ServiceId) != null ?
                    services.Data!.FirstOrDefault(s => s.ServiceId == p.ServiceId)!.ServiceNo : 0);

        return new ApiResponseModel<List<PctDto>>().Success(pcts);
    }

    [HttpGet]
    public async Task<ApiResponseModel<PctDto>> GetPctById(decimal PctId)
    {
        var services = await _serviceAppService.GetServices();
        var pct = (await _fdrDbContext.Pcts.AsNoTracking().FirstOrDefaultAsync(p => p.PctId == PctId)).Adapt<PctDto>();

        return new ApiResponseModel<PctDto>().Success(pct);
    }

    [HttpPost]
    public async Task<ApiResponseModel<AddRequestResponse>> AddRequest([FromBody] RequestDto<PctDto> request)
    {
        await using var transaction = await _fdrDbContext.Database.BeginTransactionAsync();
        try
        {


            if (request.ActivityType == (int)ActivityType.Add)
            {
                request.Title = "New PCT";
                request.Description = $"New PCT has been added '{request.NewData.Description}'";
            }


            if (request.ActivityType == (int)ActivityType.Edit)
            {
                request.Title = "Updated PCT";
                request.Description = $"The PCT has been updated with this description '{request.NewData.Description}'";
            }

            if (request.ActivityType == (int)ActivityType.Delete)
            {
                request.Title = "Deleted PCT";
                request.Description = $"The PCT for '{request.NewData.Description}' has been deleted";
            }

            await _promotionRequestsAppService.AddRequest(request);

            switch (request.ActivityType)
            {
                case (int)Domain.Shared.Enums.ActivityType.Edit:
                    await UpdateLockStatus(request.OldData.PctId);
                    break;

                case (int)Domain.Shared.Enums.ActivityType.Delete:
                    await UpdateLockStatus(request.NewData.PctId);
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
    public async Task<ApiResponseModel<AddRequestResponse>> UpdateLockStatus(decimal id)
    {
        var result = await _fdrDbContext.Pcts
            .Where(p => p.PctId == id)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Islocked, p => true));

        return new ApiResponseModel<AddRequestResponse>() { IsSuccess = true };
    }
}