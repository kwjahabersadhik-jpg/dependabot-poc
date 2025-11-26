using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.Reports;
using Refit;

namespace CreditCardsSystem.Domain.Interfaces;

public interface IDocumentAppService : IRefitClient
{
    const string Controller = "/api/Document/";

    [Post($"{Controller}{nameof(GetDocumentFiles)}")]
    Task<ApiResponseModel<DocuwareDocumentDto[]>> GetDocumentFiles(SearchDocumentAttributes attributes);

    [Post($"{Controller}{nameof(DownloadFile)}")]
    Task<ApiResponseModel<EFormResponse>> DownloadFile(DownloadDocumentAttributes attributes);

    [Post($"{Controller}{nameof(UploadDocuments)}")]
    Task<Guid> UploadDocuments(DocumentFileAttributes attributes);

    [Post($"{Controller}{nameof(GetFileByFieldId)}")]
    Task<byte[]> GetFileByFieldId(Guid fieldId);

}