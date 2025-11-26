using CreditCardsSystem.Data;
using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Models.Access;
using CreditCardsSystem.Domain.Models.Reports;
using CreditCardsSystem.Domain.Models.RequestActivity;
using CreditCardsSystem.Utility.Extensions;
using Kfh.Aurora.Auth;
using Kfh.Aurora.Logging;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Data;
using Telerik.DataSource.Extensions;
using Exception = System.Exception;

namespace CreditCardsSystem.Application.Report;

public class EODReportsAppService(IAuditLogger<EODReportsAppService> auditLogger, FdrDBContext fdrDbContext, IReportAppService reportAppService, IAuthManager authManager) : BaseApiResponse, IEODReportsAppService, IAppService
{
    private readonly IAuditLogger<EODReportsAppService> auditLogger = auditLogger;
    private readonly FdrDBContext fdrDbContext = fdrDbContext;
    private readonly IReportAppService reportAppService = reportAppService;
    private readonly IAuthManager _authManager = authManager;

    [HttpGet]
    public async Task<ReadOnlyCollection<CFUActivityDto>> GetCFUActivities()
    {
        try
        {
            auditLogger.Log.Information("Report creating for GetCFUActivities");
            var cfus = fdrDbContext.CfuActivities.AsNoTracking().ProjectToType<CFUActivityDto>().ToReadOnlyCollection();
            auditLogger.Log.Information("Report creation completed for GetCFUActivities");
            return cfus;
        }
        catch (Exception ex)
        {
            auditLogger.Log.Information(ex, "Failed report creation for GetCFUActivities");
            return ReadOnlyCollection<CFUActivityDto>.Empty;
        }
    }

    [HttpPost]
    public async Task<ApiResponseModel<EODBranchReportDto>> GetEODBranchReport([FromBody] EODBranchReportRequest request)
    {

        try
        {
            auditLogger.Log.Information($"Loading {nameof(GetEODBranchReport)}");

            var requestActivities = fdrDbContext.RequestActivities.Where(x => x.BranchId == request.BranchId && x.CfuActivityId != null).AsNoTracking();

            if (request.TellerId is not null)
                requestActivities = requestActivities.Where(x => x.TellerId == request.TellerId);

            if (request.FromCreationDate is not null)
                requestActivities = requestActivities.Where(x => x.CreationDate >= request.FromCreationDate);

            if (request.ToCreationDate is not null)
                requestActivities = requestActivities.Where(x => x.CreationDate <= request.ToCreationDate);

            if (request.CfuActivityId is not null)
                requestActivities = requestActivities.Where(x => x.CfuActivityId == request.CfuActivityId);


            List<EODBranchReportItemDto> reportItems = await (from ra in requestActivities
                                                              join cfa in fdrDbContext.CfuActivities on ra.CfuActivityId equals cfa.CfuActivityId
                                                              group ra by new { ra.CfuActivityId, cfa.DescriptionEn, cfa.DescriptionAr } into gra
                                                              select new EODBranchReportItemDto
                                                              {
                                                                  ActivityDescription = gra.Key.DescriptionEn,
                                                                  ActivityId = gra.Key.CfuActivityId,
                                                                  Summaries = gra.GroupBy(x => new { x.TellerId, x.TellerName })
                                                                  .Select((x) => new EODReportSummary()
                                                                  {
                                                                      TellerId = x.Key.TellerId ?? 0,
                                                                      TellerName = x.Key.TellerName ?? "",
                                                                      CfuActivityId = gra.Key.CfuActivityId ?? 0,
                                                                      NoOfActivities = gra.Count(),
                                                                  })
                                                              }).ToListAsync();

            auditLogger.Log.Information($"Success {nameof(GetEODBranchReport)}");
            return Success(new EODBranchReportDto()
            {
                BranchName = requestActivities.FirstOrDefault()?.BranchName?.Split("-")[0] ?? "",
                Items = reportItems
            });




        }
        catch (System.Exception ex)
        {
            auditLogger.Log.Information($"Failed {nameof(GetEODBranchReport)}");
            return Failure<EODBranchReportDto>(message: ex.Message);
        }
    }

    [HttpPost]
    public async Task<ApiResponseModel<EODStaffReportDto>> GetEODStaffReport([FromBody] EODStaffReportRequest request)
    {

        try
        {
            auditLogger.Log.Information($"Loading {nameof(GetEODStaffReport)}");

            var requestActivities = fdrDbContext.RequestActivities.Where(x => x.BranchId == request.BranchId && x.TellerId == request.TellerId && x.CfuActivityId != null).AsNoTracking();


            if (request.FromCreationDate is not null)
                requestActivities = requestActivities.Where(x => x.CreationDate >= request.FromCreationDate);

            if (request.ToCreationDate is not null)
                requestActivities = requestActivities.Where(x => x.CreationDate <= request.ToCreationDate);



            if (!requestActivities.Any())
                return Success(new EODStaffReportDto() { TellerId = (decimal)request.TellerId! });

            EODStaffReportDto response = new()
            {

                TellerId = (decimal)request.TellerId!,
                TellerName = requestActivities.FirstOrDefault()?.TellerName ?? "",
                BranchName = requestActivities.FirstOrDefault()?.BranchName?.Split("-")[0] ?? "",
                Summaries = await (from ra in requestActivities
                                   join cfa in fdrDbContext.CfuActivities on ra.CfuActivityId equals cfa.CfuActivityId
                                   group ra by new { ra.CfuActivityId, cfa.DescriptionEn, cfa.DescriptionAr } into gra
                                   select new EODStaffReportSummary
                                   {
                                       ActivityId = gra.Key.CfuActivityId,
                                       ActivityDescription = gra.Key.DescriptionEn,
                                       Count = gra.Count()

                                   }).ToListAsync()
            };



            auditLogger.Log.Information($"Success {nameof(GetEODStaffReport)}");
            return Success(response);

        }
        catch (Exception ex)
        {
            auditLogger.Log.Information(ex, $"Failed  {nameof(GetEODStaffReport)}");
            return Failure<EODStaffReportDto>(message: ex.Message);
        }
    }

    [HttpPost]
    public async Task<ApiResponseModel<EODSubReport>> GetEODSubReport([FromBody] EODSubReportRequest request)
    {

        try
        {
            auditLogger.Log.Information($"Loading {nameof(GetEODSubReport)}");

            bool canViewCardNumber = _authManager.HasPermission(Permissions.CreditCardsNumber.View());

            var requestActivities = fdrDbContext.RequestActivities.Where(x => x.BranchId == request.BranchId && x.CfuActivityId != null)
                .AsNoTracking();

            if (request.TellerId is not null)
                requestActivities = requestActivities.Where(x => x.TellerId == request.TellerId);

            if (request.FromCreationDate is not null)
                requestActivities = requestActivities.Where(x => x.CreationDate >= request.FromCreationDate);

            if (request.ToCreationDate is not null)
                requestActivities = requestActivities.Where(x => x.CreationDate <= request.ToCreationDate);

            if (request.CfuActivityId is not null)
                requestActivities = requestActivities.Where(x => x.CfuActivityId == request.CfuActivityId);



            var response = await (from ra in requestActivities
                                  join r in fdrDbContext.Requests.AsNoTracking() on ra.RequestId equals r.RequestId into rar
                                  from rarl in rar.DefaultIfEmpty()
                                  select new
                                  {
                                      ra.TellerId,
                                      ra.TellerName,
                                      ra.CivilId,
                                      ra.CustomerName,
                                      rarl.CardNo
                                  }).GroupBy(x => new { x.TellerName, x.TellerId })
                            .Select(x => new EODSubReport()
                            {
                                TellerId = (long?)x.Key.TellerId,
                                TellerName = x.Key.TellerName ?? "",
                                ActivityName = ((CFUActivity)request.CfuActivityId!).GetDescription(),
                                Summaries = x.Select(s => new EODSubReportSummary()
                                {
                                    CardNumber = canViewCardNumber ? s.CardNo ?? "" : s.CardNo.Masked(6, 6),
                                    CustomerName = s.CustomerName,
                                    CivilId = s.CivilId
                                })
                            }).FirstOrDefaultAsync();

            auditLogger.Log.Information($"Success {nameof(GetEODSubReport)}");
            return Success(response);
        }
        catch (Exception ex)
        {
            auditLogger.Log.Information($"Failed {nameof(GetEODSubReport)}");
            return Failure<EODSubReport>(message: ex.Message);
        }
    }


    [HttpPost]
    public async Task<ApiResponseModel<EFormResponse>> PrintEODStaffReport([FromBody] EODStaffReportDto reportData) => await reportAppService.PrintDynamicReport(reportData);


    [HttpPost]
    public async Task<ApiResponseModel<EFormResponse>> PrintEODBranchReport([FromBody] EODBranchReportDto reportData) => await reportAppService.PrintDynamicReport(reportData);


}

