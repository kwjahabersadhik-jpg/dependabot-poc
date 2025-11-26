using CreditCardsSystem.Data;
using CreditCardsSystem.Domain.Models.Reports;
using Kfh.Aurora.Integration;
using Microsoft.EntityFrameworkCore;
using StaticDataInquiriesServiceReference;
using System.Data;
using Telerik.DataSource.Extensions;

namespace CreditCardsSystem.Application.Report;

public class SingleReportAppService(IIntegrationUtility integrationUtility,
        IOptions<IntegrationOptions> options, FdrDBContext fdrDbContext, IReportAppService reportAppService) : BaseApiResponse, ISingleReportAppService, IAppService
{

    #region Variables
    private readonly FdrDBContext fdrDbContext = fdrDbContext;
    private readonly IReportAppService reportAppService = reportAppService;
    private readonly StaticDataInquiriesServiceClient _staticDataInquiriesServiceClient = integrationUtility.GetClient<StaticDataInquiriesServiceClient>(options.Value.Client, options.Value.Endpoints.StaticDataInquiries, options.Value.BypassSslValidation);

    #endregion


    [HttpPost]
    public async Task<ApiResponseModel<EFormResponse>> PrintReport([FromBody] SingleReportFilter filter)
    {
        await filter.ModelValidationAsync();


        var result = (await _staticDataInquiriesServiceClient.getAppConfigByKeyAsync(new getAppConfigByKeyRequest("AUB_CREDIT_CARD_BIN"))).getAppConfigByKey;

        //Is AUB Card nubmer
        if (result.Split(",").Any(x => x == filter.CardNumber.Substring(0, 6)))
        {
            var aubCard = await fdrDbContext.AubCardMappings.FirstOrDefaultAsync(x => x.AubCardNo == filter.CardNumber);
            if (aubCard != null)
            {
                filter.CardNumber = aubCard.KfhCardNo;
            }
        }



        bool isCorporateCard = await (from r in fdrDbContext.Requests.AsNoTracking().Where(x => x.CardNo == filter.CardNumber.Trim())
                                      join mtx in fdrDbContext.CardtypeEligibilityMatixes on r.CardType equals mtx.CardType
                                      select mtx.IsCorporate).FirstOrDefaultAsync() ?? false;
        int iMonth = filter.Period.Month, iYear = filter.Period.Year;
        DateTime dtSTart, dtEnd;

        if (isCorporateCard)
        {
            dtSTart = new DateTime(iYear, iMonth, 1);
            dtEnd = dtSTart.AddMonths(1).AddDays(-1);
        }
        else
        {
            dtSTart = new DateTime(iYear, iMonth, 16);
            dtEnd = new DateTime(iYear, iMonth, 1).AddMonths(1).AddDays(14);
        }



        var response = (from r in fdrDbContext.Requests.AsNoTracking().Where(sd => sd.CardNo == filter.CardNumber)
                        join m in fdrDbContext.StatementMasters.AsNoTracking().Where(sd => sd.StatementDate == $"{filter.Period.Month.ToString("00")}{filter.Period.ToString("yy")}") on r.FdAcctNo equals m.FdAcctNo
                        join d in fdrDbContext.StatementDetails.AsNoTracking() on new { FAAcno = m.FdAcctNo, StatementDate = m.StatementDate } equals new { FAAcno = d.AccountNo, StatementDate = d.StatementDate }
                        select new
                        {
                            CardLimit = m.CreditLimit,
                            MinimumLimit = m.MinimumPaymentDue,
                            TotalBeginBalance = m.TotalBeginBalance,
                            CardNumber = m.PrimaryCardNo,
                            Name = m.NameAddrLine1,
                            Addrss1 = m.NameAddrLine4,
                            Addrss2 = m.NameAddrLine5,
                            Addrss3 = m.NameAddrLine3,
                            City = m.City,
                            Branch = m.Branch,
                            TotalDue = m.TotalDueAmount,
                            AvailableLimit = m.OpenToBuy,
                            ShadowAccountNumber = m.ShadowAcctNo,
                            AccountNo = d.AccountNo,
                            TransPostDate = d.TransPostDate,
                            TransEffectiveDate = d.TransEffectiveDate,
                            CardType = d.CardType,
                            TransDescription = d.TransDescription,
                            ForeignCurrency = d.ForeignCurrency,
                            ForeignCurrencyAmount = d.ForeignCurrencyAmount,
                            TransAmount = d.TransAmount
                        }).AsQueryable();

        var master = response.FirstOrDefault().Adapt<SingleReportDto>();

        if (master is null)
            return Failure<EFormResponse>(message: "record not found!");

        master.ReportPeriod = dtSTart.ToString("dd/MM/yy") + " - " + dtEnd.ToString("dd/MM/yy");
        master.Details = response.ProjectToType<SingleReportDetailDto>();
        return await reportAppService.PrintDynamicReport(master); ;
    }






}

