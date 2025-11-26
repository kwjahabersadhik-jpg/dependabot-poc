using CreditCardsSystem.Data;
using CreditCardsSystem.Data.Models;
using CreditCardsSystem.Domain.Models.BCDPromotions.Requests;
using CreditCardsSystem.Domain.Shared.Entities.PromoEntities;
using CreditCardsSystem.Domain.Shared.Enums;
using CreditCardsSystem.Domain.Shared.Interfaces;
using Kfh.Aurora.Organization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequestStatus = CreditCardsSystem.Domain.Shared.Enums.RequestStatus;


namespace CreditCardsSystem.Application.BCDPromotions.Requests;

public class RequestsOpsAppService(
        FdrDBContext fdrDbContext,
        IDistributedCache cache,
        IHttpContextAccessor contextAccessor,
        IOrganizationClient organizationClient,
        ILogger<RequestsOpsAppService> logger,
        IRequestsHelperMethods requestsHelperMethods)
    : IAppService, IRequestsOpsAppService
{
    [HttpGet]
    public async Task<ApiResponseModel<List<RequestActivityDto>>> GetRequestsByCriteria(RequestsSearchCriteria searchCriteria)
    {
        var requests = fdrDbContext.PromoRequestActivities
            .Include(r => r.RequestActivityDetails)
            .Where(r => r.ActivityStatusId != (int)ActivityStatus.NotSet &&
                        r.ActivityTypeId != (int)ActivityType.NotSet &&
                        r.ActivityFormId != (int)ActivityForm.NotSet)
            .AsQueryable();

        var re = requests.OrderByDescending(r => r.CreationDate).ToList();

        if (searchCriteria.RequestId != 0)
            requests = requests.Where(r => r.RequestActivityId == searchCriteria.RequestId);

        if (searchCriteria.ActivityForm != ActivityForm.NotSet)
            requests = requests.Where(r => r.ActivityFormId == (int)searchCriteria.ActivityForm);

        if (searchCriteria.ActivityStatus != ActivityStatus.NotSet)
            requests = requests.Where(r => r.ActivityStatusId == (int)searchCriteria.ActivityStatus);

        if (searchCriteria.ActivityType != ActivityType.NotSet)
            requests = requests.Where(r => r.ActivityTypeId == (int)searchCriteria.ActivityType);

        if (searchCriteria.MakerID != 0)
            requests = requests.Where(r => r.MakerId == searchCriteria.MakerID);

        var requestsList = (await requests.OrderByDescending(r => r.CreationDate)
            .ToListAsync())
            .Adapt<List<RequestActivityDto>>();

        return new ApiResponseModel<List<RequestActivityDto>>().Success(requestsList);
    }

    [HttpGet]
    public async Task<ApiResponseModel<List<RequestActivityDetailsDto>>> GetRequestDetailsById(long reqId)
    {
        var reqDetails = await fdrDbContext.PromoRequestActivityDetails.Where(r => r.RequestActivityId == reqId).ToListAsync();
        var reqDetailsDto = reqDetails.Adapt<List<RequestActivityDetailsDto>>();
        return new ApiResponseModel<List<RequestActivityDetailsDto>>().Success(reqDetailsDto);
    }

    [HttpPost]
    public async Task<ApiResponseModel<object>> Approve([FromBody] ApproveRequestDto approveRequestDto)
    {
        var approvalResponse = new ApiResponseModel<object>();

        var reqId = approveRequestDto.RequestDetails.FirstOrDefault()!.RequestActivityId;
        var userId = contextAccessor.HttpContext?.User?.Claims.Single(x => x.Type == "sub").Value;

        var request = await fdrDbContext.PromoRequestActivities
            .Include(requestActivity => requestActivity.RequestActivityDetails)
            .FirstOrDefaultAsync(r => r.RequestActivityId == reqId)!;

        if (request!.MakerId == int.Parse(userId!))
            return new ApiResponseModel<object>().Fail("The maker of the request can't be the checker");

        if (request!.Islocked.HasValue && request.Islocked.Value)
            return new ApiResponseModel<object>().Fail("Request is locked; Another checker started the approval");

        var user = await organizationClient.GetUser(userId!);
        var userName = user!.FirstName + " " + user.LastName;


        var transaction = await fdrDbContext.Database.BeginTransactionAsync();

        try
        {
            //update request object
            request!.Islocked = true;
            request.ActivityStatusId = (int)ActivityStatus.Approved;
            request.LastUpdateDate = DateTime.Now;
            request.CheckerId = int.Parse(userId!);
            request.CheckerName = userName;
            await fdrDbContext.SaveChangesAsync();

            //call the approval object helper method to approve the request whether to (add - edit - delete) data in a specific table
            var requestApprovalHelperObject = GetRequestApprovalHelperObject(approveRequestDto.ActivityForm);
            requestApprovalHelperObject.SetFields(fdrDbContext, requestsHelperMethods);
            approvalResponse = await requestApprovalHelperObject.Approve(approveRequestDto);

            if (!approvalResponse.IsSuccess)
                return new ApiResponseModel<object>().Fail("something went wrong during approving the request");

            if (request.ActivityTypeId == (decimal)ActivityType.Edit)
                await UnLockRecord(request.Adapt<RequestActivityDto>());

            await transaction.CommitAsync();

            if (approveRequestDto.ActivityForm is ActivityForm.CardDef or ActivityForm.CardEligibilityMatrix)
            {
                await cache.RemoveAsync("allProducts");
            }

            return approvalResponse;
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            logger.LogError(e, nameof(Approve));
            return new ApiResponseModel<object>().Fail("something went wrong during approving the request");
        }

    }

    [HttpGet]
    public async Task<ApiResponseModel<object>> DeleteOrReject(string reason, RequestStatus requestStatus, long requestId)
    {
        if (string.IsNullOrEmpty(reason))
            return new ApiResponseModel<object>().Fail("you must enter a reason");

        await using var transaction = await fdrDbContext.Database.BeginTransactionAsync();

        try
        {
            var userId = contextAccessor.HttpContext?.User?.Claims.Single(x => x.Type == "sub").Value;

            var request = fdrDbContext.PromoRequestActivities
                .Include(r => r.RequestActivityDetails)
                .FirstOrDefault(r => r.RequestActivityId == requestId);

            request!.Reason = reason;
            request.ActivityStatusId = (byte)requestStatus;
            request.LastUpdateDate = DateTime.Today;
            request.Islocked = false;

            if (requestStatus == RequestStatus.Rejected)
            {
                if (request.MakerId.ToString() == userId)
                    return new ApiResponseModel<object>().Fail("The maker of the request can't be the checker");

                var user = await organizationClient.GetUser(userId!);
                var userName = user!.FirstName + " " + user.LastName;

                request.CheckerId = int.Parse(userId!);
                request.CheckerName = userName;
            }
            else if (requestStatus == RequestStatus.Deleted)
            {
                if (request.MakerId.ToString() != userId)
                    return new ApiResponseModel<object>().Fail("only the maker of the request can delete it");
            }

            await fdrDbContext.SaveChangesAsync();

            await UnLockRecord(request.Adapt<RequestActivityDto>());

            await transaction.CommitAsync();

            return new ApiResponseModel<object>().Success(null);
        }
        catch (Exception e)
        {
            //await transaction.RollbackAsync();
            logger.LogError(e, nameof(DeleteOrReject));
            return new ApiResponseModel<object>().Fail("something went wrong during deleting/rejecting the request");
        }

    }

    private async Task UnLockRecord(RequestActivityDto request)
    {

        if (request!.ActivityType == ActivityType.Add) return;

        if (request.ActivityForm == ActivityForm.CardEligibilityMatrix)
        {
            var id = request.RequestActivityDetails
                .FirstOrDefault(d => d.Parameter.Equals("id", StringComparison.OrdinalIgnoreCase))?.Value;

            var result = await fdrDbContext.CardtypeEligibilityMatixes
                .Where(c => c.Id.ToString() == id)
                .ExecuteUpdateAsync(s =>
                    s.SetProperty(p => p.Islocked, p => false));
        }
        else if (request.ActivityForm == ActivityForm.CardDef)
        {
            var id = request.RequestActivityDetails
                .FirstOrDefault(d => d.Parameter.Equals("CardType", StringComparison.OrdinalIgnoreCase))?.Value;

            var result = await fdrDbContext.CardDefs
                .Where(c => c.CardType.ToString() == id)
                .ExecuteUpdateAsync(s =>
                    s.SetProperty(p => p.Islocked, p => false));
        }
        else if (request.ActivityForm == ActivityForm.EligiblePromotions)
        {
            var id = request.RequestActivityDetails
                .FirstOrDefault(d => d.Parameter.Equals("PromotionCardId", StringComparison.OrdinalIgnoreCase))?.Value;

            var result = await fdrDbContext.PromotionCards
                .Where(p => p.PromotionCardId.ToString() == id)
                .ExecuteUpdateAsync(s =>
                    s.SetProperty(p => p.Islocked, p => false));
        }
        else if (request.ActivityForm == ActivityForm.GroupAttribute)
        {
            var id = request.RequestActivityDetails
                .FirstOrDefault(d => d.Parameter.Equals("AttributeID", StringComparison.OrdinalIgnoreCase))?.Value;

            var result = await fdrDbContext.GroupAttributes
                .Where(g => g.AttributeId.ToString() == id)
                .ExecuteUpdateAsync(s =>
                    s.SetProperty(p => p.Islocked, p => false));
        }
        else if (request.ActivityForm == ActivityForm.PCT)
        {
            var id = request.RequestActivityDetails
                .FirstOrDefault(d => d.Parameter.Equals("PCTID", StringComparison.OrdinalIgnoreCase))?.Value;

            var result = await fdrDbContext.Pcts
                .Where(p => p.PctId.ToString() == id)
                .ExecuteUpdateAsync(s =>
                    s.SetProperty(p => p.Islocked, p => false));
        }
        else if (request.ActivityForm == ActivityForm.Promotion)
        {
            var id = request.RequestActivityDetails
                .FirstOrDefault(d => d.Parameter.Equals("PromotionID", StringComparison.OrdinalIgnoreCase))?.Value;

            var result = await fdrDbContext.Promotions
                .Where(p => p.PromotionId.ToString() == id)
                .ExecuteUpdateAsync(s =>
                    s.SetProperty(p => p.Islocked, p => false));
        }
        else if (request.ActivityForm == ActivityForm.PromotionGroup)
        {
            var id = request.RequestActivityDetails
                .FirstOrDefault(d => d.Parameter.Equals("GroupID", StringComparison.OrdinalIgnoreCase))?.Value;

            var result = await fdrDbContext.PromotionGroups
                .Where(g => g.GroupId.ToString() == id)
                .ExecuteUpdateAsync(s =>
                    s.SetProperty(p => p.Islocked, p => false));
        }
        else if (request.ActivityForm == ActivityForm.Services)
        {
            var id = request.RequestActivityDetails
                .FirstOrDefault(d => d.Parameter.Equals("ServiceID", StringComparison.OrdinalIgnoreCase))?.Value;

            var result = await fdrDbContext.Services
                .Where(g => g.ServiceId.ToString() == id)
                .ExecuteUpdateAsync(s =>
                    s.SetProperty(p => p.Islocked, p => false));
        }

    }

    private IRequestApprovalHelpers GetRequestApprovalHelperObject(ActivityForm requestActivityForm)
    {
        if (requestActivityForm == ActivityForm.CardEligibilityMatrix)
            return Activator.CreateInstance<RequestApprovalHelpers<CardtypeEligibilityMatix>>();

        if (requestActivityForm == ActivityForm.CardDef)
            return Activator.CreateInstance<RequestApprovalHelpers<CardDefinition>>();

        if (requestActivityForm == ActivityForm.EligiblePromotions)
            return Activator.CreateInstance<RequestApprovalHelpers<PromotionCard>>();

        if (requestActivityForm == ActivityForm.GroupAttribute)
            return Activator.CreateInstance<RequestApprovalHelpers<GroupAttribute>>();

        if (requestActivityForm == ActivityForm.PCT)
            return Activator.CreateInstance<RequestApprovalHelpers<Pct>>();

        if (requestActivityForm == ActivityForm.Promotion)
            return Activator.CreateInstance<RequestApprovalHelpers<Promotion>>();

        if (requestActivityForm == ActivityForm.PromotionGroup)
            return Activator.CreateInstance<RequestApprovalHelpers<PromotionGroup>>();

        return Activator.CreateInstance<RequestApprovalHelpers<Service>>();

    }
}