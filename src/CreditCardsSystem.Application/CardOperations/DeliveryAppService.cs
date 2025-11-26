using CreditCardsSystem.Data;
using CreditCardsSystem.Data.Models;
using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Models.CardOperation;
using CreditCardsSystem.Domain.Models.RequestDelivery;
using Microsoft.EntityFrameworkCore;

namespace CreditCardsSystem.Application.CardOperations;

public class DeliveryAppService : BaseApiResponse, IDeliveryAppService, IAppService
{

    #region Private Fields

    private readonly IRequestActivityAppService _requestActivityAppService;
    private readonly ICardDetailsAppService _cardDetailsAppService;
    private readonly ILookupAppService _lookupService;

    private readonly FdrDBContext _fdrDBContext;

    public DeliveryAppService(IRequestActivityAppService requestActivityAppService, ICardDetailsAppService cardDetailsAppService, FdrDBContext fdrDBContext, ILookupAppService lookupService)
    {
        _requestActivityAppService = requestActivityAppService;
        _cardDetailsAppService = cardDetailsAppService;
        _fdrDBContext = fdrDBContext;
        _lookupService = lookupService;
    }


    #endregion



    [HttpGet]
    public async Task<ApiResponseModel<CardDeliverResponse>> RequestDelivery(DeliveryOption? deliveryOption, decimal? oldToNewReqId, int? deliveryBranchId)
    {
        deliveryOption ??= DeliveryOption.COURIER;

        var newRequestDelivery = new RequestDeliveryDto()
        {
            CreateDate = DateTime.Now,
            RequestId = oldToNewReqId,
            DeliveryType = deliveryOption?.ToString() ?? ""
        };

        if (deliveryOption == DeliveryOption.BRANCH)
        {
            var deliveryBranch = (await _lookupService.GetAllBranches())?.Data?.FirstOrDefault(x => x.BranchId == deliveryBranchId);
            newRequestDelivery.DeliveryBranchId = deliveryBranchId;
            newRequestDelivery.DeliveryBranchName = deliveryBranch?.Name;
        }

        newRequestDelivery.RequestDeliveryStatusId = deliveryOption == DeliveryOption.BRANCH ? (int)DeliveryStatus.BRANCH_UNDER_DELIVERY_PROCESSING : (int)DeliveryStatus.COURIER_UNDER_DELIVERY_PROCESSING;

        await _fdrDBContext.RequestDeliveries.AddAsync(newRequestDelivery.Adapt<RequestDelivery>());
        await _fdrDBContext.SaveChangesAsync();

        return Success(new CardDeliverResponse() { IsActivityRequestCreated = true });
    }

    [HttpGet]
    public async Task<ApiResponseModel<CardDeliverResponse>> DeliverCard(string cardNumber)
    {
        var response = new ApiResponseModel<CardDeliverResponse>();

        var requestDelivery = await _fdrDBContext.RequestDeliveries.Include(x => x.Request).FirstOrDefaultAsync(x => x.Request.CardNo == cardNumber);
        //) ?? throw new ApiException(message: "Delivery not yet initiated");

        if (requestDelivery == null)
            return Failure<CardDeliverResponse>(message: "Delivery not yet initiated");

        if (IsAlreadyDelivered(requestDelivery.RequestDeliveryStatusId))
            return response.Success(new() { IsDelivered = true });

        await UpdateDeliveryStatus(requestDelivery);

        var cardInfo = (await _cardDetailsAppService.GetCardInfo(requestDelivery.RequestId))?.Data;
        //Prepare RequestActivityRequest
        await _requestActivityAppService.LogRequestActivity(new()
        {
            IssuanceTypeId = (int)cardInfo?.IssuanceType,
            BranchId = requestDelivery.Request.BranchId,
            CivilId = requestDelivery.Request.CivilId,
            RequestId = requestDelivery.RequestId,
            CfuActivityId = (int)CFUActivity.REQUEST_DELIVERY_STATUS,
            RequestActivityStatusId = requestDelivery.RequestDeliveryStatusId
        });

        return response.Success(new() { IsActivityRequestCreated = true });
    }

    private async Task UpdateDeliveryStatus(RequestDelivery requestDelivery)
    {
        requestDelivery.RequestDeliveryStatusId = requestDelivery.RequestDeliveryStatusId < 10 ?
            (int)Domain.Enums.DeliveryStatus.BRANCH_DELIVERED_TO_CUSTOMER :
            (int)Domain.Enums.DeliveryStatus.COURIER_DELIVERED_TO_CUSTOMER;

        await _fdrDBContext.SaveChangesAsync();
    }
    private static bool IsAlreadyDelivered(int requestDeliveryStatusId)
    {
        if (requestDeliveryStatusId is (int)Domain.Enums.DeliveryStatus.BRANCH_DELIVERED_TO_CUSTOMER or (int)Domain.Enums.DeliveryStatus.COURIER_DELIVERED_TO_CUSTOMER)
            return true;
        else
            return false;
    }
}

public record DeliveryRequest(DeliveryOption deliveryOption, decimal? oldToNewReqId, int? deliveryBranchId);