using CreditCardsSystem.Data;
using CreditCardsSystem.Data.Models;
using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Models.Access;
using CreditCardsSystem.Domain.Models.CardIssuance;
using CreditCardsSystem.Domain.Models.CardOperation;
using CreditCardsSystem.Domain.Models.CoBrand;
using CreditCardsSystem.Domain.Models.Workflow;
using CreditCardsSystem.Domain.Shared.Interfaces.Workflow;
using CreditCardsSystem.Domain.Shared.Models.Membership;
using CreditCardsSystem.Domain.Shared.Models.RequestActivity;
using CreditCardsSystem.Utility.Extensions;
using Kfh.Aurora.Auth;
using Microsoft.EntityFrameworkCore;

namespace CreditCardsSystem.Application.MemberShipService
{
    public class MemberShipAppService(FdrDBContext fdrDBContext, IAuthManager authManager, IRequestActivityAppService requestActivityAppService, IWorkflowAppService workflowAppService) : BaseApiResponse, IMemberShipAppService, IAppService
    {
        private readonly FdrDBContext _fdrDBContext = fdrDBContext;
        private readonly IAuthManager _authManager = authManager;
        private readonly IRequestActivityAppService _requestActivityAppService = requestActivityAppService;
        private readonly IWorkflowAppService _workflowAppService = workflowAppService;

        [HttpPost]
        public async Task<ApiResponseModel<MembershipDeleteRequestDto>> GetMemberShipDeleteRequestById([FromBody] MemberShipDeleteRequestFilter request)
        {

            var existingDeleteRequest = await (from dr in _fdrDBContext.MembershipDeleteRequests.AsNoTracking().Where(x => x.Id == request.Id)
                                               join cmp in _fdrDBContext.Companies.AsNoTracking() on dr.CompanyId equals cmp.CompanyId into drcmp
                                               from drc in drcmp.DefaultIfEmpty()
                                               let rp = _fdrDBContext.RequestParameters.AsNoTracking().FirstOrDefault(x => x.Parameter == "CLUB_MEMBERSHIP_ID" && x.Value == dr.ClubMembershipId)
                                               where (request.ClubMembershipId == null || dr.ClubMembershipId == request.ClubMembershipId) &&
                                                    (request.CompanyId == null || dr.CompanyId == request.CompanyId) &&
                                                    (request.CivilId == null || dr.CivilId == request.CivilId)
                                               select new MembershipDeleteRequestDto()
                                               {
                                                   RequestId = rp == null ? 0 : Convert.ToDecimal(rp.ReqId),
                                                   CivilId = dr.CivilId,
                                                   ClubMembershipId = dr.ClubMembershipId,
                                                   CompanyId = dr.CompanyId,
                                                   CompanyName = drc.CompanyName ?? dr.CompanyId.ToString(),
                                                   ApproveDate = dr.ApproveDate,
                                                   ApprovedBy = dr.ApprovedBy,
                                                   Id = dr.Id,
                                                   ApproverReason = dr.ApproverReason,
                                                   RejectDate = dr.RejectDate,
                                                   RequestDate = dr.RequestDate,
                                                   RequestorReason = dr.RequestorReason,
                                                   RequestedBy = dr.RequestedBy,
                                                   Status = dr.Status == null ? null : (DeleteRequestStatus)dr.Status
                                               }).Distinct().FirstOrDefaultAsync();


            return Success(existingDeleteRequest);
        }

        [HttpPost]
        public async Task<ApiResponseModel<IEnumerable<MembershipDeleteRequestDto>>> GetMemberShipDeleteRequests([FromBody] MemberShipDeleteRequestFilter request)
        {
            var existingDeleteRequest = (from dr in _fdrDBContext.MembershipDeleteRequests.AsNoTracking().Where(x => x.Status == 0)
                                         join cmp in _fdrDBContext.Companies.AsNoTracking() on dr.CompanyId equals cmp.CompanyId into drcmp
                                         from drc in drcmp.DefaultIfEmpty()
                                         let rp = _fdrDBContext.RequestParameters.AsNoTracking().FirstOrDefault(x => x.Parameter == "CLUB_MEMBERSHIP_ID" && x.Value == dr.ClubMembershipId)
                                         where (request.ClubMembershipId == null || dr.ClubMembershipId == request.ClubMembershipId) &&
                                              (request.CompanyId == null || dr.CompanyId == request.CompanyId) &&
                                              (request.CivilId == null || dr.CivilId == request.CivilId)
                                         select new MembershipDeleteRequestDto()
                                         {
                                             RequestId = rp == null ? 0 : Convert.ToDecimal(rp.ReqId),
                                             CivilId = dr.CivilId,
                                             ClubMembershipId = dr.ClubMembershipId,
                                             CompanyId = dr.CompanyId,
                                             CompanyName = drc.CompanyName ?? dr.CompanyId.ToString(),
                                             ApproveDate = dr.ApproveDate,
                                             ApprovedBy = dr.ApprovedBy,
                                             Id = dr.Id,
                                             ApproverReason = dr.ApproverReason,
                                             RejectDate = dr.RejectDate,
                                             RequestDate = dr.RequestDate,
                                             RequestorReason = dr.RequestorReason,
                                             RequestedBy = dr.RequestedBy,
                                             Status = dr.Status == null ? null : (DeleteRequestStatus)dr.Status
                                         }).Distinct();


            return Success(existingDeleteRequest.ProjectToType<MembershipDeleteRequestDto>().AsEnumerable());
        }

        [HttpPost]
        public async Task<ApiResponseModel<UpdateMembershipDeleteResponse>> UpdateMemberShipDeleteRequests([FromBody] UpdateMembershipDeleteRequest request)
        {
            List<UpdateMembershipDeleteDto> FailedItems = [];

            foreach (var deleteRequest in request.Items)
            {
                using var trans = await _fdrDBContext.Database.BeginTransactionAsync();

                try
                {
                    var deleteRequestRecord = await _fdrDBContext.MembershipDeleteRequests.FirstOrDefaultAsync(x => x.Id == deleteRequest.Id);

                    if (deleteRequest is null)
                        continue;

                    deleteRequestRecord!.Status = (int)deleteRequest.Status!;
                    deleteRequestRecord.ApproverReason = deleteRequest.ApproverReason;
                    deleteRequestRecord.ApprovedBy = Convert.ToInt32(_authManager.GetUser()?.KfhId);

                    if (deleteRequest.Status == Domain.Enums.DeleteRequestStatus.Approve)
                    {
                        deleteRequestRecord.ApproveDate = DateTime.Now;

                        //deleting membership info
                        var memberShipIfoToDelete = await _fdrDBContext.MembershipInfos.FirstAsync(x => x.ClubMembershipId == deleteRequestRecord.ClubMembershipId);
                        _fdrDBContext.MembershipInfos.Remove(memberShipIfoToDelete);

                        //removing membership info detail from request parameters on this request id
                        var requestParameters = await _fdrDBContext.RequestParameters.Where(x => x.ReqId == deleteRequest.RequestId).Where(x => x.Parameter == "CLUB_MEMBERSHIP_ID" || x.Parameter == "COMPANY_NAME" || x.Parameter == "CLUB_NAME").ToListAsync();
                        if (requestParameters.Count != 0)
                        {
                            _fdrDBContext.RequestParameters.RemoveRange(requestParameters);
                            foreach (var rp in requestParameters)
                            {
                                rp.Value = "-";
                            }
                            await _fdrDBContext.RequestParameters.AddRangeAsync(requestParameters);
                        }

                    }

                    if (deleteRequest.Status == Domain.Enums.DeleteRequestStatus.Reject)
                    {
                        deleteRequestRecord.RejectDate = DateTime.Now;
                    }

                    await _fdrDBContext.SaveChangesAsync();
                    await trans.CommitAsync();
                }
                catch (Exception ex)
                {
                    FailedItems.Add(deleteRequest);
                    await trans.RollbackAsync();
                }
            }

            return Success(new UpdateMembershipDeleteResponse() { FailedItems = FailedItems });
        }


        [HttpPost]
        public async Task<ApiResponseModel<ProcessResponse>> ProcessMembershipDeleteRequest([FromBody] ProcessMembershipDeleteRequest request)
        {
            bool isAuthorized = authManager.HasPermission(Permissions.MemberShipDeleteRequest.EnigmaApprove());
            if (!isAuthorized)
                return Failure<ProcessResponse>(GlobalResources.NotAuthorized);

            var taskDetail = await _workflowAppService.GetTaskById(request.TaskId!.Value);
            _ = long.TryParse(taskDetail!.Payload.GetValueOrDefault(WorkflowVariables.MemberShipDeleteRequestId)?.ToString(), out long memberShipDeleteRequestId);

            var membershipDeleteRequest = await _fdrDBContext.MembershipDeleteRequests.FirstOrDefaultAsync(x => x.Id == memberShipDeleteRequestId);
            if (membershipDeleteRequest is null)
                return Failure<ProcessResponse>(message: "Invalid request");

            request.CardNumber = request.CardNumber.Masked(6, 6);
            request.Activity = CFUActivity.MemberShipDeleteRequest;

            if (request.ActionType == ActionType.Rejected)
            {
                membershipDeleteRequest.RejectDate = DateTime.Now;
                await _fdrDBContext.SaveChangesAsync();

                await _requestActivityAppService.CompleteActivity(request);
                return Success(new ProcessResponse() { CardNumber = "" }, message: "Membership delete request rejected successfully");
            }


            return await Approve();

            #region local functions
            async Task<ApiResponseModel<ProcessResponse>> Approve()
            {
                _ = int.TryParse(taskDetail!.Payload.GetValueOrDefault(WorkflowVariables.CompanyId)?.ToString(), out int companyId);

                string civilId = taskDetail!.Payload.GetValueOrDefault(WorkflowVariables.CivilId)?.ToString() ?? "";
                string clubMembershipId = taskDetail!.Payload.GetValueOrDefault(WorkflowVariables.ClubMembershipId)?.ToString() ?? "";


                membershipDeleteRequest.Status = (int)DeleteRequestStatus.Approve;
                membershipDeleteRequest.ApproverReason = request.ApproverReason;
                membershipDeleteRequest.ApprovedBy = Convert.ToInt32(_authManager.GetUser()?.KfhId);
                membershipDeleteRequest.ApproveDate = DateTime.Now;


                //deleting membership info
                var memberShipIfoToDelete = await _fdrDBContext.MembershipInfos.FirstOrDefaultAsync(x => x.ClubMembershipId == membershipDeleteRequest.ClubMembershipId);
                if (memberShipIfoToDelete is null)
                {
                    await _requestActivityAppService.CompleteActivity(request);
                    return Success(new ProcessResponse() { CardNumber = "" }, message: "Membership info has been deleted successfully");
                }

                _fdrDBContext.MembershipInfos.Remove(memberShipIfoToDelete);


                var relatedRequestId = _fdrDBContext.RequestParameters.AsNoTracking().FirstOrDefault(x => x.Parameter == "CLUB_MEMBERSHIP_ID" && x.Value == membershipDeleteRequest.ClubMembershipId)?.ReqId;

                //removing membership info detail from request parameters on this request id
                var requestParameters = await _fdrDBContext.RequestParameters.Where(x => x.ReqId == relatedRequestId).Where(x => x.Parameter == "CLUB_MEMBERSHIP_ID" || x.Parameter == "COMPANY_NAME" || x.Parameter == "CLUB_NAME").ToListAsync();
                if (requestParameters.Count != 0)
                {
                    _fdrDBContext.RequestParameters.RemoveRange(requestParameters);
                    foreach (var rp in requestParameters)
                    {
                        rp.Value = "-";
                    }
                    await _fdrDBContext.RequestParameters.AddRangeAsync(requestParameters);
                }

                await _fdrDBContext.SaveChangesAsync();

                await _requestActivityAppService.CompleteActivity(request);
                return Success(new ProcessResponse() { CardNumber = "" }, message: "Membership delete request approved successfully!");
            }
            #endregion
        }

        [HttpGet]
        public async Task<ApiResponseModel<List<MemberShipInfoDto>>> GetMemberships(string? civilId, int? companyId)
        {
            var response = new ApiResponseModel<List<MemberShipInfoDto>>();

            var memberships = await _fdrDBContext.MembershipInfos.AsNoTracking().Where(mi => (civilId == null || mi.CivilId == civilId) &&
            (companyId == null || mi.CompanyId == companyId)).ProjectToType<MemberShipInfoDto>().OrderByDescending(x => x.DateCreated).ToListAsync();

            return response.Success(memberships);
        }


        [HttpGet]
        public async Task<ApiResponseModel<List<MemberShipInfoDto>>> GetMemberShipIdConflicts(string civilId, int companyId, string membershipId)
        {
            var response = new ApiResponseModel<List<MemberShipInfoDto>>();

            var membershipIds = await _fdrDBContext.MembershipInfos.AsNoTracking().Where(mi => mi.CivilId != civilId && mi.CompanyId == companyId && mi.ClubMembershipId == membershipId)
                .ProjectToType<MemberShipInfoDto>().OrderByDescending(x => x.DateCreated).ToListAsync();

            return response.Success(membershipIds);
        }





        [HttpPost]
        public async Task<ApiResponseModel<RequestingDeleteMemberShipResponse>> RequestDeleteMemberShip([FromBody] RequestingDeleteMemberShipRequest request)
        {
            var response = new ApiResponseModel<RequestingDeleteMemberShipResponse>();

            if (request.CompanyId <= 0 || string.IsNullOrEmpty(request.ClubMembershipId))
                return response.Fail(message: "Please check Company Id or ClubMembershipId");

            var existingDeleteRequest = await _fdrDBContext.MembershipDeleteRequests.FirstOrDefaultAsync(x => x.Status == 0 && x.ClubMembershipId == request.ClubMembershipId && x.CompanyId == request.CompanyId);

            if (existingDeleteRequest != null)
                return request.ReturnExistingId ?
                    response.Success(new() { Id = existingDeleteRequest.Id }) :
                    response.Fail($"Delete request is already sent on {existingDeleteRequest.RequestDate!.Value.Formed()} , please try again later");

            var newId = _fdrDBContext.MembershipDeleteRequests.Max(x => x.Id) + 1;
            request.Id = newId;
            var newDeleteRequest = request.Adapt<MembershipDeleteRequest>();
            await _fdrDBContext.MembershipDeleteRequests.AddAsync(newDeleteRequest);
            await _fdrDBContext.SaveChangesAsync();

            await LogRequestActivity(newDeleteRequest);
            //TODO : SendEmail
            return response.Success(new() { Id = newDeleteRequest.Id }, "Delete request created successfully, wait for the approval email to create card");
        }

        async Task LogRequestActivity(MembershipDeleteRequest request)
        {
            var requestActivityDto = new RequestActivityDto()
            {
                CivilId = request.CivilId,
                RequestActivityStatusId = (int)RequestActivityStatus.Pending,
                CfuActivityId = (int)CFUActivity.MemberShipDeleteRequest,
                WorkflowVariables = new() {
                { "Description", $"Request deleting membership for {request.CivilId}"},
                { WorkflowVariables.CivilId, request.CivilId },
                { WorkflowVariables.MemberShipDeleteRequestId, request.Id},
                { WorkflowVariables.ClubMembershipId, request.ClubMembershipId.ToString()},
                { WorkflowVariables.CompanyId, request.CompanyId.ToString()},
                { WorkflowVariables.NoActivityRequest, true.ToString()}
                }
            };

            await _requestActivityAppService.LogRequestActivity(requestActivityDto, isNeedWorkflow: true, onlyWorkflow: true);
        }

        [NonAction]
        [HttpDelete]
        public async Task<ApiResponseModel<DeleteMemberShipResponse>> DeleteMemberShip(string civilId, int companyId)
        {
            var response = new ApiResponseModel<DeleteMemberShipResponse>();

            var existingMemberShip = (await _fdrDBContext.MembershipInfos.FirstOrDefaultAsync(mi => mi.CivilId == civilId && mi.CompanyId == companyId))
                ?? throw new ApiException(message: "Unable to find record to delete");

            _fdrDBContext.MembershipInfos.Remove(existingMemberShip);
            await _fdrDBContext.SaveChangesAsync();
            return response.Success(new() { MemberShipId = existingMemberShip.ClubMembershipId.ToString() }, message: "Successfully deleted");
        }

        [NonAction]
        [HttpDelete]
        public async Task<ApiResponseModel<DeleteMemberShipResponse>> DeleteMemberShipById(string civilId, string membershipId)
        {
            var response = new ApiResponseModel<DeleteMemberShipResponse>();

            var existingMemberShip = (await _fdrDBContext.MembershipInfos.FirstOrDefaultAsync(mi => mi.CivilId == civilId && mi.ClubMembershipId == membershipId))
                ?? throw new ApiException(message: "Unable to find record to delete");

            _fdrDBContext.MembershipInfos.Remove(existingMemberShip);
            await _fdrDBContext.SaveChangesAsync();
            return response.Success(new() { MemberShipId = existingMemberShip.ClubMembershipId.ToString() }, message: "Successfully deleted");


        }

        [NonAction]
        [HttpPost]
        public async Task<ApiResponseModel<CreateMemberShipResponse>> CreateMemberShip(MemberShipInfoDto request)
        {
            var response = new ApiResponseModel<CreateMemberShipResponse>();

            var newMemberShip = await _fdrDBContext.MembershipInfos.AddAsync(request.Adapt<MembershipInfo>());
            await _fdrDBContext.SaveChangesAsync();

            return response.Success(new() { MemberShipId = newMemberShip.Entity.ClubMembershipId }, message: "Successfully Created");
        }


        [NonAction]
        [HttpPost]
        public async Task<ApiResponseModel<DeleteMemberShipResponse>> DeleteAndCreateMemberShipIfAny(string civilId, CoBrand coBrand)
        {
            MemberShipInfoDto newMemberShipInfo = new()
            {
                CivilId = civilId!,
                CompanyId = coBrand.Company.CompanyId,
                FileName = $"SSO_{DateTime.Now.Formed(ConfigurationBase.AccountOnBoardingDateFormat)}",
                DateCreated = DateTime.Now,
                ClubMembershipId = coBrand.MemberShipId.ToString()
            };

            var existingMemberShip = await GetMemberships(newMemberShipInfo.CivilId, newMemberShipInfo.CompanyId);
            var recentMemberShip = existingMemberShip.Data?.FirstOrDefault();

            if (existingMemberShip.IsSuccess && recentMemberShip is not null)
            {
                newMemberShipInfo.FileName = recentMemberShip.FileName;
                newMemberShipInfo.DateCreated = recentMemberShip.DateCreated ?? DateTime.Now;
                newMemberShipInfo.DateUpdated = DateTime.Now;
                await DeleteMemberShip(recentMemberShip.CivilId, recentMemberShip.CompanyId);
            }

            await CreateMemberShip(newMemberShipInfo);

            return new() { IsSuccess = true };
        }


        [NonAction]
        [HttpPost]
        public async Task<ApiResponseModel<DeleteMemberShipResponse>> DeleteAndCreateMemberShipIfAnyById(string civilId, CoBrand coBrand)
        {
            if (coBrand?.MemberShipId is not null)
                await DeleteMemberShipById(civilId, coBrand.MemberShipId.ToString()!);

            MemberShipInfoDto newMemberShipInfo = new()
            {
                CivilId = civilId!,
                CompanyId = coBrand.Company.CompanyId,
                FileName = $"SSO_{DateTime.Now.Formed(ConfigurationBase.AccountOnBoardingDateFormat)}",
                DateCreated = DateTime.Now,
                ClubMembershipId = coBrand.NewMemberShipId.ToString()!, //New membership Id
            };

            await CreateMemberShip(newMemberShipInfo);

            return new() { IsSuccess = true };
        }
    }
}
