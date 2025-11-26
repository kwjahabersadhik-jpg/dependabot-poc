using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Models.Access;
using CreditCardsSystem.Domain.Models.CardOperation;
using CreditCardsSystem.Domain.Shared.Models.Reports;
using CreditCardsSystem.Domain.Shared.Models.RequestActivity;
using CreditCardsSystem.Utility.Extensions;
using CreditCardUpdateServiceReference;
using Kfh.Aurora.AccessControl;
using Kfh.Aurora.Auth;
using Kfh.Aurora.Integration;
using Kfh.Aurora.Logging;

namespace CreditCardsSystem.Application.CardOperations;
public class StopAndReportAppService(IAccessControlClient accessControlClient, IAuthManager authManager, IIntegrationUtility integrationUtility, IOptions<IntegrationOptions> options, ICardDetailsAppService cardDetailsAppService, IRequestActivityAppService requestActivityAppService, IAuditLogger<StopAndReportAppService> auditLogger) : BaseApiResponse, IStopAndReportAppService, IAppService
{
    #region Private Fields
    private readonly CreditCardUpdateServicesServiceClient _creditCardUpdateServiceClient = integrationUtility.GetClient<CreditCardUpdateServicesServiceClient>(options.Value.Client, options.Value.Endpoints.CreditCardUpdate, options.Value.BypassSslValidation);
    private readonly IAccessControlClient accessControlClient = accessControlClient;
    private readonly IAuthManager authManager = authManager;
    private readonly ICardDetailsAppService _cardDetailsAppService = cardDetailsAppService;
    private readonly IRequestActivityAppService _requestActivityAppService = requestActivityAppService;
    private readonly IAuditLogger<StopAndReportAppService> _auditLogger = auditLogger;

    #endregion

    [HttpPost]
    public async Task<ApiResponseModel<ProcessResponse>> ReportLostOrStolen([FromBody] StopCardRequest request)
    {

        if (!authManager.HasPermission(Permissions.ReportLostOrStolen.Approve()))
            return Failure<ProcessResponse>(GlobalResources.NotAuthorized);

        var cardInfoResult = await _cardDetailsAppService.GetCardInfo(request.RequestId, includeCardBalance: true);

        if (!cardInfoResult.IsSuccessWithData)
            throw new ApiException(message: "Invalid request Id");


        var cardInfo = cardInfoResult?.Data;

        //Direct Approval no need to create request
        return await Approve();

        #region local functions
        async Task<ApiResponseModel<ProcessResponse>> Approve()
        {
            await ApprovalValidation();
            string log = $" report card status from {ConfigurationBase.Status_LostOrStolen} to {CreditCardStatus.TemporaryClosed.ToString()} for {cardInfo.CardNumber}";

            try
            {
                await _creditCardUpdateServiceClient.reportCreditCardStatusAsync(new()
                {
                    cardNo = cardInfo.CardNumber,
                    cardStatus = ConfigurationBase.TemporaryStopStatus,
                });
            }
            catch (System.Exception ex)
            {
                _auditLogger.Log.Error("Failed to {log}", log);
                throw new System.Exception(message: $"Unable to report lost or stolen, please try again later! {ex.Message}", innerException: ex.InnerException);
            }


            _auditLogger.Log.Information($"Successfully {log}");
            return Success(new ProcessResponse() { CardNumber = cardInfo.CardNumber.Masked(6, 6), Message = $"Successfully {log}" });

            #region local functions
            async Task ApprovalValidation()
            {
                //TODO: TAMAgent only can do this action
                // throw new ApiException(message: "Not Authorized");

                //if (string.IsNullOrEmpty(cardInfo.ExternalStatusCode))
                //    throw new ApiException(message: "Invalid external status");

                if (cardInfo.CardBlockStatus == ConfigurationBase.Status_LostOrStolen && cardInfo.InternalStatus == ConfigurationBase.Status_Delinquent && cardInfo.InternalStatus == ConfigurationBase.Status_ChargeOff)
                    throw new ApiException(message: $"Unable to stop, due to Card block status is {ConfigurationBase.Status_LostOrStolen}  and internal status is {ConfigurationBase.Status_Delinquent}  and {ConfigurationBase.Status_ChargeOff}");


                if (cardInfo.PlasticActionEnum is not null && cardInfo.PlasticActionEnum != PlasticActions.NoAction || cardInfo.InternalStatus == ConfigurationBase.Status_Delinquent || cardInfo.InternalStatus == ConfigurationBase.Status_ChargeOff) // check internal status is not delinquent more than 90 days
                    throw new ApiException(message: $"Unable to stop, due to Card plastic action is {cardInfo.PlasticActionEnum}  and internal status is {ConfigurationBase.Status_Delinquent}  or {ConfigurationBase.Status_ChargeOff}");

                await Task.CompletedTask;
            }


            #endregion
        }
        #endregion
    }

    [HttpPost]
    public async Task<ApiResponseModel<ProcessResponse>> RequestStopCard([FromBody] StopCardRequest request)
    {
        bool isAuthorizedAppUser = authManager.HasPermission(Permissions.StopCard.Request());
        bool isAuthorizedExternalAppUser = await ExternalUserHasPermission(request, Permissions.StopCard.Request());

        if ((request.KfhId == null && !isAuthorizedAppUser) || (request.KfhId != null && !isAuthorizedExternalAppUser))
        {
            return Failure<ProcessResponse>(GlobalResources.NotAuthorized);
        }


        var cardInfoResult = await _cardDetailsAppService.GetCardInfo(request.RequestId);

        if (!cardInfoResult.IsSuccessWithData)
            throw new ApiException(message: "Invalid request Id");


        var cardInfo = cardInfoResult?.Data;

        //Direct Approval no need to create request
        return await Approve();

        #region local functions
        async Task<ApiResponseModel<ProcessResponse>> Approve()
        {
            await ApprovalValidation();

            try
            {
                await _creditCardUpdateServiceClient.reportCreditCardStatusAsync(new()
                {
                    cardNo = cardInfo.CardNumber,
                    cardStatus = ConfigurationBase.TemporaryStopStatus,
                });
            }
            catch (System.Exception ex)
            {
                _auditLogger.Log.Error("Failed to change card {cardNumber} status from {oldStatus} to {newStatus}", cardInfo?.CardNumber?.Masked(6, 6), cardInfo.CardStatus.ToString(), CreditCardStatus.TemporaryClosed.ToString());
                throw new System.Exception(message: $"Unable to stop this card, please try again later! {ex.Message}", innerException: ex.InnerException);
            }

            var requestActivity = new RequestActivityDto
            {

                IssuanceTypeId = (int)cardInfo.IssuanceType,
                CardType = cardInfo.CardType,
                CardNumber = cardInfo.CardNumber,
                CivilId = cardInfo.CivilId,
                RequestId = cardInfo.RequestId,
                CfuActivityId = (int)CFUActivity.Temporary_Closed,
                RequestActivityStatusId = (int)RequestActivityStatus.Approved,
                Details = new()
        {
            { ReportingConstants.KEY_OLD_CARD_STATUS,cardInfo.CardStatus.ToString()},
            { ReportingConstants.KEY_NEW_CARD_STATUS, CreditCardStatus.TemporaryClosed.ToString()}
        }
            };

            if (request.KfhId is not null)
                requestActivity.TellerId = (decimal)request.KfhId;

            await _requestActivityAppService.LogRequestActivity(requestActivity);

            var successMsg = $"Successfully changed card status from {cardInfo.CardStatus} to {CreditCardStatus.TemporaryClosed}";

            _auditLogger.Log.Information(successMsg);
            return Success(new ProcessResponse() { CardNumber = cardInfo.CardNumber.Masked(6, 6), Message = successMsg });

            #region local functions
            async Task ApprovalValidation()
            {
                //TODO: TAMAgent only can do this action
                if (!string.IsNullOrEmpty(cardInfo.ExternalStatusCode))
                    throw new ApiException(message: "Invalid external status");

                bool isDelinquentChargeOff = cardInfo.InternalStatus == ConfigurationBase.Status_Delinquent && cardInfo.InternalStatus == ConfigurationBase.Status_ChargeOff;

                if (cardInfo.PlasticActionEnum != PlasticActions.NoAction && isDelinquentChargeOff) // check internal status is not delinquent more than 90 days
                    throw new ApiException(message: $"Unable to stop, due to Card plastic action is {cardInfo.PlasticActionEnum}  and internal status is {ConfigurationBase.Status_Delinquent}  or {ConfigurationBase.Status_ChargeOff}");


                if (cardInfo.CardBlockStatus == ConfigurationBase.Status_LostOrStolen && !isDelinquentChargeOff)
                    throw new ApiException(message: $"Unable to stop, due to Card block status is {ConfigurationBase.Status_LostOrStolen}  and internal status is {ConfigurationBase.Status_Delinquent}  and {ConfigurationBase.Status_ChargeOff}");

                await Task.CompletedTask;
            }


            #endregion
        }
        #endregion
    }

    private async Task<bool> ExternalUserHasPermission(StopCardRequest request, string v)
    {
        return request.KfhId != null;

        //var permission = await accessControlClient.GetAllPermissions(request.KfhId.ToString()!, prefix: Permissions.StopCard);
        //return permission.Any(x => x.Name == Permissions.StopCard.Request());
    }
}
