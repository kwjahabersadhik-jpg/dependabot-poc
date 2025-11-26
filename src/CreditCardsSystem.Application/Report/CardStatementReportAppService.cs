using CreditCardsSystem.Data;
using CreditCardsSystem.Data.Models;
using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Models.Access;
using CreditCardsSystem.Domain.Models.CardStatement;
using CreditCardsSystem.Domain.Models.Reports;
using CreditCardsSystem.Utility.Extensions;
using CreditCardTransactionInquiryServiceReference;
using Kfh.Aurora.Auth;
using Kfh.Aurora.Common.Shared.Interfaces.Customer;
using Kfh.Aurora.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MiniExcelLibs;
using Newtonsoft.Json;
using System.Data;
using Telerik.DataSource.Extensions;
using Telerik.Reporting;
using Telerik.Reporting.Processing;
using Table = Telerik.Reporting.Table;


namespace CreditCardsSystem.Application.Report;

public class CardStatementReportAppService(ILogger<CardStatementReportAppService> logger,
    IDocumentAppService documentAppService, IAuthManager authManager, IIntegrationUtility integrationUtility,
    IOptions<IntegrationOptions> options, FdrDBContext fdrDbContext, IReportAppService reportAppService, Microsoft.Extensions.Configuration.IConfiguration configuration, ICustomerProfileCommonApi customerProfileCommonApi) : BaseApiResponse, ICardStatementReportAppService, IAppService
{
    private readonly IAuthManager _authManager = authManager;
    private readonly IDocumentAppService documentAppService = documentAppService;
    private readonly ILogger<CardStatementReportAppService> _logger = logger;
    private readonly ICustomerProfileCommonApi _customerProfileCommonApi = customerProfileCommonApi;
    #region Variables
    private readonly FdrDBContext fdrDbContext = fdrDbContext;
    private readonly CreditCardInquiryServicesServiceClient _creditCardsInquiryServiceClient = integrationUtility.GetClient<CreditCardInquiryServicesServiceClient>(options.Value.Client, options.Value.Endpoints.CreditCardTransactionInquiry, options.Value.BypassSslValidation);

    #endregion


    [HttpPost]
    public async Task<ApiResponseModel<TableDataSource>> GetCreditCardStatement([FromBody] CreditCardStatementReportRequest? request, CancellationToken cancellationToken = default)
    {
        if (request?.Parameter?.RequestId is null)
            throw new ApiException(message: "Request Id is missing in your request");

        await ValidateBiometricStatus(request.Parameter.RequestId!);

        cancellationToken.ThrowIfCancellationRequested();

        return await GetCardStatement(request, cancellationToken);


    }

    private async Task<ApiResponseModel<TableDataSource>> GetCardStatement(CreditCardStatementReportRequest request, CancellationToken cancellationToken)
    {
        var card = await fdrDbContext.Requests.AsNoTracking().FirstOrDefaultAsync(r => r.RequestId == request!.Parameter!.RequestId, cancellationToken);
        if (card is null)
            return Failure<TableDataSource>("Invalid card");

        var cardMatrixConfig = await fdrDbContext.CardtypeEligibilityMatixes.AsNoTracking().FirstOrDefaultAsync(c => c.CardType == card!.CardType, cancellationToken);


        if (card.IsAUB == 1)
        {
            var aubMapping = await fdrDbContext.AubCardMappings.FirstOrDefaultAsync(aub => aub.KfhCardNo == card.CardNo);
            if (aubMapping is not null)
                card.CardNo = aubMapping.AubCardNo;
        }

        if (card.CardNo != null)
        {
            return request!.ReportType switch
            {
                ReportType.CycleToDate => await GenerateReportOnCycleToDate(request, card?.CardNo!, cardMatrixConfig),
                ReportType.MonthYear => await GenerateReportOnMonthYear(request, card?.CardNo!, cardMatrixConfig),
                ReportType.FromToDate => await GenerateReportOnFromToDate(request, card?.CardNo!),
                _ => new()
            };
        }

        return Failure<TableDataSource>("Card Number is Empty");
    }

    [HttpPost]
    public async Task<ApiResponseModel<EFormResponse>> PrepareReport([FromBody] CreditCardStatementReportRequest? request, string format = "PDF", CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (request?.Parameter?.RequestId is null)
            throw new ApiException(message: "Request Id is missing in your request");

        await ValidateBiometricStatus(request.Parameter.RequestId!);

        var apiResponse = new ApiResponseModel<EFormResponse>();
        var tableDataSource = await GetCardStatement(request, cancellationToken);
        //var CanViewCardNumber = _authManager.HasPermission(Permissions.CreditCardsNumber.View());

        if (!tableDataSource.IsSuccessWithData)
            return apiResponse.Fail("No Data for selected period");


        if (tableDataSource.Data?.CreditCardTransaction?.Count == 0)
        {
            return apiResponse.Fail("No Data for selected period");
        }

        var card = await fdrDbContext.Requests.Where(r => r.RequestId == request!.Parameter!.RequestId).AsNoTracking()
        .FirstOrDefaultAsync(cancellationToken);
        //Get Card Name by card No
        var cardExtension = await fdrDbContext.CardDefs.AsNoTracking().Include(c => c.CardDefExts).FirstOrDefaultAsync(c => c.CardType == card!.CardType, cancellationToken);

        var carNumber = card!.CardNo?.Masked(6, 6).SplitByFour();

        var cardFullNo = carNumber + "-" + cardExtension?.Name;
        var profile = await fdrDbContext.Profiles.AsNoTracking().FirstOrDefaultAsync(p => p.CivilId == card.CivilId, cancellationToken);
        var customerName = $"{profile?.FirstName} {profile?.LastName}";
        var tableDataSourceTransaction = tableDataSource.Data;

        request.IsRebrand = true;

        var CurrencyORG = cardExtension.CardDefExts.FirstOrDefault(cde => cde.CardType == card.CardType && cde.Attribute.ToUpper() == "ORG");
        if (CurrencyORG != null)
        {
            var currencyISO = (await fdrDbContext.CardCurrencies.AsNoTracking().FirstOrDefaultAsync(cur => cur.Org == CurrencyORG!.Value, cancellationToken: cancellationToken))?.CurrencyIsoCode ?? "";
            request.CardCurrency = currencyISO;
        }

        //Assign missing values
        request.CardNo = cardFullNo;
        request.NoTransactions = tableDataSource.Data!.CreditCardTransaction!.Count;
        request.Name = customerName;
        request.IsCobrand = tableDataSource.Data.IsCobrand;
        request.Cashback =
           await GetCashBack(request.Parameter!.RequestId);

        // calculate totals
        request.TotalDeclined = tableDataSource.Data.CreditCardTransaction.Where(c => c.descriptionField.Contains("Decline")).Sum(x => x.amountField);
        request.TotalHold = tableDataSource.Data.CreditCardTransaction.Where(c => c.descriptionField.Contains("AUTH CODE")).Sum(x => x.amountField);
        request.TotalCredit = tableDataSource.Data.CreditCardTransaction.Where(d => d.isCreditField).Sum(x => x.amountField);
        request.TotalDebit = tableDataSource.Data.CreditCardTransaction.Where(d => d.isCreditField == false).Sum(x => x.amountField);
        string rootpath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot");

        if (format.Equals("XLSX", StringComparison.CurrentCultureIgnoreCase))
        {
            tableDataSourceTransaction.CardFullNumber = cardFullNo;
            tableDataSourceTransaction.CardCurrency = request.CardCurrency;
            tableDataSourceTransaction.CustomerName = customerName;
            tableDataSourceTransaction.IsCobrand = tableDataSource.Data.IsCobrand;
            tableDataSourceTransaction.Cashback = request.Cashback;
            tableDataSourceTransaction.FromDate = Convert.ToDateTime(request.FromDate).ToString(ConfigurationBase.DateFormat);
            tableDataSourceTransaction.ToDate = Convert.ToDateTime(request.ToDate).ToString(ConfigurationBase.DateFormat);

            //= IIF(Fields.cardTypeField = '0', 'Primary', (IIF(Fields.cardTypeField = '1', 'Supplementary', '')) )
            tableDataSourceTransaction.CreditCardTransaction?.ForEach(x =>
            {
                x.cardTypeField = x.cardTypeField == "0" ? "Primary" : (x.cardTypeField = x.cardTypeField == "1" ? "Supplementary" : "");
            });


            bool IsRebrand = configuration.GetValue<string>("Rebrand")?.Equals("true") ?? true;

            using (var memoryStream = new MemoryStream())
            {
                memoryStream.SaveAsByTemplate($"{rootpath}/Report/CreditCardStatementTemplate{(IsRebrand ? "V2" : "V1")}.xlsx", tableDataSourceTransaction);
                memoryStream.Seek(0, SeekOrigin.Begin);
                var response = await UploadDocument(request, memoryStream.ToArray(), FileExtension.xlsx);
                return Success(response);
            }
        }

        //prepare report

        var byteArr = await File.ReadAllBytesAsync($"{rootpath}/Report/CardStatementReport.trdp", cancellationToken);
        var reportPackager = new ReportPackager();
        using var sourceStream = new MemoryStream(byteArr);
        var report = (Telerik.Reporting.Report)reportPackager.UnpackageDocument(sourceStream);
        // assigning report sources
        if (report.DataSource is JsonDataSource json)
        {
            json.Source = JsonConvert.SerializeObject(request);
        }
        //assign table1 source
        if (report.Items["detailSection1"].Items["table1"] is Table table1)
        {
            if (table1.DataSource is JsonDataSource json1)
            {
                json1.Source = JsonConvert.SerializeObject(tableDataSourceTransaction.CreditCardTransaction);
            }
        }
        if (tableDataSourceTransaction.IsCobrand)
        {
            if (report.Items["detailSection1"].Items["table2"] is Table table2)
            {
                if (table2.DataSource is JsonDataSource json2)
                {
                    json2.Source = JsonConvert.SerializeObject(tableDataSourceTransaction);
                }
            }
        }


        // Parameters Goes Here
        //report.ReportParameters["SubRebort_IsActive"].Value = 11;
        InstanceReportSource reportSource = new InstanceReportSource
        {
            ReportDocument = report
        };
        ReportProcessor reportProcessor = new();
        RenderingResult result = reportProcessor.RenderReport(format, reportSource, null);
        //return apiresponse.Success(result.DocumentBytes);


        var pdfResponse = await UploadDocument(request, result.DocumentBytes);
        return Success(pdfResponse);


        async Task<EFormResponse> UploadDocument(CreditCardStatementReportRequest? cardStatementReportDataSourceDto, byte[] DocumentBytes, FileExtension extension = FileExtension.pdf)
        {
            Guid fileId = Guid.Empty;
            string fileName = $"CreditCardStatement_{cardStatementReportDataSourceDto.CardNo.Masked(6, 6)}".Trim();
            DocumentFileAttributes attributes = new()
            {
                Type = "CreditCardStatement",
                Description = fileName,
                FileName = fileName,
                Extension = extension,
                FileBytes = DocumentBytes
            };
            try
            {
                //Uploading file into server
                fileId = await documentAppService.UploadDocuments(attributes);
            }
            catch (System.Exception ex)
            {
                string message = $"unable to upload CreditCardStatement file for {attributes.FileName}-{ex.Message}";
                logger.LogInformation(message: message);
            }

            return new EFormResponse() { FileId = fileId, FileName = attributes.FileName, FileBytes = DocumentBytes };
        }
    }


    [HttpPost]
    public Task<decimal> GetCashBack(decimal reqId)
    {
        var FdAcctNo = fdrDbContext.Requests.Where(r => r.RequestId == reqId).AsNoTracking().FirstOrDefault()!.FdAcctNo;
        var cashback = fdrDbContext.StatementDetails.AsNoTracking().Where(x => x.AccountNo == FdAcctNo && x.TransDescription!.ToUpper().Contains("CASHBACK"))
            .Sum(y => y.TransAmount);
        return Task.FromResult(cashback ?? 0);
    }


    private async Task<ApiResponseModel<TableDataSource>> GenerateReportOnFromToDate(CreditCardStatementReportRequest request, string cardNumber)
    {
        var apiResponse = new ApiResponseModel<TableDataSource>();
        TableDataSource tableDataSource = new() { CreditCardTransaction = [] };

        var response = await _creditCardsInquiryServiceClient.searchCreditCardHistoryAsync(new()
        {
            cardNo = cardNumber,
            fromDate = request.Parameter!.FromDate,
            toDate = request.Parameter.ToDate,
            creditOnly = request.Parameter.IsCredit,
            debitOnly = request.Parameter.IsDebit,
            description = request.Parameter.Description,
        });

        if (response is null || response.searchCreditCardHistoryResult.Length == 0)
            return apiResponse.Success(tableDataSource, message: "No Data for selected period");


        UpdateTransactionAmountIfCredit(response.searchCreditCardHistoryResult);


        tableDataSource = GetCurrentCardBalance(response.searchCreditCardHistoryResult, tableDataSource);

        if (!string.IsNullOrEmpty(request?.Parameter?.Description))
        {
            tableDataSource.CreditCardTransaction = tableDataSource.CreditCardTransaction?.Where(x => x.descriptionField == request?.Parameter?.Description).ToList();
        }

        return apiResponse.Success(tableDataSource);

    }

    private async Task<ApiResponseModel<TableDataSource>> GenerateReportOnCycleToDate(CreditCardStatementReportRequest request, string cardNumber, CardtypeEligibilityMatix? cardMatrixConfig)
    {
        var apiResponse = new ApiResponseModel<TableDataSource>();
        TableDataSource tableDataSource = new() { CreditCardTransaction = [] };

        DateTime frmDate = GetFromDateForCycle(cardMatrixConfig);
        request.FromDate = frmDate.ToString(ConfigurationBase.ReportDateFormat);
        request.ToDate = DateTime.Now.ToString(ConfigurationBase.ReportDateFormat);

        getCurrentActivityRequest getCurrentActivityRequest = new()
        {
            cardNo = cardNumber,
            cardId = ""
        };

        var response = await _creditCardsInquiryServiceClient.getCurrentActivityAsync(getCurrentActivityRequest);

        if (response is null || response.getCurrentActivityResult.Length == 0)
            return apiResponse.Success(tableDataSource, message: "No Data for selected period");


        UpdateTransactionAmountIfCredit(response.getCurrentActivityResult);


        tableDataSource = GetCurrentCardBalance(response.getCurrentActivityResult, tableDataSource);

        if (!string.IsNullOrEmpty(request?.Parameter?.Description))
        {
            tableDataSource.CreditCardTransaction = tableDataSource.CreditCardTransaction?.Where(x => x.descriptionField == request?.Parameter?.Description).ToList();
        }


        return apiResponse.Success(tableDataSource);



        static DateTime GetFromDateForCycle(CardtypeEligibilityMatix? cardMatrixConfig)
        {
            var startDateFirstDayOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            if (cardMatrixConfig?.IsCorporate == true) //boruj 2021-08-18: if corp it will get trans that happened from the first day of the month
                return startDateFirstDayOfMonth;

            if (DateTime.Now.Day >= 16)
                return startDateFirstDayOfMonth.AddDays(16);

            return startDateFirstDayOfMonth.AddDays(15).AddMonths(-1);
        }
    }

    private async Task<ApiResponseModel<TableDataSource>> GenerateReportOnMonthYear(CreditCardStatementReportRequest request, string cardNumber, CardtypeEligibilityMatix? cardMatrixConfig)
    {
        var apiResponse = new ApiResponseModel<TableDataSource>();
        TableDataSource tableDataSource = new() { CreditCardTransaction = new() };

        bool isCoBrand = cardMatrixConfig != null && (cardMatrixConfig.IsCobrandCredit == true || cardMatrixConfig.IsCobrandPrepaid == true);
        var selMonth = Convert.ToInt32(request.FromDate);
        var selYear = Convert.ToInt32(request.ToDate);

        DateTime frmDate, todate;
        DateTime dt = new DateTime(selYear, selMonth, 1); //first day of the month
        if (cardMatrixConfig?.IsCorporate == true)
        {
            frmDate = dt;                                //first day of the month
            todate = frmDate.AddMonths(1).AddHours(-1); //last day of the month
        }
        else
        {
            frmDate = dt.AddMonths(-1).AddDays(15);      //the 16th of prev month
            todate = dt.AddDays(14);                    //the 15th of the current month
        }


        request.FromDate = frmDate.ToString(ConfigurationBase.ReportDateFormat);
        request.ToDate = todate.ToString(ConfigurationBase.ReportDateFormat);

        getArchivedMonthlyStatementRequest getArchivedMonthlyStatementRequest = new()
        {
            cardNo = cardNumber,
            month = selMonth,
            year = selYear,
            cardId = ""
        };

        var monthlyStatement = (await _creditCardsInquiryServiceClient.getArchivedMonthlyStatementAsync(getArchivedMonthlyStatementRequest)).getArchivedMonthlyStatementResult;


        if (monthlyStatement is null || monthlyStatement.transactions.Length == 0)
            return apiResponse.Success(tableDataSource, message: "No Data for selected period");



        if (isCoBrand)
        {
            tableDataSource.AnnualPoints = monthlyStatement.annualPoints ?? "0";
            tableDataSource.BonusPoints = monthlyStatement.bonusPoints ?? "0";
            tableDataSource.InternationalPoints = monthlyStatement.internationalPoints ?? "0";
            tableDataSource.LocalPoints = monthlyStatement.localPoints ?? "0";
            tableDataSource.IsCobrand = true;
        }

        UpdateTransactionAmountIfCredit(monthlyStatement.transactions);

        tableDataSource = GetCurrentCardBalance(monthlyStatement.transactions, tableDataSource);

        if (!string.IsNullOrEmpty(request?.Parameter?.Description))
        {
            tableDataSource.CreditCardTransaction = tableDataSource.CreditCardTransaction?.Where(x => x.descriptionField == request?.Parameter?.Description).ToList();
        }

        return apiResponse.Success(tableDataSource);

    }

    private void UpdateTransactionAmountIfCredit(creditCardTransaction[] transactions)
    {
        foreach (var transaction in transactions)
        {
            if (transaction.isCredit)
                transaction.amount = -1 * transaction.amount;
        }
    }

    private TableDataSource GetCurrentCardBalance(creditCardTransaction[] transactions, TableDataSource tableDataSource)
    {
        var previousBalance = transactions.OrderBy(x => x.date).FirstOrDefault(x => x.description.ToUpper().Contains(ConfigurationBase.PreviousBalanceDescription));
        double previousBalanceAmount = previousBalance != null ? previousBalance.amount * -1 : 0;

        var creditCardTransaction = transactions.Select(item => new CustomCardTransactionsDTO()
        {
            amountField = Convert.ToDecimal(item.amount),
            DebitAmount = item.isCredit ? null : Convert.ToDecimal(item.amount),
            CreditAmount = item.isCredit ? Convert.ToDecimal(item.amount) * -1 : null,
            cardTypeField = item.cardType,
            currencyField = item.currency,
            dateField = item.dateSpecified ? item.date : null,
            postingDateField = item.postingDateSpecified ? item.postingDate : null,
            foreignAmountField = Convert.ToDecimal(item.foreignAmount),
            isCreditField = item.isCredit,
            descriptionField = item.description.Replace("/ +/g", " "),
            transactionCodeField = item.transactionCode,
            visaMCIndicatorField = item.visaMCIndicator,
            CardCurrentBalance = Convert.ToDecimal(UpdatePreviousBalanceAmountIfDontHave(item))
        });


        double UpdatePreviousBalanceAmountIfDontHave(creditCardTransaction transaction)
        {
            if (transaction.description.Contains(ConfigurationBase.PreviousBalanceDescription))
                return transaction.amount;

            previousBalanceAmount = transaction.amount + previousBalanceAmount;
            return previousBalanceAmount;
        }

        tableDataSource.CreditCardTransaction?.AddRange(creditCardTransaction);

        return tableDataSource;
    }

    private async Task ValidateBiometricStatus(decimal requestId)
    {
        var request = fdrDbContext.Requests.AsNoTracking().FirstOrDefault(x => x.RequestId == requestId) ?? throw new ApiException(message: "Invalid request Id ");

        var bioStatus = await _customerProfileCommonApi.GetBiometricStatus(request!.CivilId);
        if (bioStatus.ShouldStop)
            throw new ApiException(message: GlobalResources.BioMetricRestriction);
    }
}

