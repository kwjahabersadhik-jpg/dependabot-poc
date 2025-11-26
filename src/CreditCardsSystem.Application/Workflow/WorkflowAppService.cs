using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Models.Access;
using CreditCardsSystem.Domain.Models.Workflow;
using CreditCardsSystem.Domain.Shared.Interfaces.Workflow;
using CreditCardsSystem.Utility.Crypto;
using CreditCardsSystem.Utility.Extensions;
using Kfh.Aurora.Auth;
using Kfh.Aurora.Logging;
using Kfh.Aurora.Organization;
using Kfh.Aurora.Storage.Models;
using Kfh.Aurora.Workflow;
using Kfh.Aurora.Workflow.Dto;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using Telerik.DataSource.Extensions;
using FileInfo = Kfh.Aurora.Storage.Models.FileInfo;
using InstanceRunRequest = Kfh.Aurora.Workflow.Dto.InstanceRunRequest;

namespace CreditCardsSystem.Application.Workflow;

public class WorkflowAppService(IHttpClientFactory clientFactory,
IOrganizationClient organizationClient, IWorkflowClient workflowClient, IAuthManager authManager, IAuditLogger<WorkflowAppService> auditLogger,
ILogger<WorkflowAppService> logger) :
    BaseApiResponse, IWorkflowAppService, IAppService
{
    private readonly HttpClient _tasksClient = clientFactory.CreateClient("tasks");
    [HttpGet]
    public async Task<bool> HealthCheck() => true;


    [HttpPost]
    public async Task<CancelInstanceResponse?> CancelWorkFlow(Guid? instanceId, Guid? taskId) => await workflowClient.CancelWorkflowInstance(new()
    {
        InstanceId = instanceId,
        TaskId = taskId
    });


    [HttpPost]
    public async Task<InstanceRunResponse> CreateWorkFlow(WorkFlowMakerRequest request)
    {
        var kfhId = authManager.GetUser()!.KfhId;
        InstanceRunRequest instanceRunRequest = new()
        {
            WorkflowKey = $"{request.WorkFlowKey.GetDescription().Trim()}",
            InitiatingApplication = "creditCard",
            InitiatingUser = kfhId,
            Variables = new()
        };

        if (request.Variables is not null)
            instanceRunRequest.Variables.AddRange(request.Variables);

        var instanceRunResponse = await workflowClient.RunWorkflowInstance(instanceRunRequest!);
        auditLogger.Log.Information("{activity} ({InstanceId}) created for request activityId {requestId}:  ", instanceRunResponse.InstanceId.ToString(), WorkFlowKey.CardRequestWorkflow.ToString(), request.RequestActivityId);


        return instanceRunResponse;
    }


    [HttpPost]
    public async Task ReturnToMaker(ReturnToMakerRequest request)
    {
        CompleteTaskRequest taskRequest = new()
        {
            Assignee = authManager.GetUser()?.KfhId ?? "",
            TaskId = request.TaskId!.Value,
            InstanceId = request.InstanceId!.Value,
            Status = ActionType.Returned.ToString(),
            Payload = new()
            {
                { "Outcomes",new List<string>{ ActionType.Returned.ToString()} },
                { "Description", "Return" }
            },
            Comments = string.IsNullOrEmpty(request.Comments) ? null : new() { request.Comments }
        };

        await workflowClient.CompleteTask(taskRequest);

        auditLogger.Log.Information("{activity} ({InstanceId}) returned back to maker  :  ", WorkFlowKey.CardRequestWorkflow.ToString(), taskRequest.InstanceId.ToString());
    }

    [HttpPost]
    public async Task ReSubmitTask(ReturnToMakerRequest request)
    {
        CompleteTaskRequest taskRequest = new()
        {
            Assignee = authManager.GetUser()?.KfhId ?? "",
            TaskId = request.TaskId!.Value,
            InstanceId = request.InstanceId!.Value,
            Status = "ReSubmit",
            Payload = new()
            {
                { "Outcomes",new List<string>{ ActionType.ReSubmitted.ToString()} },
                { "Description", "ReSubmit" }
            },
            Comments = string.IsNullOrEmpty(request.Comments) ? null : new() { request.Comments }
        };

        await workflowClient.CompleteTask(taskRequest);

        auditLogger.Log.Information("{activity} ({InstanceId}) returned back to maker  :  ", WorkFlowKey.CardRequestWorkflow.ToString(), taskRequest.InstanceId.ToString());
    }


    [HttpPost]
    public async Task SubmitCorporateMakerRequest([FromBody] SubmitCorporateMakerInput input)
    {
        await AttachFiles(input.UploadFiles, input.InstanceId, input.TaskId, new CancellationToken());

        var kfhId = authManager.GetUser()!.KfhId;
        var request = new CompleteTaskRequest
        {
            TaskId = input.TaskId,
            Comments = new(),
            //Status = "Submitted",
            Payload = new()
            {
                { "InstallmentFromId", input.InstallmentFromId },
                { "InstallmentTypeId", input.InstallmentTypeId },
                { "RequestActivityId", input.RequestActivityId! },
                { "EmployeeId", Convert.ToInt32(kfhId) }
            }
        };

        await workflowClient.CompleteTask(request);
    }

    [HttpGet]
    public async Task<ApiResponseModel<List<TaskResult>>> GetUserTasks()
    {
        //if (!await workflowClient.HealthCheck())
        //    return Failure<List<TaskResult>>(GlobalResources.EnigmaAPiStatus);

        var kfhId = authManager.GetUser()!.KfhId;
        var response = await workflowClient.GetUserTasks(kfhId, "creditcard");

        return Success(response.Tasks);
    }



    [HttpPost]
    public async Task CompleteTask([FromBody] CompleteTaskRequest request, decimal? kfhId = null)
    {
        request.Assignee = kfhId?.ToString() ?? authManager.GetUser()!.KfhId;
        await workflowClient.CompleteTask(request);
    }

    [HttpPost]
    public async Task ClaimTask(Guid taskId)
    {
        var kfhId = authManager.GetUser()!.KfhId;
        await workflowClient.ClaimTask(new()
        {
            KfhId = kfhId,
            TaskId = taskId
        });
    }

    [HttpPost]
    public async Task UnclaimTask(Guid taskId)
    {
        var kfhId = authManager.GetUser()!.KfhId;
        await workflowClient.UnclaimTask(new()
        {
            TaskId = taskId
        });
    }

    [HttpPost]
    public async Task<List<FileInfo>?> GetAttachmentInfo(Guid instanceId)
    {
        var result = await workflowClient.GetAttachmentInfo(new()
        {
            InstanceId = instanceId
        }, new CancellationToken());

        return result?.Attachments;
    }

    [HttpPost]
    public async Task<string> DeleteAttachment(Guid instanceId, [FromBody] List<Guid> fileIds)
    {
        var kfhId = authManager.GetUser()!.KfhId;
        return string.Empty;

        //var result = await workflowClient.DeleteAttachments(new()
        //{
        //    InstanceId = instanceId,
        //    FileIds = fileIds,
        //    DeletedBy = kfhId
        //}, new CancellationToken());

        //return result?.Result;
    }

    [HttpPost]
    public async Task<Stream> DownloadAttachment(Guid fileId)
    {
        var result = await workflowClient.DownloadAttachment(fileId, new CancellationToken());
        return result;
    }

    [HttpPost]
    public async Task<GetCommentsResponse> GetComments(Guid instanceId)
    {
        var result = await workflowClient.GetComments(new()
        {
            InstanceId = instanceId
        });

        return result;
    }


    [HttpGet]
    public async Task<TaskResult> GetTaskById(Guid taskId)
    {
        bool canViewCardNumber = authManager.HasPermission(Permissions.CreditCardsNumber.View());

        var userTask = (await workflowClient.GetTasks())?.Tasks.FirstOrDefault(x => x.Id == taskId) ?? throw new ApiException(message: "invalid taskId");

        if (userTask.Payload.ContainsKey(WorkflowVariables.CardNumber))
        {
            string cardNumber = userTask.Payload.GetValueOrDefault(WorkflowVariables.CardNumber)?.ToString() ?? "";
            if (cardNumber.Length <= 16)
                userTask!.Payload[WorkflowVariables.CardNumber] = cardNumber.SaltThis();
        }

        return userTask;
    }





    private async Task AttachFiles(List<UploadFilesDTO> files, Guid instanceId, Guid taskId, CancellationToken c)
    {
        var kfhId = authManager.GetUser()!.KfhId;

        foreach (var file in files)
        {
            await workflowClient.AttachFile(new AttachFileRequest
            {
                TaskId = taskId,
                InstanceId = instanceId,
                UploadedBy = kfhId,
                File = FileParameters.Create(file.Name, file.FileData, $"{file.Name}{file.Extension}")
            }, c);
        }
    }





    [HttpGet]
    public async Task<List<CaseDto>?> GetCases()
    {
        //var isAuthorized = authManager.HasPermission("requests.view");
        //if (!isAuthorized)
        //{
        //    logger.LogError("User is not authorized");
        //    return [];
        //}

        var url = QueryHelpers.AddQueryString("/case/get-cases", "kfhId", authManager.GetUser()!.KfhId);

        var result = await _tasksClient.GetFromJsonAsync<GetCasesResponse>(url);

        var users = result?.Cases.Where(x => !string.IsNullOrEmpty(x.InitiatedBy)).Select(o => o.InitiatedBy).Distinct().ToList();

        if (users is not { Count: > 0 })
            return result?.Cases.Where(x => x.ClientId == "creditcard").ToList();

        var initiatingUser = await organizationClient.GetUsers(users);
        return result?.Cases.Where(x => x.ClientId == "creditcard").Select(o => new CaseDto
        {
            Source = o.Source,
            Id = o.Id,
            Title = o.Title,
            Description = o.Description,
            InitiatedBy = o.InitiatedBy,
            InitiatingUser = initiatingUser.FirstOrDefault(x => x.KfhId == o.InitiatedBy),
            Url = o.Url,
            CreatedOn = o.CreatedOn,
            UpdatedOn = o.UpdatedOn,
            ClientId = o.ClientId
        }).ToList();
    }

    [HttpGet]
    public async Task<CaseDetailDto?> GetCase(Guid instanceId)
    {
        //var isAuthorized = authManager.HasPermission("requests.view");
        //if (!isAuthorized)
        //{
        //    logger.LogError("User is not authorized");
        //    return null;
        //}

        var url = QueryHelpers.AddQueryString("/case/get-cases", "kfhId", authManager.GetUser()!.KfhId);

        var result = await _tasksClient.GetFromJsonAsync<GetCaseDetailResponse>(url);
        if (result == null)
        {
            return null;
        }

        var response = result.Cases.FirstOrDefault(x => x.Id == instanceId.ToString());
        return response ?? null;
    }

    [HttpGet]
    public async Task<List<GetTaskHistoryResponse>?> GetCaseDetail([FromQuery] Guid instanceId)
    {
        //var isAuthorized = authManager.HasPermission("requests.view");
        //if (!isAuthorized)
        //{
        //    logger.LogError("User is not authorized");
        //    return null;
        //}

        var details = await workflowClient.GetTaskHistory(new()
        {
            InstanceId = instanceId
        });


        var task = details.FirstOrDefault();
        //var requestBy = task.Payload["RequestedBy"];
        var requestUser = await organizationClient.GetUserDetails(task.Payload["RequestedBy"].ToString());
        task.Payload.Add("RequestedUserName", requestUser.Name.ToString());
        return details;
    }
}