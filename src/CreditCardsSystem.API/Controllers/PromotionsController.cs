using CreditCardsSystem.Api;
using CreditCardsSystem.Data;
using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.Access;
using CreditCardsSystem.Domain.Models.BCDPromotions.Requests;
using CreditCardsSystem.Domain.Shared.Enums;
using CreditCardsSystem.Domain.Shared.Interfaces;
using Kfh.Aurora;
using Kfh.Aurora.AccessControl;
using Kfh.Aurora.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CreditCardsSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    [Authorize(AuthorizationConstants.Policies.ApiPolicy)]
    public class PromotionsController(FdrDBContext fdrDbContext,
        IDiscovery discovery,
        IConfiguration configuration,
        IRequestsOpsAppService requestsOpsAppService,
        IAccessControlClient accessControlClient,
        ILogger<PromotionsController> logger) : ControllerBase
    {

        [HttpGet]
        public async Task<IActionResult> GetPromotionsTasks(string kfhId)
        {
            logger.LogInformation("get promotions admin tasks by kfhId : {kfhId}", kfhId);

            if (string.IsNullOrEmpty(kfhId) || (int.TryParse(kfhId, out var userId) && userId <= 0))
                return BadRequest("invalid kfh employee id");

            var creditCardAppUrl = discovery.Get("app_creditcard");
            var creditCardClient = "creditCard";

            var userPermissionsResult = await accessControlClient.GetPermissions(kfhId, creditCardClient);

            var promotionsTasksResponse = await requestsOpsAppService.GetRequestsByCriteria(new RequestsSearchCriteria
            {
                ActivityStatus = ActivityStatus.Pending,
            });

            if (!promotionsTasksResponse.IsSuccess)
                return BadRequest(promotionsTasksResponse.Message);

            if (!promotionsTasksResponse.Data!.Any())
                return Ok(new List<TaskDto>());

            logger.LogInformation("admin promotions tasks {@tasks}", promotionsTasksResponse.Data.Select(x=> new { x.ActivityForm, x.ActivityStatus, x.MakerName }));

            var requests = (from pt in promotionsTasksResponse.Data!
                            where userPermissionsResult.Permissions.Contains(DetectTaskPermission(pt.ActivityForm))
                            let TitleDescription = GetTitleAndDescription(pt)
                            select new TaskDto()
                            {
                                ActionUrl = $"{creditCardAppUrl}/request/details?RequestId={pt.RequestActivityId.Encode()}",
                                Application = "CreditCard",
                                //Assignees = new List<Assignees>() { new("PERMISSION", DetectTaskPermission(r.ActivityForm) ) },
                                CreatedDate = pt.CreationDate,
                                Id = pt.RequestActivityId.ToString()!,
                                IsCompleted = false,
                                Title = TitleDescription.title,
                                Description = TitleDescription.description,
                                Status = "Open"
                            })
                .OrderByDescending(r => r.CreatedDate)
                .ToList();

            return Ok(requests);
        }


        private (string title, string description) GetTitleAndDescription(RequestActivityDto requestActivityDto)
        {
            var Title = requestActivityDto.RequestActivityDetails.FirstOrDefault(x => x.Parameter == "Title")?.Value ?? $"{requestActivityDto.ActivityFormName} {requestActivityDto.ActivityFormName}";
            var Description = requestActivityDto.RequestActivityDetails.FirstOrDefault(x => x.Parameter == "Description")?.Value ?? $"{requestActivityDto.ActivityFormName} {requestActivityDto.ActivityFormName}";

            return (Title, Description);
        }
        private string DetectTaskPermission(ActivityForm activityForm)
        {
            var permission = activityForm switch
            {
                ActivityForm.Promotion => Permissions.Promotions.Approve(),
                ActivityForm.EligiblePromotions => Permissions.EligiblePromotions.Approve(),
                ActivityForm.PromotionGroup => Permissions.PromotionGroup.Approve(),
                ActivityForm.GroupAttribute => Permissions.GroupAttributes.Approve(),
                ActivityForm.Services => Permissions.Services.Approve(),
                ActivityForm.PCT => Permissions.PCT.Approve(),
                ActivityForm.CardDef => Permissions.CardDefinitions.Approve(),
                ActivityForm.CardEligibilityMatrix => Permissions.CardEligibilityMatrix.Approve(),
                _ => "invalid activity form"
            }
                ;

            return permission;
        }

    }
}