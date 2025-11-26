using CreditCardsSystem.Data;
using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Models.Reports;
using CreditCardsSystem.Domain.Shared.Interfaces;
using Dashboard.ExternalService.IntegrationAPi.CashApi;
using DocumentManagementSerivceReference;
using Kfh.Aurora.Integration;
using Kfh.Aurora.ReportHistory;
using Kfh.Aurora.ReportHistory.Models;
using Kfh.Aurora.Storage;
using Kfh.Aurora.Storage.Models;
using Kfh.Aurora.Utilities.FileValidation;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Telerik.DataSource.Extensions;

namespace CreditCardsSystem.Application.Documents
{
    public class DocumentAppService(IReportHistoryClient reportHistoryClient,
                                    IConfiguration configuration,
                                    IDocumwareApi documwareApi,
                                    IHttpContextAccessor contextAccessor,
                                    IGroupsAppService groupsAppService,
                                    IIntegrationUtility integrationUtility,
                                    IOptions<IntegrationOptions> options,
                                    IOptions<DocuwareOptions> docuwareOptions,
                                    FdrDBContext fdrDbContext,
                                    IStorageClient storageClient, IFileValidator fileValidator) : BaseApiResponse, IDocumentAppService, IAppService
    {
        private readonly IDocumwareApi docuwareApi = documwareApi;
        private readonly IHttpContextAccessor contextAccessor = contextAccessor;
        private readonly IConfiguration _configuration = configuration;
        private readonly IReportHistoryClient _reportHistoryClient = reportHistoryClient;
        private readonly DocumentManagementSerivceClient _documentServiceClient = integrationUtility.GetClient<DocumentManagementSerivceClient>(options.Value.Client, options.Value.Endpoints.DocumentManagement, options.Value.BypassSslValidation);
        private readonly IStorageClient _storageClient = storageClient;
        private readonly IFileValidator _fileValidator = fileValidator;

        [HttpPost]
        public async Task<Guid> UploadDocuments([FromBody] DocumentFileAttributes attributes)
        {
            var isValidExtension = await _fileValidator.IsValidExtension(attributes.FileBytes ?? [], ["pdf", "xls", "xlsx", "docx", "jpg", "jpeg", "png", "svg"]);

            if (!Helpers.IsValidFile(attributes.FileName, attributes.FileBytes ?? [], isValidExtension.IsValid, maxFileSizeInMB: 10))
                throw new ApiException(message: GlobalResources.InvalidFile);
            
            string? kfhId = string.Empty;

            if (contextAccessor.HttpContext?.User.HasClaim(x => x.Type == "sub") ?? false)
            {
                kfhId = contextAccessor.HttpContext?.User.Claims.Single(x => x.Type == "sub").Value;
            }


            if (attributes.FileServer == FileServer.Docuware)
            {
                await docuwareApi.UploadDocument(new()
                {
                    AppID = docuwareOptions.Value.ApplicationId,
                    WindowID = docuwareOptions.Value.WindowId,
                    Extension = $"{attributes.Extension}",
                    FileBytes = attributes.FileBytes,
                    KeyValueList1 = JsonConvert.SerializeObject(attributes.MetaData?.Select(x => new keyValuePair() { key = x.Key, value = x.Value.ToString() })),
                });

                return Guid.Empty;
            }

            var request = new CreateReportHistoryDto
            {
                File = FileParameters.Create("file1", attributes.FileBytes!, attributes.FileName),
                Description = attributes.Description,
                Application = _configuration["Serilog:Properties:Application"] ?? "",
                ApplicationAr = _configuration["Serilog:Properties:Application"] ?? "",
                KfhId = string.IsNullOrEmpty(kfhId) ? attributes.KfhId : kfhId,
                Type = attributes.Type,// "Card Issuance E-Form",
                Metadata = new()
                {
                {"dateOfPrinting", DateTimeOffset.Now.ToString()},
                {"typeIndicator", attributes.Extension.ToString()},
                {"requestId", attributes.RequestId}
            }
            };

            var status = await _reportHistoryClient.CreateReport(request);

            return status is { Success: true } ? status.Result!.FileId : Guid.Empty;
        }


        [HttpPost]
        public async Task<ApiResponseModel<DocuwareDocumentDto[]>> GetDocumentFiles([FromBody] SearchDocumentAttributes attributes)
        {

            if (attributes.FileServer == FileServer.Docuware)
            {
                var response = await docuwareApi.GetAllDocuments(new()
                {
                    AppID = docuwareOptions.Value.ApplicationId,
                    WindowID = docuwareOptions.Value.WindowId,
                    CallingAppUserID = "KFH",
                    sKeys = attributes.MetaData?.Select(x => x.Key).ToArray(),
                    sValues = attributes.MetaData?.Select(x => x.Value).ToArray()
                });


                return Success(response);
            }


            return Success<DocuwareDocumentDto[]>([]);
        }

        [HttpPost]
        public async Task<ApiResponseModel<EFormResponse>> DownloadFile([FromBody] DownloadDocumentAttributes attributes)
        {
            if (attributes.FileServer == FileServer.Docuware)
            {
                var response = await docuwareApi.DownloadFile(new()
                {
                    AppID = docuwareOptions.Value.ApplicationId,
                    WindowID = docuwareOptions.Value.WindowId,
                    DocId = attributes.DocId,
                    CallingAppUserID = "KFH"
                });


                return Success(new EFormResponse() { FileBytes = response });
            }



            var fileBytes = (await _documentServiceClient.getDocumentAsync(new getDocumentRequest()
            {
                cabinetName = docuwareOptions.Value.CabinetName,
                docId = attributes.DocId
            }))?.getDocumentResult;


            return Success(new EFormResponse() { FileBytes = fileBytes });
        }

        [HttpGet]
        public async Task<byte[]> GetFileByFieldId(Guid fieldId) => (await _storageClient.DownloadFileAsByteArray(fieldId))!;


    }
}
