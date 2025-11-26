using CreditCardsSystem.Data;
using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Models.CardIssuance;
using CreditCardsSystem.Domain.Models.Report;
using CreditCardsSystem.Domain.Models.Reports;
using CreditCardsSystem.Domain.Models.Request;
using CreditCardsSystem.Domain.Shared.Interfaces;
using CreditCardTransactionInquiryServiceReference;
using Kfh.Aurora.Integration;
using Microsoft.Extensions.Configuration;
using System.Data;
using Telerik.DataSource.Extensions;

namespace CreditCardsSystem.Application.Report;

public class StatisticalReportAppService(IGroupsAppService groupsAppService, IIntegrationUtility integrationUtility, IOptions<IntegrationOptions> options, FdrDBContext fdrDbContext, IReportAppService reportAppService) : BaseApiResponse, IStatisticalReportAppService, IAppService
{
    private readonly IGroupsAppService groupsAppService = groupsAppService;
    private readonly FdrDBContext fdrDbContext = fdrDbContext;
    private readonly IReportAppService reportAppService = reportAppService;
    private readonly CreditCardInquiryServicesServiceClient _creditCardsInquiryServiceClient = integrationUtility.GetClient<CreditCardInquiryServicesServiceClient>(options.Value.Client, options.Value.Endpoints.CreditCardTransactionInquiry, options.Value.BypassSslValidation);


    #region Statistical Report
    [HttpPost]
    public async Task<ApiResponseModel<IEnumerable<StatisticalReportData>>> GetStatisticalReport([FromBody] RequestFilter filter)
    {
        var creditCardSearchCriteria = new CreditCardSearchCriteria();


        if (filter.CardStatus is not null)
            creditCardSearchCriteria.CardStatus = (int)filter.CardStatus;

        if (filter.ProductId is not null)
            creditCardSearchCriteria.CardType = [(int)filter.ProductId];

        if (filter.BranchId is not null)
            creditCardSearchCriteria.BranchID = [Convert.ToInt16(filter.BranchId)];



        if (filter.RequestedDateFrom is not null)
            creditCardSearchCriteria.RequestDateFrom = DateTime.ParseExact(filter.RequestedDateFrom.Value.ToString("yyyy/MM/dd"), "yyyy/MM/dd", null);

        if (filter.RequestedDateTo is not null)
        {
            creditCardSearchCriteria.RequestDateTo = DateTime.ParseExact(filter.RequestedDateTo.Value.ToString("yyyy/MM/dd"), "yyyy/MM/dd", null);
            creditCardSearchCriteria.RequestDateTo = creditCardSearchCriteria.RequestDateTo?.AddDays(1);
        }


        if (filter.ApprovedDateFrom is not null)
            creditCardSearchCriteria.ApproveDateFrom = DateTime.ParseExact(filter.ApprovedDateFrom.Value.ToString("yyyy/MM/dd"), "yyyy/MM/dd", null);

        if (filter.ApprovedDateTo is not null)
        {
            creditCardSearchCriteria.ApproveDateTo = DateTime.ParseExact(filter.ApprovedDateTo.Value.ToString("yyyy/MM/dd"), "yyyy/MM/dd", null);
            creditCardSearchCriteria.ApproveDateTo = creditCardSearchCriteria.ApproveDateTo?.AddDays(1);
        }



        if (filter.BranchId is not null)
            creditCardSearchCriteria.BranchID = [Convert.ToInt16(filter.BranchId)];

        var multipleCreditCardSearchCriteria = creditCardSearchCriteria.Adapt<multipleCreditCardSearchCriteria>();


        var requestParameters = new List<requestParameters>();

        if (!string.IsNullOrEmpty(filter.CustomerClass))
            requestParameters.Add(new() { parameter = "CUSTOMER_CLASS_CODE", value = creditCardSearchCriteria.RequestParameterDto.CustomerClassCode });

        if (filter.Gender is not null)
            requestParameters.Add(new() { parameter = "SELLER_GENDER_CODE", value = creditCardSearchCriteria.RequestParameterDto.SellerGenderCode });

        if (requestParameters.Count > 0)
            multipleCreditCardSearchCriteria.requestParameters = requestParameters.ToArray();

        multipleCreditCardSearchCriteria.approveDatefromSpecified = multipleCreditCardSearchCriteria.approveDatefrom != default;
        multipleCreditCardSearchCriteria.approveDateToSpecified = multipleCreditCardSearchCriteria.approveDateTo != default;
        multipleCreditCardSearchCriteria.requestDateFromSpecified = multipleCreditCardSearchCriteria.requestDateFrom != default;
        multipleCreditCardSearchCriteria.requestDateToSpecified = multipleCreditCardSearchCriteria.requestDateTo != default;
        multipleCreditCardSearchCriteria.approvedId = -1;
        multipleCreditCardSearchCriteria.customerAgeFrom = -1;
        multipleCreditCardSearchCriteria.customerAgeTo = -1;
        multipleCreditCardSearchCriteria.customerGender = -1;
        multipleCreditCardSearchCriteria.customerNationality = -1;
        multipleCreditCardSearchCriteria.sellerID = -1;
        multipleCreditCardSearchCriteria.tellerID = -1;
        multipleCreditCardSearchCriteria.reqId = -1;



        var result = (await _creditCardsInquiryServiceClient.searchMultipleCreditCardDataAsync(new searchMultipleCreditCardDataRequest()
        {
            multipleCreditCardSearchCriteria = multipleCreditCardSearchCriteria
        }))?.searchMultipleCreditCardDataResult;


        if (result is null)
            return Success(Enumerable.Empty<StatisticalReportData>());

        var branches = await groupsAppService.GetLocations();

        var creditCards = result!.AsQueryable().ProjectToType<MultipleCreditCardSearchResponse>().ToList();

        var allCards = from cc in creditCards
                       join branch in branches on cc.branchID.ToString() equals branch.Value
                       join card in fdrDbContext.CardDefs on cc.cardType equals card.CardType
                       let requestParameterForCardCategory = cc.requestParameters.FirstOrDefault(x => x.Parameter == "IsSupplementaryOrPrimaryChargeCard")
                       let collateral = cc.requestParameters.FirstOrDefault(x => x.Parameter == "ISSUING_OPTION")?.Value
                       let cardSubTypeAndName = Helpers.GetCreditCardProductName(card, requestParameterForCardCategory == null ? "" : requestParameterForCardCategory.Value)
                       select new StatisticalReportData()
                       {
                           ProductName = cardSubTypeAndName.productName,
                           CardCategory = cardSubTypeAndName.cardCategory,
                           RequestDate = cc.requestDate,
                           Branch = branch.Attribute,
                           RequestLimit = cc.requestLimit,
                           CardNo = cc.cardNo,
                           ApprovedLimit = cc.approvedLimit,
                           ApproveDate = cc.approveDate,
                           CustomerName = cc.customerName,
                           SellerID = cc.sellerID,
                           TellerID = cc.tellerID,
                           ApprovedBy = cc.approvedBy,
                           Status = (CreditCardStatus)cc.cardStatus,
                           Collateral = collateral,
                           CivilID = cc.civilID,
                           AcctNo = cc.acctNo
                       };

        if (filter.IsSupplementaryCard)
            allCards = allCards.Where(x => x.CardCategory == CardCategoryType.Supplementary);

        return Success(allCards);

    }

    [HttpPost]
    public async Task<ApiResponseModel<IEnumerable<StatisticalChangeLimitHistoryData>>> GetStatisticalChangeLimitHistory([FromBody] RequestFilter filter)
    {

        var res = from ch in fdrDbContext.ChangeLimitHistories
                  join r in fdrDbContext.Requests on ch.ReqId.Trim() equals r.RequestId.ToString()
                  select new StatisticalChangeLimitHistoryData
                  {
                      Id = ch.Id,
                      ReqId = ch.ReqId,
                      OldLimit = ch.OldLimit,
                      NewLimit = ch.NewLimit,
                      IsTempLimitChange = ch.IsTempLimitChange,
                      LogDate = ch.LogDate,
                      Status = ch.Status,
                      RefuseReason = ch.RefuseReason,
                      InitiatorId = ch.InitiatorId,
                      ApproverId = ch.ApproverId,
                      ApproveDate = ch.ApproveDate,
                      RejectDate = ch.RejectDate,
                      PurgeDays = ch.PurgeDays,
                      CivilId = r.CivilId,
                      CardNo = r.CardNo,
                      ChangeType = ch.IsTempLimitChange == "1" ? "TEMPORARY" : "PERMANENT"
                  };




        if (!string.IsNullOrEmpty(filter.CardNumber))
            res = res.Where(x => x.CardNo == filter.CardNumber);


        if (filter.RequestedDateFrom is not null)
            res = res.Where(x => x.LogDate >= filter.RequestedDateFrom);

        if (filter.RequestedDateTo is not null)
            res = res.Where(x => x.LogDate <= filter.RequestedDateTo);



        return Success(res.AsEnumerable());

    }

    [HttpPost]
    public async Task<ApiResponseModel<EFormResponse>> PrintStatisticalChangeLimitHistoryReport([FromBody] ChangeLimitReportDto reportData) => await reportAppService.PrintDynamicReport(reportData);

    #endregion

    internal class CreditCardSearchCriteria
    {
        public int ApprovedId { get; set; } = -1;
        public int CustomerAgeFrom { get; set; } = -1;
        public int CustomerAgeTo { get; set; } = -1;
        public int CustomerGender { get; set; } = -1;
        public int CustomerNationality { get; set; } = -1;
        public int SellerID { get; set; } = -1;
        public int TellerID { get; set; } = -1;
        public int ReqId { get; set; } = -1;
        public int CardStatus { get; set; } = -1;
        public int[] CardType { get; set; }
        public int[] BranchID { get; set; }
        public RequestParameterDto RequestParameterDto { get; internal set; }
        public DateTime? RequestDateFrom { get; set; }
        public DateTime? RequestDateTo { get; set; }
        public DateTime? ApproveDateFrom { get; set; }
        public DateTime? ApproveDateTo { get; set; }
    }

}

