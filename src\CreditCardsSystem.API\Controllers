using CreditCardsSystem.Domain.Interfaces;
using CreditCardsSystem.Domain.Models.CardDelivery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CreditCardsSystem.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    [Authorize(AuthorizationConstants.Policies.ApiPolicy)]

    public class CardDeliveryController
    {
        private readonly ICardDeliveryAppService _cardDeliveryAppService;
        public CardDeliveryController(ICardDeliveryAppService cardDeliveryAppService)
        {
            _cardDeliveryAppService = cardDeliveryAppService;
        }

        [Produces(typeof(List<CardDeliveryDto>))]
        [HttpGet(Name = nameof(GetCardDelivery))]
        public async Task<JsonResult> GetCardDelivery(string civilId)
        {
            var result = await _cardDeliveryAppService.GetCardDelivery(civilId);
            return new JsonResult(result);
        }
    }

}
