using CreditCardsSystem.Data;
using CreditCardsSystem.Data.Models;
using CreditCardsSystem.Domain.Models.Migs;
using InformaticaManagementServiceReference;
using Kfh.Aurora.Integration;
using Kfh.Aurora.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Linq.Expressions;

namespace CreditCardsSystem.Application.MIGS;


public class MigsAppService(FdrDBContext fdrDBContext,
                            IAuditLogger<MigsAppService> auditLogger,
                            IIntegrationUtility integrationUtility,
                            IOptions<IntegrationOptions> options,
                            IConfiguration configuration) : BaseApiResponse, IMigsAppService, IAppService
{
    private readonly FdrDBContext fdrDBContext = fdrDBContext;
    private readonly IAuditLogger<MigsAppService> auditLogger = auditLogger;
    private readonly InformaticaManagementServiceClient _informaticaManagementServiceClient = integrationUtility.GetClient<InformaticaManagementServiceClient>(options.Value.Client, options.Value.Endpoints.InformaticaManagement, options.Value.BypassSslValidation);


    [HttpGet]
    public async Task<MasterDto?> GetMasterDataById(int loadId)
    {

        var records = await fdrDBContext.Masters.AsNoTracking().FirstOrDefaultAsync(x => x.LoadId == loadId);

        return records?.Adapt<MasterDto>();
    }

    [HttpGet]
    public async Task<ApiResponseModel<IEnumerable<MasterDto>>> GetLoadIds(string loadDate)
    {

        if (!DateTime.TryParse(loadDate, out DateTime _loadDate))
            return Failure<IEnumerable<MasterDto>>("Invalid load date");

        if (_loadDate == DateTime.MinValue)
            return Failure<IEnumerable<MasterDto>>("Invalid load date");


        auditLogger.Log.Information("Loading load ids for {date}", _loadDate);
        var records = await fdrDBContext.Masters.AsNoTracking().Where(x => x.LoadDate.Date.Equals(_loadDate.Date)).ToListAsync();


        if (records.Count == 0)
        {
            return Failure<IEnumerable<MasterDto>>("no data found");
        }

        var ids = records.Select(r => new MasterDto()
        {
            Id = r.Id,
            LoadId = r.LoadId,
            FileCreatedOn = r.FileCreatedOn,
            Status = r.Status,
            PHXRejectedTransactionsCount = r.PHXRejectedTransactionsCount,
            TotalCreditTransactionsAmount = r.TotalCreditTransactionsAmount,
            TotalDebitTransactionsAmount = r.TotalDebitTransactionsAmount,
            TotalPHXRejectedTransactionsAmount = r.TotalPHXRejectedTransactionsAmount
        });

        auditLogger.Log.Information("Successfully loaded {count} ids for date {date}", ids.Count(), _loadDate);

        return Success(ids.AsEnumerable());
    }


    [HttpPost]
    public async Task<ApiResponseModel> GenerateFile([FromBody] GenerateFileRequestDto request)
    {

        var statusresult = await _informaticaManagementServiceClient.getWorkflowStatusDetailsAsync(new getWorkflowStatusDetailsRequest()
        {
            username = configuration.GetValue<string>("Informatica:Username"),
            password = configuration.GetValue<string>("Informatica:Password"),
            folderName = configuration.GetValue<string>("Informatica:FolderName"),
            wfName = configuration.GetValue<string>("Informatica:FDRWorkflowName"),
            integrationServiceName = ""
        });

        auditLogger.Log.Information("Migs Informatica: Started FDR file generate");

        await fdrDBContext.Transactions.Where(i => i.LoadId == request.LoadId).ExecuteUpdateAsync(s => s.SetProperty(p => p.IsSentToFDR, false));

        GenerateFileRequest GenerateFileRequest = new()
        {
            CreateOn = DateTime.Now,
            IsNewRequest = true,
            LoadId = request.LoadId,
            RequestType = request.FileType
        };
        await fdrDBContext.GenerateFileRequests.AddAsync(GenerateFileRequest);
        await fdrDBContext.SaveChangesAsync();

        try
        {
            auditLogger.Log.Information("Migs Informatica: calling informatica serivce to create workflow");

            var result = await _informaticaManagementServiceClient.startWorkflowAsync(new startWorkflowRequest()
            {
                username = configuration.GetValue<string>("Informatica:Username"),
                password = configuration.GetValue<string>("Informatica:Password"),
                folderName = configuration.GetValue<string>("Informatica:FolderName"),
                wfName = configuration.GetValue<string>("Informatica:FDRWorkflowName"),
                integrationServiceName = ""
            });

            if (result is null)
            {
                await DeleteGeneratedFileRequest(GenerateFileRequest);
                return Failure("Unabel to start Informatica workflow, please try again");
            }


            if (result.startWorkflow.isSuccessful)
                return Success("Informatica workflow started successfully");
        }
        catch (System.Exception ex)
        {
            await DeleteGeneratedFileRequest(GenerateFileRequest);
            auditLogger.Log.Error(ex, "Migs Informatica: Unabel to start Informatica workflow");
            throw;
        }


        return Success("Informatica workflow started successfully");
        async Task DeleteGeneratedFileRequest(GenerateFileRequest GenerateFileRequest)
        {
            fdrDBContext.GenerateFileRequests.Attach(GenerateFileRequest);
            fdrDBContext.GenerateFileRequests.Remove(GenerateFileRequest);
            await fdrDBContext.SaveChangesAsync();
        }
    }




}

public static class Extension
{
    public static IQueryable<TSource> WhereIf<TSource>(this IQueryable<TSource> source, bool condition, Expression<Func<TSource, bool>> predicate)
    {
        if (condition)
            return source.Where(predicate);

        return source;
    }
}
