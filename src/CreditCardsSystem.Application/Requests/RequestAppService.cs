using CreditCardDelegationServiceReference;
using CreditCardsSystem.Data;
using CreditCardsSystem.Data.Models;
using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Models.CardIssuance;
using CreditCardsSystem.Domain.Models.CardOperation;
using CreditCardsSystem.Domain.Models.Request;
using CreditCardsSystem.Domain.Models.UserSettings;
using CreditCardsSystem.Domain.Shared.Interfaces.Workflow;
using CreditCardsSystem.Domain.Shared.Models.Account;
using CreditCardsSystem.Utility.Extensions;
using HoldManagementServiceReference;
using Kfh.Aurora.Auth;
using Kfh.Aurora.Integration;
using Kfh.Aurora.Logging;
using Kfh.Aurora.Organization;
using Microsoft.EntityFrameworkCore;
using StandingOrderServiceReference;
using Telerik.DataSource.Extensions;
using RequestDto = CreditCardsSystem.Domain.Models.RequestDto;

namespace CreditCardsSystem.Application.Requests
{
    public class RequestAppService : BaseApiResponse, IRequestAppService, IAppService
    {
        private readonly FdrDBContext _fdrDBContext;
        private readonly StandingOrderServiceClient _standingOrderService;
        private readonly ICardPaymentAppService _cardPaymentAppService;
        private readonly ICardDetailsAppService _cardDetailsAppService;
        private readonly HoldManagementServiceClient _holdManagementServiceClient;
        private readonly CreditCardDelegationServicesServiceClient _delegateRequestClient;
        private readonly IAuditLogger<RequestAppService> _auditLogger;
        private readonly IAuthManager _authManager;
        private readonly IEmployeeAppService _employeeService;
        private readonly IOrganizationClient _organizationClient;
        private readonly IUserPreferencesClient _userPreferencesClient;
        private readonly IWorkflowAppService _workflowAppService;
        public List<UserPreferences>? UserPreferences { get; set; } = null!;
        public UserDto? CurrentUser { get; set; } = null!;



        public RequestAppService(FdrDBContext fdrDBContext, IOptions<IntegrationOptions> options, IIntegrationUtility integration, ICardPaymentAppService cardPaymentAppService, ICardDetailsAppService cardDetailsAppService, IAuditLogger<RequestAppService> auditLogger, IAuthManager authManager, IEmployeeAppService employeeService, IOrganizationClient organizationClient, IUserPreferencesClient userPreferencesClient, IWorkflowAppService workflowAppService)
        {
            _fdrDBContext = fdrDBContext;
            _standingOrderService = integration.GetClient<StandingOrderServiceClient>(options.Value.Client, options.Value.Endpoints.StandingOrder, options.Value.BypassSslValidation);
            _holdManagementServiceClient = integration.GetClient<HoldManagementServiceClient>(options.Value.Client, options.Value.Endpoints.HoldManagment, options.Value.BypassSslValidation);
            _delegateRequestClient = integration.GetClient<CreditCardDelegationServicesServiceClient>(options.Value.Client, options.Value.Endpoints.DelegateRequest, options.Value.BypassSslValidation);

            _cardPaymentAppService = cardPaymentAppService;
            _cardDetailsAppService = cardDetailsAppService;
            _auditLogger = auditLogger;
            _authManager = authManager;
            _employeeService = employeeService;
            _organizationClient = organizationClient;
            _userPreferencesClient = userPreferencesClient;
            _workflowAppService = workflowAppService;
        }

        [HttpGet]
        public async Task<UserDto> GetUserDetails(decimal? kfhId)
        {
            if (kfhId is null or 0)
                kfhId = Convert.ToDecimal(_authManager.GetUser()?.KfhId);

            CurrentUser = await _employeeService.GetCurrentLoggedInUser(kfhId);
            UserPreferences = await _userPreferencesClient.GetUserPreferences(kfhId.ToString()!);

            _ = int.TryParse(UserPreferences?.FromUserPreferences().DefaultBranchIdValue, out int _defaultBranchId);
            var defaultBranches = UserPreferences?.FromUserPreferences().UserBranches;

            if (!defaultBranches.AnyWithNull())
                defaultBranches = await _organizationClient.GetUserBranches(kfhId.ToString()!);

            var defaultBranch = defaultBranches?.FirstOrDefault(x => x.BranchId == _defaultBranchId);

            CurrentUser ??= new();
            CurrentUser.DefaultBranchId = _defaultBranchId;
            CurrentUser.DefaultBranchName = defaultBranch?.Name;
            return CurrentUser;
        }


        [HttpGet]
        public async Task<ApiResponseModel<string>> GetRequestIdByCardNumber(string cardNumber)
        {
            var response = new ApiResponseModel<string>();

            if (!decimal.TryParse(cardNumber, out decimal _cardNumber))
                return response.Fail("invalid card number");


            var creditCardRequest = await (from req in _fdrDBContext.Requests.AsNoTracking()
                                           join aub in _fdrDBContext.AubCardMappings.AsNoTracking() on req.CardNo equals aub.KfhCardNo into allreq
                                           from ar in allreq.DefaultIfEmpty()
                                           where req.CardNo == cardNumber || ar.AubCardNo == cardNumber
                                           select new RequestDto
                                           {
                                               RequestId = req.RequestId
                                           }).FirstOrDefaultAsync();


            if (creditCardRequest == null)
                return response.Fail("invalid card number");

            //var creditCardRequest = (await _fdrDBContext.Requests.AsNoTracking().Where(x => x.CardNo == cardNumber).FirstOrDefaultAsync())?.Adapt<RequestDto>();

            //if (creditCardRequest == null)
            //{
            //    var aubCard = await _fdrDBContext.AubCardMappings.AsNoTracking().FirstOrDefaultAsync(x => x.AubCardNo == cardNumber);

            //    if (aubCard == null)
            //        return response.Fail("invalid card number");

            //    creditCardRequest = (await _fdrDBContext.Requests.AsNoTracking().FirstOrDefaultAsync(x => x.CardNo == aubCard.KfhCardNo))?.Adapt<RequestDto>();

            //    if (creditCardRequest == null)
            //        return response.Fail("invalid card number");
            //}


            return response.Success(creditCardRequest.RequestId.ToString());
        }


        [HttpGet]
        public async Task<ApiResponseModel<RequestDto>> GetRequestDetailByCardNumber(string cardNumber)
        {
            var response = new ApiResponseModel<RequestDto>();

            if (!decimal.TryParse(cardNumber, out decimal _cardNumber))
                return response.Fail("invalid card number");

            var creditCardRequest = (await _fdrDBContext.Requests.AsNoTracking().Where(x => x.CardNo == cardNumber).FirstOrDefaultAsync())?.Adapt<RequestDto>();

            if (creditCardRequest == null) return response.Fail("invalid request id");

            //creditCardRequest.CardNo = creditCardRequest.CardNo.Masked(6, 6);

            creditCardRequest.Parameters = GetRequestParameter(creditCardRequest.RequestId);

            return response.Success(creditCardRequest);
        }

        [HttpPost]
        public async Task<ApiResponseModel<DelegateResponse>> DelegateRequest(DelegateRequest request)
        {

            var cardRequest = await _fdrDBContext.Requests.AsNoTracking().Where(x => x.RequestId == request.ReqId).FirstOrDefaultAsync();
            if (cardRequest == null)
                return Failure<DelegateResponse>("invalid request id");

            var response = await _delegateRequestClient.delegateAsync(new() { requestID = request.ReqId.ToString(), userID = request.KfhUserId.ToString() });
            string logMessage = $"Delegate Request - request id:{request.ReqId} - KFH User Id:{request.KfhUserId}";

            if (!response.delegateResult.isSuccessful)
            {
                _auditLogger.Log.Error("Failed {@logMessage} - {@description}", logMessage, response.delegateResult.description);
                return Failure<DelegateResponse>(response.delegateResult.description);
            }

            _auditLogger.Log.Information("Success - {@logMessage} - {@description}", logMessage, response.delegateResult.description);
            return Success(new DelegateResponse(), message: ($"The request has been delegated successfully {response?.delegateResult?.description}"));

        }

        [HttpGet]
        public async Task<ApiResponseModel<CancelRequestResponse>> CancelRequest(decimal reqId)
        {
            var response = new ApiResponseModel<CancelRequestResponse>();

            var cardRequest = await _fdrDBContext.Requests.AsNoTracking().FirstOrDefaultAsync(r => r.RequestId == reqId);
            if (cardRequest == null)
                return response.Fail("invalid request id");

            if (cardRequest.ReqStatus is not ((int)CreditCardStatus.Pending or (int)CreditCardStatus.PendingForCreditCheckingReview))
                return response.Fail($"We can delete only pending status request, request current status is {(CreditCardStatus)cardRequest.ReqStatus}");

            var requestParameter = GetRequestParameter(reqId);

            using var transaction = await _fdrDBContext.Database.BeginTransactionAsync();
            try
            {
                await RemoveStandingOrder(cardRequest.RequestId, cardRequest.AcctNo, requestParameter);
                await RemovePromotions(cardRequest.RequestId.ToString(), cardRequest.CardNo, cardRequest.CivilId);
                await RollbackMarginAndDepositTransactions(cardRequest, requestParameter);
                await CancelWorkflow(cardRequest.RequestId);
                //await RemoveRequestParameters(cardRequest.RequestId);
                await RemoveRequestActivityAndDetails(cardRequest.RequestId);
                await RemoveSupplementaryDetails(cardRequest, requestParameter);
                _fdrDBContext.Requests.Remove(cardRequest);
                await _fdrDBContext.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (System.Exception ex)
            {
                await transaction.RollbackAsync();
                throw;
            }

            return response.Success(new() { ReqId = reqId }, message: "Successfully canceled!");
        }

        [HttpGet]
        public async Task<ApiResponseModel<RequestDto>> GetRequestDetail(decimal reqId)
        {
            var response = new ApiResponseModel<RequestDto>();

            var creditCardRequest = (await _fdrDBContext.Requests.AsNoTracking().Where(x => x.RequestId == reqId).FirstOrDefaultAsync())?.Adapt<RequestDto>();

            if (creditCardRequest == null) return response.Fail("invalid request id");

            //creditCardRequest.CardNo = creditCardRequest.CardNo.Masked(6, 6);

            creditCardRequest.Parameters = GetRequestParameter(reqId);

            return response.Success(creditCardRequest);
        }

        [HttpGet]
        public async Task<ApiResponseModel<List<RequestDto>>> GetPendingRequests(string civilId, int? productId)
        {
            IEnumerable<int> pendingStatuses = (ConfigurationBase.PendingStatuses).Cast<int>();

            var response = new ApiResponseModel<List<RequestDto>>();
            var pendingRequests = await _fdrDBContext.Requests.AsNoTracking().Where(x => x.CivilId == civilId && pendingStatuses.Any(status => status == x.ReqStatus) &&
            (productId == null || x.CardType == productId)).ProjectToType<RequestDto>().ToListAsync();
            return response.Success(pendingRequests);
        }

        [HttpGet]
        public async Task<ApiResponseModel<List<RequestStatusDto>>> GetAllRequestStatus()
        {
            var response = new ApiResponseModel<List<RequestStatusDto>>();
            var requestStatuses = await _fdrDBContext.RequestStatuses.AsNoTracking().ProjectToType<RequestStatusDto>().ToListAsync();
            return response.Success(requestStatuses);
        }


        [HttpPost]
        public async Task<ApiResponseModel<List<RequestDto>>> GetAllRequests([FromBody] RequestFilter filter, int page = 0, int size = 20)
        {
            var requests = _fdrDBContext.Requests.AsNoTracking().Include(x => x.Parameters).AsNoTracking();

            if (filter.Category is not null && filter.Category is not ProductCategory.All)
            {

                if (filter.Category == ProductCategory.Primary)
                    requests = requests.Where(x => x.Parameters.Any(x => x.Parameter == "IsSupplementaryOrPrimaryChargeCard" && x.Value == "P"));
                else
                    requests = requests.Where(x => x.Parameters.Any(x => x.Parameter == "IsSupplementaryOrPrimaryChargeCard" && x.Value == "S"));
            }

            if (filter.RequestId is not null)
                requests = requests.Where(x => x.ReqStatus == filter.RequestId);


            if (filter.RequestStatus is not null && filter.RequestStatus is not CreditCardStatus.All)
                requests = requests.Where(x => x.ReqStatus == (int)filter.RequestStatus);

            if (filter.CardNumber.Trim() != string.Empty)
                requests = requests.Where(x => x.CardNo == filter.CardNumber.Trim());

            if (filter.CustomerCivilId is not null)
                requests = requests.Where(x => x.CivilId == filter.CustomerCivilId);

            if (filter.ProductId is not null)
                requests = requests.Where(x => x.CardType == filter.ProductId);

            if (filter.RequestedDateFrom is not null)
                requests = requests.Where(x => x.ReqDate.Date == filter.RequestedDateFrom);

            if (filter.SellerId is not null)
                requests = requests.Where(x => x.SellerId == filter.SellerId);

            if (filter.Collateral is not null)
            {
                string io = filter.Collateral!.ToString() ?? "";
                requests = requests.Where(x => x.Parameters.Any(x => x.Parameter == "ISSUING_OPTION" && x.Value == io));
            }

            if (filter.Application is not null && filter.Application is not Applications.All)
            {
                string application = filter.Application!.ToString() ?? "";

                if (filter.Application == Applications.SSO)
                    requests = requests.Where(x => !x.Parameters.Any(x => x.Parameter == "Application"));
                else
                    requests = requests.Where(x => x.Parameters.Any(x => x.Parameter == "Application" && x.Value == application));
            }


            if (filter.ApprovedDateFrom is not null)
                requests = requests.Where(x => x.ApproveDate >= filter.ApprovedDateFrom);

            if (filter.ApprovedDateTo is not null)
                requests = requests.Where(x => x.ApproveDate <= filter.ApprovedDateTo);



            requests = requests.OrderByDescending(x => x.ReqDate);
            return Success(await requests.ProjectToType<RequestDto>().ToListAsync());
        }


        [HttpGet]
        public async Task<List<RequestParameter>> GetParameters(decimal reqId)
        {
            return await _fdrDBContext.RequestParameters.AsNoTracking().Where(x => x.ReqId == reqId).ToListAsync();
        }

        #region Non Action Methods

        [NonAction]
        public async Task AddRequestParameters(RequestParameterDto parameters, decimal reqId, bool deleteBeforeInsert = false)
        {
            var parametersToAdd = DictionaryExtension.ConvertObjectToDictionary(parameters)
                .Select(x => new RequestParameter()
                {
                    ReqId = reqId,
                    Parameter = x.Key,
                    Value = x.Value
                }).Where(x => x.Value != null);

            if (deleteBeforeInsert)
            {
                var existingParameters = await _fdrDBContext.RequestParameters.Where(x => x.ReqId == reqId).ToListAsync();
                var matchedParameters = (from erp in existingParameters
                                         join rrp in parametersToAdd on erp.Parameter equals rrp.Parameter
                                         select erp).ToList();

                if (matchedParameters.AnyWithNull())
                {
                    _fdrDBContext.RequestParameters.RemoveRange(matchedParameters);
                    await _fdrDBContext.SaveChangesAsync();
                }
            }

            await _fdrDBContext.RequestParameters.AddRangeAsync(parametersToAdd);
            await _fdrDBContext.SaveChangesAsync();
        }

        [NonAction]
        public async Task RemoveRequestParameters(RequestParameterDto parameters, decimal reqId)
        {
            var parametersToRemove = DictionaryExtension.ConvertObjectToDictionary(parameters)
                .Select(x => new RequestParameter()
                {
                    ReqId = reqId,
                    Parameter = x.Key,
                    Value = x.Value
                }).Where(x => x.Value != null).ToList();

            var existingParameters = await _fdrDBContext.RequestParameters.AsNoTracking().Where(x => x.ReqId == reqId).ToListAsync();
            var matchedParameters = from erp in existingParameters
                                    join rrp in parametersToRemove on erp.Parameter equals rrp.Parameter
                                    select erp;

            if (!matchedParameters.AnyWithNull())
                return;

            _fdrDBContext.RequestParameters.RemoveRange(matchedParameters);

            await _fdrDBContext.SaveChangesAsync();
        }



        [NonAction]
        public async Task<RequestResponse> CreateNewRequest(RequestDto request)
        {
            await GetUserDetails(null);
            request.BranchId = (int)CurrentUser?.DefaultBranchId;

            var newRequest = request.Adapt<Request>();

            //TODO: added for salary collateral card approval
            newRequest.Limit = request.RequestedLimit;

            newRequest.Expiry = "0000";

            await _fdrDBContext.Requests.AddAsync(newRequest);
            await _fdrDBContext.SaveChangesAsync();
            return new RequestResponse() { ReqId = newRequest.RequestId };
        }

        [NonAction]
        public async Task<bool> HasPendingOrActiveCard(string civilId, int productId)
        {
            var statuses = _fdrDBContext.Requests.Where(x => x.CivilId == civilId && x.CardType == productId && x.ReqStatus != (int)CreditCardStatus.Closed)
              .Select(x => x.ReqStatus);

            var hasDuplicateOrActive = statuses.Any(status => status == (int)CreditCardStatus.Pending || status == (int)CreditCardStatus.Active || status == (int)CreditCardStatus.Approved);

            return await Task.FromResult(hasDuplicateOrActive);
        }


        [HttpGet]
        public async Task<bool> HasPendingOrActiveCard(string civilId)
        {
            var isHasPending = _fdrDBContext.Requests.AsNoTracking().Any(x => x.CivilId == civilId && x.ReqStatus == (int)CreditCardStatus.Pending);

            return await Task.FromResult(isHasPending);
        }


        [NonAction]
        public async Task UpdateRequestParameter(decimal reqId, string parameter, string value)
        {
            var existingParameter = await _fdrDBContext.RequestParameters.FirstOrDefaultAsync(x => x.ReqId == reqId && x.Parameter == parameter);

            if (existingParameter is not null)
            {
                _fdrDBContext.RequestParameters.Remove(existingParameter);
                await _fdrDBContext.SaveChangesAsync();
            }

            _fdrDBContext.RequestParameters.Add(new() { ReqId = reqId, Parameter = parameter, Value = value });
            await _fdrDBContext.SaveChangesAsync();
        }

        [NonAction]
        public async Task UpdateCollateralDetails(decimal reqId, string accountNumber, string depositNumber, int amount)
        {
            var cardRequest = await _fdrDBContext.Requests.FirstOrDefaultAsync(x => x.RequestId == reqId);
            if (cardRequest is null) return;

            _ = Enum.TryParse(cardRequest.Parameters.FirstOrDefault(x => x.Parameter == "ISSUING_OPTION")?.Value, out Collateral _collateral);

            RequestParameterDto? requestParameter = null;

            if (_collateral is Collateral.AGAINST_DEPOSIT)
            {
                cardRequest.DepositNo = depositNumber;
                cardRequest.DepositAmount = amount;
                requestParameter = new()
                {
                    DepositAccountNumber = accountNumber,
                    DepositAmount = amount.ToString(),
                    DepositNumber = depositNumber
                };
            }

            if (_collateral is Collateral.AGAINST_MARGIN)
            {
                requestParameter = new()
                {
                    MarginAccountNumber = accountNumber,
                    MarginAmount = amount.ToString()
                };
            }

            if (requestParameter is null)
                return;

            await _fdrDBContext.SaveChangesAsync();

            await AddRequestParameters(requestParameter, reqId: reqId, deleteBeforeInsert: true);
        }
        #endregion

        #region Private Methods

        private async Task RollbackMarginAndDepositTransactions(Request cardRequest, RequestParameterDto requestParameter)
        {
            if (requestParameter.IsSupplementaryOrPrimaryChargeCard?.Equals("S", StringComparison.InvariantCultureIgnoreCase) == true)
            {
                return;
            }


            if (Enum.TryParse(typeof(Collateral), requestParameter.Collateral, out object? _collateral) == false)
                return;


            if ((Collateral)_collateral == Collateral.AGAINST_MARGIN && !string.IsNullOrEmpty(requestParameter.MarginTransferReferenceNumber))
            {
                _ = await _cardPaymentAppService.ReverseMonetary(new()
                {
                    DebitAccountNumber = cardRequest?.AcctNo ?? "",
                    ReferenceNumber = requestParameter.MarginTransferReferenceNumber ?? "",
                    MarginAccount = new MarginAccount(cardRequest?.RequestedLimit ?? 0, "", 0)
                });
            }

            if ((Collateral)_collateral is (Collateral.AGAINST_DEPOSIT or Collateral.AGAINST_DEPOSIT_USD) && requestParameter.DepositNumber is not null)
            {
                IEnumerable<RequestActivityDetail> activityDetails = await GetRequestActivityDetail(cardRequest.RequestId);
                if (!activityDetails.Any())
                    return;

                _ = Int64.TryParse(activityDetails.FirstOrDefault(x => x.Paramter == "DEPOSIT_NUMBER")?.Value, out long blockNumber);
                _ = double.TryParse(activityDetails.FirstOrDefault(x => x.Paramter == "DEPOSIT_AMOUNT")?.Value, out double blockAmount);
                _ = await _holdManagementServiceClient.removeHoldAsync(new()
                {
                    accountNumber = activityDetails.FirstOrDefault(x => x.Paramter == "DEPOSIT_ACCOUNT_NO")?.Value,
                    blockNumber = blockNumber,
                    blockAmount = blockAmount
                });
            }
        }

        private async Task RemoveStandingOrder(decimal reqId, string? accountNumber, RequestParameterDto requestParameter)
        {
            var standingOrderId = requestParameter.StandingOrderIDForMargin;

            if (string.IsNullOrEmpty(standingOrderId) || string.IsNullOrEmpty(accountNumber))
                return;

            if (int.TryParse(standingOrderId, out int _standingOrderId))
                await _standingOrderService.deleteSOAsync(accountNumber, _standingOrderId, ConfigurationBase.CustomStandingOrderServiceName);
        }
        private async Task RemovePromotions(string reqId, string? cardNumber, string civilId)
        {
            if (civilId is null)
                return;

            cardNumber ??= string.Concat(Enumerable.Repeat("0", 16));

            await _fdrDBContext.PromotionBeneficiaries.Where(x => (x.CardNo == cardNumber || x.CardNo == reqId) && x.CivilId == civilId).ExecuteDeleteAsync();
        }
        private async Task RemoveRequestParameters(decimal? reqId)
        {
            if (reqId is null)
                return;

            //var requestParamters = _fdrDBContext.RequestParameters.Where(x => x.ReqId == reqId);

            //if (requestParamters.Any())
            //    _fdrDBContext.RequestParameters.RemoveRange(requestParamters);

            await _fdrDBContext.RequestParameters.Where(x => x.ReqId == reqId).ExecuteDeleteAsync();
        }

        private async Task CancelWorkflow(decimal? reqId)
        {
            if (reqId is null)
                return;

            var workFlowInstanceId = (await _fdrDBContext.RequestParameters.FirstOrDefaultAsync(x => x.Parameter == "WorkFlowInstanceId" && x.ReqId == reqId))?.Value;
            //TODO:
            if (!string.IsNullOrEmpty(workFlowInstanceId))
            {
                var response = await _workflowAppService.CancelWorkFlow(instanceId: Guid.Parse(workFlowInstanceId), null);
            }

        }


        private async Task RemoveRequestActivityAndDetails(decimal? reqId)
        {
            if (reqId is null)
                return;

            var requestActivity = await _fdrDBContext.RequestActivities.FirstOrDefaultAsync(x => x.RequestId == reqId);
            if (requestActivity is null)
                return;


            await _fdrDBContext.RequestActivityDetails.Where(x => x.RequestActivityId == requestActivity.RequestActivityId).ExecuteDeleteAsync();
            _fdrDBContext.RequestActivities.Remove(requestActivity);
        }
        private async Task RemoveSupplementaryDetails(Request cardRequest, RequestParameterDto requestParameter)
        {
            if (cardRequest is null)
                return;

            if (requestParameter.IsSupplementaryOrPrimaryChargeCard?.Equals("S", StringComparison.InvariantCultureIgnoreCase) == true)
            {
                var preregisteredPayee = await _fdrDBContext.PreregisteredPayees.FirstOrDefaultAsync(x => x.CardNo == cardRequest.RequestId.ToString());
                _fdrDBContext.PreregisteredPayees.Remove(preregisteredPayee);
            }

            var requestIds = await _fdrDBContext.RequestParameters.Where(x => x.Parameter == "PRIMARY_CARD_REQUEST_ID" && x.Value == cardRequest.RequestId.ToString())
                .Select(x => x.ReqId.ToString()).ToListAsync();

            if (requestIds.Count == 0)
                return;

            var preregisteredPayees = _fdrDBContext.PreregisteredPayees.Where(x => x.CivilId == cardRequest.CivilId && requestIds.Any(rid => rid == x.CardNo));

            if (!preregisteredPayees.AnyWithNull())
                return;

            _fdrDBContext.Requests.RemoveRange(_fdrDBContext.Requests.Where(x => requestIds.Any(rid => rid == x.RequestId.ToString())));
            _fdrDBContext.PreregisteredPayees.RemoveRange(preregisteredPayees);

        }



        private RequestParameterDto GetRequestParameter(decimal reqId)
        {
            var requestParameters = _fdrDBContext.RequestParameters.AsNoTracking().Where(x => x.ReqId == reqId)
            .Select(x => new KeyValueTable
            {
                ColumnName = x.Parameter,
                ColumnValue = x.Value!
            });

            return DictionaryExtension.ConvertKeyValueDataToObject<RequestParameterDto>(requestParameters);
        }

        private async Task<IEnumerable<RequestActivityDetail>> GetRequestActivityDetail(decimal reqId)
        {
            return (await _fdrDBContext.RequestActivities.AsNoTracking().Include(x => x.Details).FirstOrDefaultAsync(x => x.RequestId == reqId))?.Details ?? Enumerable.Empty<RequestActivityDetail>();
        }

        [NonAction]
        public async Task<decimal> GenerateNewRequestId(string civilId)
        {
            decimal reqId = (await _fdrDBContext.Database.SqlQueryRaw<decimal>("SELECT FDR.SEQ.NEXTVAL FROM DUAL").ToListAsync()).FirstOrDefault();
            return decimal.Parse($"{civilId[5..]}{reqId}");
        }



        #endregion
    }


}
