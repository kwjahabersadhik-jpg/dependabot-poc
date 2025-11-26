using CreditCardsSystem.Api;
using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Interfaces;
using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.Card;
using CreditCardsSystem.Domain.Models.CardDelivery;
using CreditCardsSystem.Domain.Models.CardOperation;
using CreditCardsSystem.Domain.Models.CardPayment;
using CreditCardsSystem.Domain.Models.Reports;
using CreditCardsSystem.Domain.Models.RequestActivity;
using CreditCardsSystem.Domain.Shared.Interfaces.Workflow;
using CreditCardsSystem.Utility.Extensions;
using Kfh.Aurora.Logging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace CreditCardsSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    [Authorize(AuthorizationConstants.Policies.ApiPolicy)]
    public class CardsController(ILogger<CardsController> logger, IConfiguration configuration, IWorkflowAppService workflowAppService, IRequestActivityAppService requestActivityAppService, ICustomerProfileAppService customerProfileAppService, ILookupAppService lookupAppService, ICardDetailsAppService cardDetailsAppService, ICardPaymentAppService cardPaymentAppService, IReportAppService reportAppService, IStopAndReportAppService stopAndReportAppService, IReplacementAppService replacementAppService, ICreditCardArtworkAppService creditCardArtworkAppService, ICardInquiryAppService cardInquiryAppService, IActivationAppService activationAppService, IAuditLogger<CardsController> auditLogger) : ControllerBase
    {
        private readonly ICustomerProfileAppService _customerProfileAppService = customerProfileAppService;
        private readonly ILookupAppService _lookupAppService = lookupAppService;
        private readonly ICardDetailsAppService _cardDetailsAppService = cardDetailsAppService;
        private readonly ICardPaymentAppService _cardPaymentAppService = cardPaymentAppService;
        private readonly IStopAndReportAppService _stopAndReportAppService = stopAndReportAppService;
        private readonly IReplacementAppService _replacementAppService = replacementAppService;
        private readonly ICreditCardArtworkAppService _creditCardArtworkAppService = creditCardArtworkAppService;
        private readonly ICardInquiryAppService _cardInquiryAppService = cardInquiryAppService;
        private readonly ILogger<CardsController> _logger = logger;
        private readonly IAuditLogger<CardsController> _auditLogger = auditLogger;
        private readonly IConfiguration configuration = configuration;
        private readonly IWorkflowAppService _workflowAppService = workflowAppService;
        private readonly IRequestActivityAppService _requestActivityAppService = requestActivityAppService;

        private readonly IActivationAppService _activationAppService = activationAppService;
        private readonly IReportAppService _reportAppService = reportAppService;

        [HttpGet(Name = "Inquiry")]
        public async Task<IActionResult> Inquiry(string civilId)
        {
            if (string.IsNullOrEmpty(civilId))
                return Ok(new ApiResponseModel<List<CardInquiryDto>>() { IsSuccess = false, Message = "Please enter civil id" });

            var result = await _cardInquiryAppService.Inquiry(civilId);
            return Ok(result);
        }


        [Produces(typeof(ApiResponseModel<List<CreditCardDto>>))]
        [HttpPost(Name = "GetAllCards")]
        public async Task<IActionResult> GetAllCards([FromBody] CustomerProfileSearchCriteria searchCriteria)
        {
            var result = await _customerProfileAppService.GetAllCards(searchCriteria);
            return Ok(result);
        }

        [Produces(typeof(ApiResponseModel<CardDetailsResponse>))]
        [HttpGet(Name = "GetCardInfo")]
        public async Task<IActionResult> GetCardInfo(string kfhId, decimal requestId, string CardNumber = "", bool includeCardBalance = false, CancellationToken cancellationToken = default)
        {
            var result = await _cardDetailsAppService.GetCardInfo(requestId, CardNumber, includeCardBalance, kfhId: kfhId, cancellationToken);
            return Ok(result);
        }

        [HttpGet(Name = "GetCardImage")]
        public async Task<IResult?> GetCardImage(string type)
        {
            var card = await _creditCardArtworkAppService.GetCardImage(type);
            if (card?.Image is null)
                return null;

            var ms = new MemoryStream(card.Image);
            return Results.File(ms, card.Extension);
        }

        [HttpGet(Name = "GetAllCardImages")]
        [Produces(typeof(ApiResponseModel<IEnumerable<CardImage?>>))]
        public async Task<IActionResult> GetAllCardImages()
        {
            var cardImages = await _creditCardArtworkAppService.GetAllCardImages();
            if (!cardImages.AnyWithNull())
                return NotFound();

            return Ok(cardImages); //this is Sara </3
        }

        [Produces(typeof(ApiResponseModel<CardStatusList>))]
        [HttpGet(Name = "GetCardStatus")]
        public async Task<IActionResult> GetCardStatus(StatusType type = StatusType.All)
        {
            var response = await _lookupAppService.GetCardStatus(type);
            if (!response.IsSuccess)
                return Ok(null);

            return Ok(response.Data);
        }

        [Produces(typeof(ApiResponseModel<CardPaymentResponse>))]
        [HttpPost(Name = "ExecuteCardPayment")]
        public async Task<IActionResult> ExecuteCardPayment(CardPaymentRequest request)
        {
            await request.ModelValidationAsync();
            var paymentResult = await _cardPaymentAppService.ExecuteCardPayment(request);
            return Ok(paymentResult);
        }


        [Produces(typeof(ApiResponseModel<ProcessResponse>))]
        [HttpPost(Name = "RequestStopCard")]
        public async Task<IActionResult> RequestStopCard(StopCardRequest request)
        {
            var stropCardResult = await _stopAndReportAppService.RequestStopCard(request);
            return Ok(stropCardResult);
        }


        [Produces(typeof(ApiResponseModel<CardReplacementResponse>))]
        [HttpPost(Name = "RequestCardReplacement")]
        public async Task<IActionResult> RequestCardReplacement(CardReplacementRequest request)
        {
            var stropCardResult = await _replacementAppService.RequestCardReplacement(request);
            return Ok(stropCardResult);
        }


        [Produces(typeof(ApiResponseModel<EFormResponse>))]
        [HttpPost(Name = "GenerateAfterSalesForm")]
        public async Task<IActionResult> GenerateAfterSalesForm(AfterSalesForm request)
        {
            var stropCardResult = await _reportAppService.GenerateAfterSalesForm(request);
            return Ok(stropCardResult);
        }


        [Produces(typeof(ApiResponseModel<string>))]
        [HttpPost(Name = "UpdateRequestWithTaskId")]
        public async Task<IActionResult> UpdateRequestWithTaskId(UpdateRequestActivityTask request)
        {
            var response = new ApiResponseModel<string>();
            try
            {
                _auditLogger.LogWithEvent(nameof(UpdateRequestWithTaskId)).Information("Update request from SSO. task Id {TaskId} , RequestActivityID {RequestActivityID}", request.TaskId, request.RequestActivityId);
                await _requestActivityAppService.UpdateSingleRequestActivityDetail(request.RequestActivityId, "TaskId", request.TaskId.ToString());
                return Ok(response.Success(""));
            }
            catch (Exception ex)
            {
                _auditLogger.LogWithEvent(nameof(UpdateRequestWithTaskId)).Error(ex, "failed UpdateRequestWithTaskId task Id {TaskId} , RequestActivityID {RequestActivityID}", request.TaskId, request.RequestActivityId);
                return Ok(response.Fail(new(ex.Message)));
            }
        }


        [AllowAnonymous]
        [Produces(typeof(ApiResponseModel<string>))]
        [HttpPost(Name = "UpdateWorkFlowWithTaskId")]
        public async Task<IActionResult> UpdateWorkFlowWithTaskId(ActivityProcessRequest request)
        {
            await request.ModelValidationAsync();

            var response = new ApiResponseModel<string>();
            try
            {
                var apiKeyHeader = HttpContext.Request.Headers["arccs-api-key"].ToString();

                if (!apiKeyHeader.Equals(configuration.GetValue<string>("SSO:arccs-api-key"), StringComparison.InvariantCultureIgnoreCase))
                {
                    _logger.LogError("Update WorkFlow With TaskId invalid apikey");
                    return Ok(response.Fail(new("Un-Authorized!")));
                }

                _auditLogger.LogWithEvent(nameof(UpdateWorkFlowWithTaskId)).Information("Update WorkFlow With TaskId {TaskId}, RequestActivityId {RequestActivityId}",  request.TaskId,request.RequestActivityId);

                await _requestActivityAppService.ValidateActivityWithWorkflow(request);


                await _requestActivityAppService.CompleteActivity(request, isFromSSO: true);
                return Ok(response.Success(""));
            }
            catch (Exception ex)
            {
                _auditLogger.LogWithEvent(nameof(UpdateWorkFlowWithTaskId)).Error(ex, "Unable to Update WorkFlow With {TaskId}, RequestActivityId {RequestActivityId}", request.TaskId, request.RequestActivityId);
                return Ok(response.Fail(new(ex.Message)));
            }
        }


        [Produces(typeof(ApiResponseModel<CardActivationStatus>))]
        [HttpPost(Name = "RequestCardReActivation")]
        public async Task<IActionResult> RequestCardReActivation(CardReActivationRequest request)
        {
            var requestCardReActivationResult = await _activationAppService.RequestCardReActivation(request);
            return Ok(requestCardReActivationResult);
        }
    }
}