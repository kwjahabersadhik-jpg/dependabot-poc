using CreditCardsSystem.Api;
using CreditCardsSystem.Domain.Interfaces;
using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.CardIssuance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CreditCardsSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    [Authorize(AuthorizationConstants.Policies.ApiPolicy)]
    public class CardsIssueController : ControllerBase
    {

        private readonly ICardIssuanceAppService _cardIssuanceAppService;
        private readonly ICustomerProfileAppService _customerProfileAppService;
        public CardsIssueController(ICardIssuanceAppService cardIssuanceAppService, ICustomerProfileAppService customerProfileAppService)
        {
            this._cardIssuanceAppService = cardIssuanceAppService;
            this._customerProfileAppService = customerProfileAppService;
        }


        [Produces(typeof(ApiResponseModel<CardIssueResponse>))]
        [HttpPost(Name = "IssuePrepaidCard")]
        public async Task<IActionResult> IssuePrepaidCard([FromBody] PrepaidCardRequest request)
        {
            var result = await _cardIssuanceAppService.IssuePrepaidCard(request);
            return Ok(result);
        }

        [Produces(typeof(ApiResponseModel<EligibleCardResponse>))]
        [HttpPost(Name = "GetEligibleCards")]
        public async Task<IActionResult> GetEligibleCards([FromBody] EligibleCardRequest request)
        {
            var result = await _cardIssuanceAppService.GetEligibleCards(request);
            return Ok(result);
        }

        [Produces(typeof(ApiResponseModel<EligibleCardResponse>))]
        [HttpPost(Name = "GetEligibleCardsByCivilId")]
        public async Task<IActionResult> GetEligibleCardsByCivilId([FromBody] EligibleCardRequestByCivilId request)
        {
            var customerProfile = await _customerProfileAppService.GetCustomerProfileMinimal(new() { CivilId = request.CivilId });
            if (!customerProfile.IsSuccess)
            {
                return NotFound();
            }

            var profile = customerProfile.Data;

            var result = await _cardIssuanceAppService.GetEligibleCards(new EligibleCardRequest()
            {
                CivilId = request.CivilId,
                CustomerType = profile.CustomerType,
                RimCode = profile.RimCode?.ToString(),
                DateOfBirth = profile.BirthDate,
                KfhId = request.KfhId
            });

            return Ok(result);
        }

    }
}