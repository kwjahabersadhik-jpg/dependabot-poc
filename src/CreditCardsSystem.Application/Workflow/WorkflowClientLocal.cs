using CreditCardsSystem.Domain.Models.Workflow;
using Kfh.Aurora.Workflow.Dto;
using InstanceRunRequest = Kfh.Aurora.Workflow.Dto.InstanceRunRequest;

namespace CreditCardsSystem.Application.Workflow;




public interface IWorkflowClientLocal
{
    Task<bool> HealthCheck();
    Task<InstanceCancelResponse> CancelWorkFlowInstance(InstanceCancelRequest request);
    Task<GetTasksResponse> GetTasks(string? applicationName = null);

    Task<GetUserTasksResponse> GetUserTasks(string kfhId, string? applicationName = null);

    Task<CompleteTaskResponse> CompleteTask(CompleteTaskRequest request);

    Task<InstanceRunResponse> RunWorkflowInstance(InstanceRunRequest request);

    Task<AttachFileResponse> AttachFile(AttachFileRequest request, CancellationToken token);

    Task<Stream> DownloadAttachment(Guid fileId, CancellationToken token);

    Task<DeleteAttachmentResponse> DeleteAttachments(DeleteAttachmentsRequest req, CancellationToken token);

    Task<GetAttachmentsResponse?> GetAttachmentInfo(GetAttachmentsRequest req, CancellationToken token);

    Task<GetCommentsResponse> GetComments(GetCommentsRequest request);

    Task<ClaimTaskResponse> ClaimTask(ClaimTaskRequest request);

    Task<UnclaimTaskResponse> UnclaimTask(UnclaimTaskRequest request);
}

//public class WorkflowClientLocal : IWorkflowClient
//{
//    private readonly IHttpClientFactory factory;
//    private readonly IAuthManager authManager;
//    private readonly HttpClient _client;
//    public WorkflowClientLocal(IHttpClientFactory factory, IAuthManager authManager)
//    {
//        this.factory = factory;
//        this.authManager = authManager;
//        _client = factory.CreateClient("enigma");
//    }


//    public async Task<bool> HealthCheck()
//    {
//        try
//        {
//            await _client.GetAsync(_client.BaseAddress!.AbsoluteUri);
//            return true;
//        }
//        catch (Exception ex)
//        {
//            return false;
//        }
//    }

//    public async Task<CancelInstanceResponse?> CancelWorkflowInstance(CancelInstanceRequest request)
//    {
//        var result = await _client.PostAsJsonAsync("workflow/cancel", request);

//        return await result.Content.ReadFromJsonAsync<CancelInstanceResponse?>();
//    }

//    public async Task<GetTasksResponse> GetTasks(string? applicationName = null)
//    {
//        var query = HttpUtility.ParseQueryString(string.Empty);

//        if (!string.IsNullOrEmpty(applicationName))
//        {
//            query["Application"] = applicationName;
//        }

//        var builder = new UriBuilder(_client.BaseAddress + "task/get-tasks");
//        builder.Query = query.ToString();

//        var result = await _client.GetAsync(builder.ToString());

//        var response = await result.Content.ReadFromJsonAsync<GetTasksResponse>();
//        return response!;
//    }

//    public async Task<GetUserTasksResponse> GetUserTasks(string kfhId, string? applicationName = null)
//    {
//        try
//        {
//            var query = HttpUtility.ParseQueryString(string.Empty);

//            query["KfhId"] = authManager.GetUser()?.KfhId;

//            if (!string.IsNullOrEmpty(applicationName))
//            {
//                query["Application"] = applicationName;
//            }

//            var builder = new UriBuilder(_client.BaseAddress + "task/get-user-tasks")
//            {
//                Query = query.ToString()
//            };

//            var result = await _client.GetAsync(builder.ToString());
//            var response = await result.Content.ReadFromJsonAsync<GetUserTasksResponse>();

//            Console.WriteLine("DT " + response.Tasks.FirstOrDefault()?.CreatedDate);

//            return response!;
//        }
//        catch (Exception)
//        {
//            return new GetUserTasksResponse() { };
//        }

//    }

//    public async Task<CompleteTaskResponse> CompleteTask(CompleteTaskRequest request)
//    {
//        var result = await _client.PostAsJsonAsync("task/complete-task", request);

//        var response = await result.Content.ReadFromJsonAsync<CompleteTaskResponse>();
//        return response!;
//    }

//    public async Task<InstanceRunResponse> RunWorkflowInstance(InstanceRunRequest request)
//    {
//        var result = await _client.PostAsJsonAsync("workflow/start", request);

//        var response = await result.Content.ReadFromJsonAsync<InstanceRunResponse>();
//        return response!;
//    }

//    public async Task<AttachFileResponse> AttachFile(AttachFileRequest request, CancellationToken token)
//    {
//        using var multipartFormContent = new MultipartFormDataContent
//        {
//            { new StringContent(request.InstanceId.ToString()), "instanceId" },
//            { new StringContent(request.TaskId.ToString()), "taskId" },
//            { new StringContent(request.UploadedBy), "uploadedBy" },
//            { new StringContent(request.File.Name), "attachmentName" }
//        };

//        var fileStreamContent = new StreamContent(request.File.GetFile());
//        fileStreamContent.Headers.ContentType =
//            new MediaTypeHeaderValue(GetMimeTypeForFileExtension(request.File.FileName));
//        multipartFormContent.Add(fileStreamContent, name: "attachment", fileName: request.File.FileName);

//        var response = await _client.PostAsync("workflow/add-attachment", multipartFormContent, token);
//        response.EnsureSuccessStatusCode();

//        return (await response.Content.ReadFromJsonAsync<AttachFileResponse>(cancellationToken: token))!;
//    }

//    public async Task<GetAttachmentsResponse?> GetAttachmentInfo(GetAttachmentsRequest req, CancellationToken token)
//    {
//        var result = await _client.PostAsJsonAsync("workflow/get-attachment-info", req, cancellationToken: token);
//        result.EnsureSuccessStatusCode();

//        return await result.Content.ReadFromJsonAsync<GetAttachmentsResponse>(cancellationToken: token);
//    }

//    public async Task<Stream> DownloadAttachment(Guid fileId, CancellationToken token)
//    {
//        var result = await _client.GetStreamAsync($"workflow/download-attachment?fileId={fileId}", cancellationToken: token);
//        return result;
//    }

//    //public async Task<DeleteAttachmentResponse> DeleteAttachments(DeleteAttachmentsRequest req, CancellationToken token)
//    //{
//    //    var result = await _client.PostAsJsonAsync("workflow/delete-attachment", req, cancellationToken: token);
//    //    result.EnsureSuccessStatusCode();

//    //    var response = await result.Content.ReadFromJsonAsync<DeleteAttachmentResponse>(cancellationToken: token);
//    //    return response!;
//    //}

//    public async Task<GetCommentsResponse> GetComments(GetCommentsRequest request)
//    {
//        var result = await _client.PostAsJsonAsync("task/get-comments", request);
//        result.EnsureSuccessStatusCode();

//        var response = await result.Content.ReadFromJsonAsync<GetCommentsResponse>();
//        return response!;
//    }

//    public async Task<ClaimTaskResponse> ClaimTask(ClaimTaskRequest request)
//    {
//        var result = await _client.PostAsJsonAsync("task/claim-task", request);
//        result.EnsureSuccessStatusCode();

//        var response = await result.Content.ReadFromJsonAsync<ClaimTaskResponse>();
//        return response!;
//    }

//    public async Task<UnclaimTaskResponse> UnclaimTask(UnclaimTaskRequest request)
//    {
//        var result = await _client.PostAsJsonAsync("task/unclaim-task", request);
//        result.EnsureSuccessStatusCode();

//        var response = await result.Content.ReadFromJsonAsync<UnclaimTaskResponse>();
//        return response!;
//    }

//    private static string GetMimeTypeForFileExtension(string filePath)
//    {
//        const string defaultContentType = "application/octet-stream";

//        var provider = new FileExtensionContentTypeProvider();

//        if (provider.TryGetContentType(filePath, out var contentType))
//            return contentType;

//        contentType = defaultContentType;

//        return contentType;
//    }

//    public Task<DeleteAttachmentResponse> DeleteAttachments(DeleteAttachmentsRequest req, CancellationToken token)
//    {
//        throw new NotImplementedException();
//    }

//    public Task<TeamAssigneesResponse?> GetAssignees(Guid taskId)
//    {
//        throw new NotImplementedException();
//    }

//    public Task<AddCommentsResponse> AddComments(AddCommentsRequest request)
//    {
//        throw new NotImplementedException();
//    }

//    public Task<GetUserTeamsResponse> GetUserTeams(string kfhId)
//    {
//        throw new NotImplementedException();
//    }

//    public Task<List<GetTaskHistoryResponse>?> GetTaskHistory(GetTaskHistoryRequest request)
//    {
//        throw new NotImplementedException();
//    }

//    public Task<GetTaskDetailResponse> GetTaskDetail(GetTaskDetailRequest request)
//    {
//        throw new NotImplementedException();
//    }

//    public Task<GetInstanceResponse> GetWorkflowInstance(GetInstanceRequest request)
//    {
//        throw new NotImplementedException();
//    }
//}