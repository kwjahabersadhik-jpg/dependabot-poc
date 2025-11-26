using CreditCardsSystem.Data;
using CreditCardsSystem.Domain.Models.BCDPromotions.Groups;
using CreditCardsSystem.Domain.Models.BCDPromotions.Requests;
using CreditCardsSystem.Domain.Models.CardIssuance;
using CreditCardsSystem.Domain.Shared.Enums;
using CreditCardsSystem.Domain.Shared.Interfaces;
using CreditCardsSystem.Domain.Shared.Models.BCDPromotions.CardDefinition;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CreditCardsSystem.Application.BCDPromotions.CardDefs;

public class CardDefsAppService : IAppService, ICardDefsAppService
{
    private readonly FdrDBContext _fdrDbContext;
    private readonly IGroupsAppService _groupsAppService;
    private readonly IRequestMaker<CardMatrixDto> _promotionRequestsAppService;
    private readonly ILogger<CardDefsAppService> _logger;
    private readonly IRequestMaker<PostCardDefDto> _cardDefRequestsAppService;

    public CardDefsAppService(FdrDBContext fdrDbContext,
        IGroupsAppService groupsAppService, IRequestMaker<CardMatrixDto> promotionRequestsAppService,
        ILogger<CardDefsAppService> logger, IRequestMaker<PostCardDefDto> cardDefRequestsAppService)
    {
        _fdrDbContext = fdrDbContext;
        _groupsAppService = groupsAppService;
        _promotionRequestsAppService = promotionRequestsAppService;
        _logger = logger;
        _cardDefRequestsAppService = cardDefRequestsAppService;
    }


    [HttpGet]
    public async Task<ApiResponseModel<List<CardDefinitionDto>>> GetCardsTypes()
    {

        var cardsTypes = (await _fdrDbContext.CardDefs
                .Include(c => c.CardDefExts)
                .OrderBy(c => c.Name)
                .ToListAsync())
            .Adapt<List<CardDefinitionDto>>();

        return new ApiResponseModel<List<CardDefinitionDto>>().Success(cardsTypes);
    }

    [HttpGet]
    public async Task<ApiResponseModel<CardDefinitionDto>> GetCardTypeById(int cardTypeId)
    {
        var cardType = (await _fdrDbContext.CardDefs
            .Include(c => c.CardDefExts)
            .FirstOrDefaultAsync(c => c.CardType == cardTypeId))
            .Adapt<CardDefinitionDto>();

        return new ApiResponseModel<CardDefinitionDto>().Success(cardType);

    }

    [HttpGet]
    public async Task<ApiResponseModel<List<CardMatrixDto>>> GetCardsMatrix()
    {
        var cardsMatrix = (await _fdrDbContext.CardtypeEligibilityMatixes.AsNoTracking().ToListAsync()).Adapt<List<CardMatrixDto>>();
        var cardsTypes = (await GetCardsTypes()).Data;
        var locations = await _groupsAppService.GetLocations();
        var customerClasses = await _groupsAppService.GetCustomerClass();

        foreach (var card in cardsMatrix)
        {
            card.CardTypeName = cardsTypes!.FirstOrDefault(c => c.CardType == card.CardType)!.Name;
            card.AllowedBranchesDesc = GetDesc(locations, card.AllowedBranches);
            card.AllowedClassCodeDesc = GetDesc(customerClasses, card.AllowedClassCode);
        }

        cardsMatrix = cardsMatrix.OrderBy(c => c.CardTypeName).ToList();
        return new ApiResponseModel<List<CardMatrixDto>>().Success(cardsMatrix);
    }

    [HttpPost]
    public async Task<bool> IsCardExist([FromBody] CardMatrixDto card)
    {
        var isCardExist = await _fdrDbContext.CardtypeEligibilityMatixes
            .AnyAsync(c => c.CardType == card.CardType && c.Id != card.ID);

        return isCardExist;
    }

    [HttpPost]
    public async Task<ApiResponseModel<AddRequestResponse>> AddCardMatrixRequest([FromBody] RequestDto<CardMatrixDto> request)
    {
        await using var transaction = await _fdrDbContext.Database.BeginTransactionAsync();
        try
        {
            if (request.ActivityType == (int)ActivityType.Add)
            {
                request.Title = "New Card Matrix";
                request.Description = $"New card matrix has been added for '{request.NewData.CardTypeName}'";
            }

            if (request.ActivityType == (int)ActivityType.Edit)
            {
                request.Title = "Updated Card Matrix";
                request.Description = $"The matrix data for '{request.NewData.CardTypeName}' has been updated";
            }

            if (request.ActivityType == (int)ActivityType.Delete)
            {
                request.Title = "Deleted Card Matrix";
                request.Description = $"The matrix data for '{request.NewData.CardTypeName}' has been deleted";
            }


            await _promotionRequestsAppService.AddRequest(request);

            switch (request.ActivityType)
            {
                case (int)ActivityType.Edit:
                    await UpdateCardMatrixLockStatus(request.OldData.ID);
                    break;

                case (int)ActivityType.Delete:
                    await UpdateCardMatrixLockStatus(request.NewData.ID);
                    break;
            }

            await transaction.CommitAsync();
            return new ApiResponseModel<AddRequestResponse>().Success(null);

        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            _logger.LogError(e, nameof(AddCardMatrixRequest));
            return new ApiResponseModel<AddRequestResponse>().Fail("something went wrong during adding the request");

        }
    }

    [HttpPost]
    public async Task<ApiResponseModel<AddRequestResponse>> AddCardDefinitionRequest([FromBody] RequestDto<PostCardDefDto> request)
    {
        var cardTypes = await _fdrDbContext.CardDefs.Select(c => c.CardType).ToListAsync();

        switch (request.ActivityType)
        {
            case (int)ActivityType.Add when cardTypes.Any(c => c == request.NewData.CardType):
                return new ApiResponseModel<AddRequestResponse>().Fail("card type is duplicated");
            case (int)ActivityType.Edit when (request.OldData.CardType != request.NewData.CardType) && (cardTypes.Any(c => c == request.NewData.CardType)):
                return new ApiResponseModel<AddRequestResponse>().Fail("card type is duplicated");
            default:
                {
                    await using var transaction = await _fdrDbContext.Database.BeginTransactionAsync();
                    try
                    {
                        if (request.ActivityType == (int)ActivityType.Add)
                        {
                            request.Title = "New Card Definition";
                            request.Description = $"New card definition has been added for '{request.NewData.Name}'";
                        }

                        if (request.ActivityType == (int)ActivityType.Edit)
                        {
                            request.Title = "Updated Card Definition";
                            request.Description = $"The card definition for {request.NewData.Name} has been updated";
                        }

                        if (request.ActivityType == (int)ActivityType.Delete)
                        {
                            request.Title = "Deleted Card Definition";
                            request.Description = $"The card definition for {request.NewData.Name} has been deleted";
                        }


                        await _cardDefRequestsAppService.AddRequest(request);

                        switch (request.ActivityType)
                        {
                            case (int)ActivityType.Edit:
                                await UpdateCardDefLockStatus(request.OldData.CardType!.Value);
                                break;

                            case (int)ActivityType.Delete:
                                await UpdateCardDefLockStatus(request.NewData.CardType!.Value);
                                break;
                        }

                        await transaction.CommitAsync();
                        return new ApiResponseModel<AddRequestResponse>().Success(null);

                    }
                    catch (Exception e)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogError(e, nameof(AddCardDefinitionRequest));
                        return new ApiResponseModel<AddRequestResponse>().Fail("something went wrong during adding the request");

                    }

                }
        }
    }

    [HttpGet]
    public async Task<ApiResponseModel<AddRequestResponse>> UpdateCardMatrixLockStatus(long id)
    {
        var result = await _fdrDbContext.CardtypeEligibilityMatixes
            .Where(c => c.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Islocked, p => true));

        return new ApiResponseModel<AddRequestResponse>() { IsSuccess = true };
    }

    [HttpGet]
    public async Task<ApiResponseModel<AddRequestResponse>> UpdateCardDefLockStatus(int id)
    {
        var result = await _fdrDbContext.CardDefs
            .Where(c => c.CardType == id)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Islocked, p => true));

        return new ApiResponseModel<AddRequestResponse>() { IsSuccess = true };
    }

    private string GetDesc(List<GroupAttributeLookupDto> lst, string value)
    {
        var desc = "";
        if (string.IsNullOrEmpty(value)) return desc;

        if (value == "0")
            desc = "All,";
        else
        {
            var values = value.Split(',');
            foreach (var branch in values)
            {
                var branchLookup = lst.FirstOrDefault(x => x.Value == branch);
                if (branchLookup != null)
                    desc += branchLookup.Attribute + ",";
            }
        }

        return desc[..^1];
    }


}