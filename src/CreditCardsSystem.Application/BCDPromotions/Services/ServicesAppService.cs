using CreditCardsSystem.Data;
using CreditCardsSystem.Domain.Models.BCDPromotions.Requests;
using CreditCardsSystem.Domain.Models.BCDPromotions.Services;
using CreditCardsSystem.Domain.Shared.Enums;
using CreditCardsSystem.Domain.Shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CreditCardsSystem.Application.BCDPromotions.Services;

public class ServicesAppService : IAppService, IServicesAppService
{

    private readonly FdrDBContext _fdrDbContext;
    private readonly IRequestMaker<ServiceDto> _promotionRequestsAppService;
    private readonly ILogger<ServicesAppService> _logger;


    public ServicesAppService(FdrDBContext fdrDbContext,
        IRequestMaker<ServiceDto> promotionRequestsAppService,
        ILogger<ServicesAppService> logger)
    {
        _fdrDbContext = fdrDbContext;
        _promotionRequestsAppService = promotionRequestsAppService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ApiResponseModel<List<ServiceDto>>> GetServices()
    {
        var services = await _fdrDbContext.Services
                                    .Select(s => new ServiceDto
                                    {
                                        ServiceId = Convert.ToInt64(s.ServiceId),
                                        ServiceNo = s.ServiceNo,
                                        NoOfMonths = s.NoOfMonths,
                                        ServiceDescription = s.ServiceDescription!,
                                        IsLocked = s.Islocked,
                                    }).ToListAsync();

        return new ApiResponseModel<List<ServiceDto>>().Success(services);
    }

    [HttpPost]
    public async Task<ApiResponseModel<AddRequestResponse>> AddRequest([FromBody] RequestDto<ServiceDto> request)
    {
        await using var transaction = await _fdrDbContext.Database.BeginTransactionAsync();
        try
        {
            if (request.ActivityType == (int)ActivityType.Add)
            {
                request.Title = "New Service";
                request.Description = $"New service has been updated with this description '{request.NewData.ServiceDescription}'";
            }


            if (request.ActivityType == (int)ActivityType.Edit)
            {
                request.Title = "Updated Service";
                request.Description = $"The Service has been updated with this description '{request.NewData.ServiceDescription}'";
            }

            if (request.ActivityType == (int)ActivityType.Delete)
            {
                request.Title = "Deleted Service";
                request.Description = $"The Service for '{request.NewData.ServiceDescription}' has been deleted";
            }

            await _promotionRequestsAppService.AddRequest(request);

            switch (request.ActivityType)
            {
                case (int)Domain.Shared.Enums.ActivityType.Edit:
                    await UpdateLockStatus(request.OldData.ServiceId);
                    break;

                case (int)Domain.Shared.Enums.ActivityType.Delete:
                    await UpdateLockStatus(request.NewData.ServiceId);
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
        var result = await _fdrDbContext.Services
            .Where(p => p.ServiceId == id)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Islocked, p => true));

        return new ApiResponseModel<AddRequestResponse>() { IsSuccess = true };
    }
}