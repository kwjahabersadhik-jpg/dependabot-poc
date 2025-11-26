using CreditCardsSystem.Domain.Models.Options;
using CreditCardsSystem.Domain.Models.Reports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RestSharp;

namespace Dashboard.ExternalService.IntegrationAPi.CashApi;

public class DocumwareApi : IDocumwareApi
{
    private readonly RestClient _client;
    private readonly IConfiguration _configuration;
    private readonly IOptions<IntegrationOptions> _integrationOptions;
    private readonly ILogger<DocumwareApi> _logger;

    public DocumwareApi(IConfiguration configuration, IOptions<IntegrationOptions> integrationOptions, ILogger<DocumwareApi> logger)
    {
        var options = new RestClientOptions(integrationOptions.Value.Endpoints.DocumentManagement)
        {
            MaxTimeout = -1,
        };

        if (integrationOptions.Value.BypassSslValidation)
        {
            options.RemoteCertificateValidationCallback = (_, _, _, _) => true;
        }

        _client = new RestClient(options);
        _client.AddDefaultHeader("apiKey", configuration["Docuware:ApiKey"]!);

        _configuration = configuration;
        _integrationOptions = integrationOptions;
        _logger = logger;
    }





    public async Task UploadDocument(StoreBytes req)
    {
        var request = new RestRequest("/DWWSIntegration/api/Documents/StoreBytes", Method.Post)
        {
            AlwaysMultipartFormData = true
        };

        request.AddParameter("WindowID", _configuration["Docuware:WindowID"])
        .AddParameter("AppID", _configuration["Docuware:ApplicationID"])
        .AddParameter("Extension", req.Extension)
        .AddParameter("KeyValueList1", req.KeyValueList1)
        .AddParameter("KeyValueList2", req.KeyValueList2)
        .AddFile("File", req.FileBytes, req.FileName);
        await _client.ExecuteAsync(request);
    }

    public async Task<DocuwareDocumentDto[]> GetAllDocuments(GetAllDocumentRequest req)
    {
        var request = new RestRequest("/DWWSIntegration/api/Documents/GetAllDocuments", Method.Post).AddJsonBody(JsonConvert.SerializeObject(req));

        RestResponse response = await _client.ExecuteAsync(request);
        if (response.IsSuccessStatusCode)
            return JsonConvert.DeserializeObject<DocuwareDocumentDto[]>(response.Content!) ?? [];

        return [];
    }


    public async Task<byte[]?> DownloadFile(DocumentFileRequest req)
    {
        var request = new RestRequest($"/DWWSIntegration/api/Documents/DownloadDocumentPDF", Method.Get)
        {
            AlwaysMultipartFormData = true
        };

        request.AddParameter("WindowID", _configuration["Docuware:WindowID"])
     .AddParameter("AppID", _configuration["Docuware:ApplicationID"])
     .AddParameter("CallingAppUserID", "KFH")
        .AddParameter("DocID", req.DocId);

        RestResponse response = await _client.ExecuteAsync(request);
        if (response.IsSuccessStatusCode)
            return response.RawBytes!;


        return [];
    }
}
