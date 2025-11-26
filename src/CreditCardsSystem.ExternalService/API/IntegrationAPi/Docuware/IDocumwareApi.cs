using CreditCardsSystem.Domain.Models.Reports;

namespace Dashboard.ExternalService.IntegrationAPi.CashApi;

public interface IDocumwareApi
{
    Task UploadDocument(StoreBytes req);
    Task<DocuwareDocumentDto[]> GetAllDocuments(GetAllDocumentRequest req);
    Task<byte[]?> DownloadFile(DocumentFileRequest req);
}