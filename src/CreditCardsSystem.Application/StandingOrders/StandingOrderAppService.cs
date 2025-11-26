using AddCreditCardStandingOrderServiceReference;
using CloseCreditCardStandingOrderServiceReference;
using CreditCardsSystem.Data;
using CreditCardsSystem.Data.Models;
using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Models.Access;
using CreditCardsSystem.Domain.Models.CreditCards;
using CreditCardsSystem.Domain.Models.StandingOrder;
using CreditCardsSystem.Utility.Crypto;
using CreditCardsSystem.Utility.Extensions;
using EditCreditCardStandingOrderServiceReference;
using Kfh.Aurora.Auth;
using Kfh.Aurora.Common.Shared.Interfaces.Customer;
using Kfh.Aurora.Integration;
using Kfh.Aurora.Logging;
using Microsoft.EntityFrameworkCore;
using MonetaryTransferServiceReference;
using ServiceFeesManagementReference;
using SOByCivilIDAndChannelIDServiceReference;

namespace CreditCardsSystem.Application.StandingOrders;

public class StandingOrderAppService(IIntegrationUtility integrationUtility,
                                     IOptions<IntegrationOptions> options,
                                     ICreditCardsAppService creditCardsAppService,
                                     FdrDBContext fdrDBContext,
                                     IFeesAppService feesAppService,
                                     IAuthManager authManager,
                                     IAuditLogger<StandingOrderAppService> auditLogger,
                                        ICustomerProfileCommonApi customerProfileCommonApi) : BaseApiResponse, IStandingOrderAppService, IAppService
{
    private readonly ViewSOByCivilIDAndChannelIDClient _viewStandingOrderByCivilIdServiceClient = integrationUtility.GetClient<ViewSOByCivilIDAndChannelIDClient>(options.Value.Client, options.Value.Endpoints.CreditCardStandingOrder, options.Value.BypassSslValidation);
    private readonly EditCreditCardStandingOrderClient _editStandingOrderServiceClient = integrationUtility.GetClient<EditCreditCardStandingOrderClient>(options.Value.Client, options.Value.Endpoints.EditCreditCardStandingOrder, options.Value.BypassSslValidation);
    private readonly CloseCreditCardStandingOrderClient _closeStandingOrderServiceClient = integrationUtility.GetClient<CloseCreditCardStandingOrderClient>(options.Value.Client, options.Value.Endpoints.CloseCreditCardStandingOrder, options.Value.BypassSslValidation);
    private readonly ServiceFeesManagementClient _serviceFeesManagementClient = integrationUtility.GetClient<ServiceFeesManagementClient>(options.Value.Client, options.Value.Endpoints.ServiceFeesManagement, options.Value.BypassSslValidation);
    private readonly AddCreditCardStandingOrderClient _addStandingOrderServiceClient = integrationUtility.GetClient<AddCreditCardStandingOrderClient>(options.Value.Client, options.Value.Endpoints.AddCreditCardStandingOrder, options.Value.BypassSslValidation);
    private readonly MonetaryTransferServiceClient _monetaryTransferServiceClient = integrationUtility.GetClient<MonetaryTransferServiceClient>(options.Value.Client, options.Value.Endpoints.MonetaryTransfer, options.Value.BypassSslValidation);
    private readonly ICreditCardsAppService _creditCardsAppService = creditCardsAppService;
    private readonly IFeesAppService _feesAppService = feesAppService;
    private readonly IAuthManager authManager = authManager;
    private readonly IAuditLogger<StandingOrderAppService> _auditLogger = auditLogger;
    private readonly ICustomerProfileCommonApi customerProfileCommonApi = customerProfileCommonApi;
    private readonly FdrDBContext _fdrDBContext = fdrDBContext;

    [HttpPost]
    public async Task<ApiResponseModel<StandingOrderResponse>> AddStandingOrders([FromBody] StandingOrderRequest request)
    {
        request.BeneficiaryCardNumber = request.BeneficiaryCardNumber.DeSaltThis();
        await ValidateBiometricStatus(request.BeneficiaryCardNumber);

        if (!authManager.HasPermission(Permissions.StandingOrder.Create()))
            return Failure<StandingOrderResponse>(GlobalResources.NotAuthorized);

        var response = new ApiResponseModel<StandingOrderResponse>();
        await ValidateStandingOrderRequest(request, response.ValidationErrors);

        string transReferenceNumber = await GetTransRefNumber(request.TotalAmount, request.DebitAccountNumber, ConfigurationBase.VAT_Add_StandingOrder_ServiceName);
        (string startDate, string endDate) = GetStandingOrderDates(request);

        var result = (await _addStandingOrderServiceClient.addCreditCardStandingOrderAsync(new(Convert.ToDouble(request.Amount), request.BeneficiaryCardNumber, request.ChargeAccountNumber ?? string.Empty,
               request.NumberOfTransfer ?? 0, request.DebitAccountNumber, startDate, endDate, request.BranchNumber, "")))?.addCreditCardStandingOrderResult;


        if (result == null || !result.isSuccessful)
            await HandleFailureResult(request.TotalAmount, result?.status, result?.description, transReferenceNumber);

        await InsertRequestParameters(request);

        return response.Success(new() { Status = result?.status ?? "" }, message: $"Successfully added new standing order for beneficiaryCardNumber {request.BeneficiaryCardNumber}");
    }

    [HttpPost]
    public async Task<ApiResponseModel<StandingOrderResponse>> UpdateStandingOrders([FromBody] StandingOrderRequest request)
    {
        request.BeneficiaryCardNumber = request.BeneficiaryCardNumber.DeSaltThis();
        if (!authManager.HasPermission(Permissions.StandingOrder.Edit()))
            return Failure<StandingOrderResponse>(GlobalResources.NotAuthorized);

        var response = new ApiResponseModel<StandingOrderResponse>();
        await ValidateStandingOrderRequest(request, response.ValidationErrors);

        string transReferenceNumber = await GetTransRefNumber(request.TotalAmount, request.DebitAccountNumber, ConfigurationBase.VAT_Edit_StandingOrder_ServiceName);
        (string startDate, string endDate) = GetStandingOrderDates(request);

        var result = (await _editStandingOrderServiceClient.editCreditCardStandingOrderAsync(new()
        {
            Body = new()
            {
                amount = Convert.ToDouble(request.Amount),
                cardNo = request.BeneficiaryCardNumber,
                chargeAccount = request.ChargeAccountNumber,
                numberOfTransfer = request.NumberOfTransfer ?? 0,
                debitAccountNumber = request.DebitAccountNumber,
                startDate = startDate,
                endDate = endDate,
                brNo = request.BranchNumber,
                soID = request.StandingOrderId ?? 0
            }
        }))?.Body?.editCreditCardStandingOrderResult;

        if (result == null || !result.isSuccessful)
            await HandleFailureResult(request.TotalAmount, result?.status, result?.description, transReferenceNumber);

        await InsertRequestParameters(request);

        return response.Success(new() { Status = result?.status ?? "" }, message: $"Successfully updated standing order for beneficiaryCardNumber {request.BeneficiaryCardNumber}");

    }

    [HttpPost]
    public async Task<ApiResponseModel<StandingOrderResponse>> CloseStandingOrders([FromBody] StandingOrderRequest request)
    {
        var response = new ApiResponseModel<StandingOrderResponse>();
        string transReferenceNumber = await GetTransRefNumber(request.TotalAmount, request.DebitAccountNumber, ConfigurationBase.VAT_Delete_StandingOrder_ServiceName);

        var result = (await _closeStandingOrderServiceClient.removeCreditCardStandingOrderAsync(new()
        {
            Body = new()
            {
                chargeAccount = request.ChargeAccountNumber,
                acctNo = request.DebitAccountNumber,
                brNo = request.BranchNumber,
                soID = request.StandingOrderId ?? 0
            }
        }))?.Body?.removeCreditCardStandingOrderResult;

        if (result == null || !result.isSuccessful)
            await HandleFailureResult(request.TotalAmount, result?.status, result?.description, transReferenceNumber);

        await DeleteRequestParameters(request.BeneficiaryCardNumber);

        return response.Success(new(), message: $"Successfuly closed standing order {result?.status ?? ""}");
    }

    [HttpGet]
    public async Task<ApiResponseModel<List<OwnedCreditCardsResponse>>> GetOwnedCreditCards(string civilId)
    {
        var response = new ApiResponseModel<List<OwnedCreditCardsResponse>>() { IsSuccess = true };

        var result = await _creditCardsAppService.GetCreditCardsByCivilId(civilId);
        if (result.IsSuccess)
            response.Data = await FilterStandingOrder(result?.Data!);
        else
            return response.Fail("Cards Not found");

        return response;
    }

    [HttpGet]
    public async Task<ApiResponseModel<List<StandingOrderDto>>> GetAllStandingOrders(string civilId, int? standingOrderId = null)
    {
        var response = new ApiResponseModel<List<StandingOrderDto>>();
        if (string.IsNullOrEmpty(civilId)) response.Fail(message: "Invalid civil Id");

        var requestBody = new viewSOByCivilIDAndChannelIDRequestBody() { civilID = civilId };
        var result = (await _viewStandingOrderByCivilIdServiceClient.viewSOByCivilIDAndChannelIDAsync(new() { Body = requestBody }))?.Body?.viewSOByCivilIDAndChannelIDResult;
        var standingOrderDtoList = result?.AsQueryable().ProjectToType<CreditCardStandingOrderDTO>().ToList();
        var standingOrders = standingOrderDtoList?.Select(item => new StandingOrderDto
        {
            Period = item.Period,
            TransferType = item.Type,
            StandingOrderId = item.Id,
            NextTransferDate = item.NextTransferDate,
            NumberOfTransfers = item.NumberOfTransfers,
            Description = item.Description,
            CardNumberDto = item.Description.SaltThis(),
            AllowDelete = item.AllowDelete,
            AllowUpdate = item.AllowUpdate,
            SourceAccount = item.Account,
            DestinationAccount = item.TransferedAccount,
            PayeeName = item.TransferedAccountName,
            StartDate = item.StartDate == DateTime.MinValue ? null : item.StartDate,
            EndDate = item.ExpiryDate == DateTime.MinValue ? null : item.ExpiryDate,
            Amount = item.Amount,
            Currency = item.ToCurrencyIsoCode,
            Type = item.TypeEnglish,
            Duration = item.ExpiryDate == DateTime.MinValue || item.StartDate == DateTime.MinValue ? null : item.ExpiryDate.Subtract(item.StartDate),
        });

        if (standingOrderId is not null)
            standingOrders = standingOrders?.Where(x => x.StandingOrderId == standingOrderId);

        return response.Success(standingOrders?.ToList());
    }


    #region Private Methods
    private async Task ValidateBiometricStatus(string beneficiaryCardNumber)
    {
        var request = _fdrDBContext.Requests.AsNoTracking().FirstOrDefault(x => x.CardNo == beneficiaryCardNumber);
        var bioStatus = await customerProfileCommonApi.GetBiometricStatus(request!.CivilId);
        if (bioStatus.ShouldStop)
            throw new ApiException(message: GlobalResources.BioMetricRestriction);
    }
    private (string startDate, string endDate) GetStandingOrderDates(StandingOrderRequest request)
    {
        string UnlimitedEndDate = "2050/12/31";
        string startDate = request.StartDate.ToStandingOrderFormat();

        string endDate = DateTime.MinValue.ToStandingOrderFormat();

        if (request.OrderDuration == DurationTypes.Date)
        {
            endDate = (request.EndDate ?? DateTime.MinValue).ToStandingOrderFormat();
            request.NumberOfTransfer = null;
        }

        if (request.OrderDuration == DurationTypes.Unlimited)
        {
            endDate = DateTime.ParseExact(UnlimitedEndDate, "yyyy/MM/dd", null).ToStandingOrderFormat();
            request.NumberOfTransfer = null;
        }


        return (startDate, endDate);
    }
    private async Task HandleFailureResult(decimal totalAmount, string? status, string? description, string transReferenceNumber)
    {

        var message = $"{status} - {description}";

        if (totalAmount > 0 && !(await ReverseServiceFee(transReferenceNumber)))
            throw new ApiException(new(), message: $"{message} and couldn't reverse service fee");

        throw new ApiException(new(), message: $"{message} process failed");

    }
    private async Task InsertRequestParameters(StandingOrderRequest request)
    {
        var requestId = _fdrDBContext.Requests.AsNoTracking().FirstOrDefault(x => x.CardNo == request.BeneficiaryCardNumber)?.RequestId;

        if (requestId == null) return;

        var requestParameters = new List<RequestParameter>
                {
                    new() { ReqId = requestId??0, Parameter = ConfigurationBase.KEY_SO_START_DATE, Value = request.StartDate.Formed() },
                    new() { ReqId = requestId??0, Parameter = ConfigurationBase.KEY_SO_CREATE_DATE, Value = DateTime.Now.Formed() },
                };

        if (request.NumberOfTransfer is not null and > 0)
        {
            requestParameters.Add(new() { ReqId = requestId ?? 0, Parameter = ConfigurationBase.KEY_SO_COUNT, Value = ((int)request.NumberOfTransfer).ToString() });
            var endDate = request.StartDate.AddMonths((int)request.NumberOfTransfer - 1).Formed();
            requestParameters.Add(new() { ReqId = requestId ?? 0, Parameter = ConfigurationBase.KEY_SO_EXPIRY_DATE, Value = endDate });
        }
        else
        {
            requestParameters.Add(new() { ReqId = requestId ?? 0, Parameter = ConfigurationBase.KEY_SO_EXPIRY_DATE, Value = (request.EndDate ?? DateTime.MinValue).Formed() });
        }

        await DeleteRequestParameters(request.BeneficiaryCardNumber);

        await _fdrDBContext.RequestParameters.AddRangeAsync(requestParameters);
        await _fdrDBContext.SaveChangesAsync();

    }
    private async Task<bool> ReverseServiceFee(string originalRefNo)
    {
        _auditLogger.Log.Information("Reversing service fee. RefNo {@originalRefNo}", originalRefNo);
        var result = (await _monetaryTransferServiceClient.reverseGenericTransactionAsync(new() { originalRefNo = originalRefNo }))?.reverseGenericTransactionResult;

        if (!result.isSuccessful)
            _auditLogger.Log.Error("Failed : Reversing service fee. RefNo {@originalRefNo} - {@message}", originalRefNo,result.description);

        return result?.isSuccessful ?? false;
    }

    private async Task DeleteRequestParameters(string cardNumber)
    {

        var requestId = _fdrDBContext.Requests.AsNoTracking().FirstOrDefault(x => x.CardNo == cardNumber)?.RequestId;

        string[] standingOrderParamters = new[] { ConfigurationBase.KEY_SO_START_DATE, ConfigurationBase.KEY_SO_COUNT, ConfigurationBase.KEY_SO_EXPIRY_DATE, ConfigurationBase.KEY_SO_CREATE_DATE };

        await _fdrDBContext.RequestParameters.Where(x => x.ReqId == requestId && standingOrderParamters.Any(sp => sp == x.Parameter)).ExecuteDeleteAsync();
    }

    private async Task<string> GetTransRefNumber(decimal totalAmount, string debitAccountNumber, string serviceName)
    {
        if (totalAmount <= 0) return string.Empty;

        string transRefno = string.Empty;

        var postFeeResponse = await _feesAppService.PostServiceFee(new() { ServiceName = serviceName, DebitAccountNumber = debitAccountNumber });
        if (postFeeResponse.IsSuccess)
        {
            transRefno = postFeeResponse?.Data?.TransRefNumber ?? "";
            if (string.IsNullOrEmpty(transRefno))
                throw new ApiException(new(), message: "Failed due to invalid transaction reference number");
        }

        return transRefno;
    }

    private async Task ValidateStandingOrderRequest(StandingOrderRequest request, List<ValidationError> validationErrors)
    {
        await request.ModelValidationAsync();

        if (request.DebitAccountNumber == null)
            validationErrors.Add(new(nameof(request.DebitAccountNumber), GlobalResources.RequiredDebitAccount));

        if (request.Amount <= 0)
            validationErrors.Add(new(nameof(request.Amount), GlobalResources.RequiredAmount));

        var configParameters = _fdrDBContext.ConfigParameters.AsNoTracking().ToList();
        if (decimal.TryParse(configParameters.FirstOrDefault(x => x.ParamName == "MaxSOAmountLimit")?.ParamValue, out decimal _maxLimit) && request.Amount > _maxLimit)
            validationErrors.Add(new(nameof(request.Amount), string.Format(GlobalResources.StandingOrderExceedAmount, _maxLimit)));

        if (decimal.TryParse(configParameters.FirstOrDefault(x => x.ParamName == "MinSOAmountLimit")?.ParamValue, out decimal _minLimit) && request.Amount < _minLimit)
            validationErrors.Add(new(nameof(request.Amount), string.Format(GlobalResources.StandingOrderLessAmount, _minLimit)));

        if (request.BeneficiaryCardNumber == null)
            validationErrors.Add(new(nameof(request.BeneficiaryCardNumber), GlobalResources.RequiredBeneficiaryCardNumber));

        if (request.StartDate.CompareTo(DateTime.Today) < 0)
            validationErrors.Add(new(nameof(request.StartDate), GlobalResources.StartDateCannotBeOld));

        if (request.IsApproved && request.StartDate.CompareTo(DateTime.Today.AddDays(7)) < 0)
            validationErrors.Add(new(nameof(request.StartDate), GlobalResources.StandingOrderStartDate));

        if (request.OrderDuration is DurationTypes.Date)
        {
            if (request.EndDate == null && (request.NumberOfTransfer == null || request.NumberOfTransfer == 0))
            {
                validationErrors.Add(new(nameof(request.OrderDuration), GlobalResources.RequiredStandingOrderDurationEnd));
            }

            if (request.EndDate != null && request.EndDate != DateTime.MinValue)
            {
                if (request.EndDate < request.StartDate)
                    validationErrors.Add(new(nameof(request.EndDate), GlobalResources.InvalidEndDate));

                if (request.EndDate.Value.Subtract(request.StartDate).TotalDays < 30)
                    validationErrors.Add(new(nameof(request.EndDate), GlobalResources.StandingOrderDatesMustBetween));
            }
        }

        if (request.OrderDuration == DurationTypes.Count)
        {

            if (request.NumberOfTransfer != null && request.NumberOfTransfer > 800)
                validationErrors.Add(new(nameof(request.NumberOfTransfer), GlobalResources.InvalidStandingOrderCount));

            if (request.NumberOfTransfer != null && request.NumberOfTransfer <= 0)
                validationErrors.Add(new(nameof(request.NumberOfTransfer), GlobalResources.InvalidMinimalStandingOrderCount));
        }

        if (!string.IsNullOrEmpty(request.ChargeAccountNumber))
        {
            if (request.TotalAmount > request.ChargeAmount)
            {
                validationErrors.Add(new(nameof(request.ChargeAmount), GlobalResources.InsufficientChargeAccountBalance));
            }
        }

        if (validationErrors.Count != 0) throw new ApiException(validationErrors, nameof(StandingOrderRequest), "validation failed");

        await Task.CompletedTask;
    }

    private async Task<List<OwnedCreditCardsResponse>> FilterStandingOrder(List<CreditCardResponse> creditCards)
    {
        var filteredSOCards = creditCards?.AsQueryable()
            .Where(x => x.CardNo != null && (x.CardStatus == (int)CreditCardStatus.Active || x.CardStatus == (int)CreditCardStatus.Approved));

        var allowedCardTypes = (await _fdrDBContext.ConfigParameters.AsNoTracking()
           .FirstOrDefaultAsync(x => x.ParamName == ConfigurationBase.SO_AllowedCardTypes))?.ParamValue.Split(",") ?? Array.Empty<string>();

        //TODO: Show credit card number based on permission
        filteredSOCards = filteredSOCards?.Where(x => allowedCardTypes.Any(at => at == x.CardType.ToString()));

        var OwnedSOCards = (from fc in filteredSOCards?.ToList()
                            join ct in _fdrDBContext.CardDefs.AsNoTracking() on fc.CardType equals ct.CardType
                            select new OwnedCreditCardsResponse()
                            {
                                CardNumber = fc.CardNo,
                                CardNumberDto = fc.CardNo.SaltThis(),
                                ProductName = ct.Name,
                                CardType = fc.CardType,
                                RequestId = fc.RequestID,
                                ApprovedLimit = fc.ApprovedLimit
                            }
                              ).ToList();

        return OwnedSOCards;
    }
    #endregion
}

