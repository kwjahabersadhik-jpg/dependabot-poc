using CreditCardsSystem.Domain.Interfaces;
using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.Workflow;
using Kfh.Aurora.Workflow.Dto;
using Refit;
using FileInfo = Kfh.Aurora.Storage.Models.FileInfo;


namespace CreditCardsSystem.Domain.Shared.Interfaces.Workflow;

public interface IWorkflowAppService : IRefitClient
{
    const string Controller = "/api/Workflow/";

    [Post($"{Controller}{nameof(CancelWorkFlow)}")]
    Task<CancelInstanceResponse?> CancelWorkFlow(Guid? instanceId, Guid? taskId);


    [Get($"{Controller}{nameof(GetUserTasks)}")]
    Task<ApiResponseModel<List<TaskResult>>> GetUserTasks();

    [Post($"{Controller}{nameof(CreateWorkFlow)}")]
    Task<InstanceRunResponse> CreateWorkFlow(WorkFlowMakerRequest request);

    [Post($"{Controller}{nameof(SubmitCorporateMakerRequest)}")]
    Task SubmitCorporateMakerRequest([Body] SubmitCorporateMakerInput input);

    [Post($"{Controller}{nameof(CompleteTask)}")]
    Task CompleteTask([Body] CompleteTaskRequest request, decimal? kfhId = null);

    [Post($"{Controller}{nameof(DownloadAttachment)}")]
    Task<Stream> DownloadAttachment(Guid fileId);

    [Post($"{Controller}{nameof(GetAttachmentInfo)}")]
    Task<List<FileInfo>?> GetAttachmentInfo(Guid instanceId);

    [Post($"{Controller}{nameof(DeleteAttachment)}")]
    Task<string> DeleteAttachment(Guid instanceId, List<Guid> fileIds);

    [Post($"{Controller}{nameof(GetComments)}")]
    Task<GetCommentsResponse> GetComments(Guid instanceId);

    [Post($"{Controller}{nameof(ClaimTask)}")]
    Task ClaimTask(Guid taskId);

    [Post($"{Controller}{nameof(UnclaimTask)}")]
    Task UnclaimTask(Guid taskId);

    [Get($"{Controller}{nameof(GetTaskById)}")]
    Task<TaskResult> GetTaskById(Guid taskId);


    [Post($"{Controller}{nameof(ReturnToMaker)}")]
    Task ReturnToMaker(ReturnToMakerRequest request);

    [Post($"{Controller}{nameof(ReSubmitTask)}")]
    Task ReSubmitTask(ReturnToMakerRequest request);

    [Get($"{Controller}{nameof(HealthCheck)}")]
    Task<bool> HealthCheck();

    [Get($"{Controller}{nameof(GetCases)}")]
    Task<List<CaseDto>?> GetCases();

    [Get($"{Controller}{nameof(GetCase)}")]
    Task<CaseDetailDto?> GetCase(Guid instanceId);

    [Get($"{Controller}{nameof(GetCaseDetail)}")]
    Task<List<GetTaskHistoryResponse>?> GetCaseDetail([Query] Guid instanceId);

}