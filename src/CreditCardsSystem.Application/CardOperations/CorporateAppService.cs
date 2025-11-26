using BankingCustomerProfileReference;
using CorporateCreditCardServiceReference;
using CreditCardsSystem.Data;
using CreditCardsSystem.Data.Models;
using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Models.Access;
using CreditCardsSystem.Domain.Models.CardOperation;
using CreditCardsSystem.Domain.Models.Workflow;
using CreditCardsSystem.Domain.Shared.Models.Reports;
using CreditCardsSystem.Domain.Shared.Models.RequestActivity;
using CreditCardsSystem.Utility.Extensions;
using Kfh.Aurora.Auth;
using Kfh.Aurora.Integration;
using Kfh.Aurora.Logging;
using Newtonsoft.Json;

namespace CreditCardsSystem.Application.CardOperations
{
    public class CorporateAppService : BaseApiResponse, IAppService, ICorporateAppService
    {
        private readonly IAuthManager _authManager;
        private readonly FdrDBContext _fdrDBContext;
        private readonly ICorporateProfileAppService _corporateProfileAppService;
        private readonly IAuditLogger<CorporateAppService> _auditLogger;
        private readonly IRequestActivityAppService _requestActivityAppService;
        private readonly CorporateCreditCardServiceClient corporateCreditCardServiceClient;
        private readonly BankingCustomerProfileServiceClient customerProfileServiceClient;

        public CorporateAppService(IIntegrationUtility integrationUtility, IAuthManager authManager, IOptions<IntegrationOptions> options, FdrDBContext fdrDBContext, IRequestActivityAppService requestActivityAppService, ICorporateProfileAppService corporateProfileAppService, IAuditLogger<CorporateAppService> auditLogger)
        {
            corporateCreditCardServiceClient = integrationUtility.GetClient<CorporateCreditCardServiceClient>(options.Value.Client, options.Value.Endpoints.CorporateCreditCard, options.Value.BypassSslValidation);
            customerProfileServiceClient = integrationUtility.GetClient<BankingCustomerProfileServiceClient>(options.Value.Client, options.Value.Endpoints.BankingCustomerProfile, options.Value.BypassSslValidation);

            _authManager = authManager;
            _fdrDBContext = fdrDBContext;
            _corporateProfileAppService = corporateProfileAppService;
            _auditLogger = auditLogger;
            _requestActivityAppService = requestActivityAppService;
        }

        [HttpPost]
        public async Task<ApiResponseModel<CorporateProfileDto>> RequestAddProfile([FromBody] CorporateProfileDto profile)
        {
            //Needs to move
            if (!_authManager.HasPermission(Permissions.CorporateProfile.Create()))
                return Failure<CorporateProfileDto>(GlobalResources.UnAuthorized);

            await Validate(profile);
            //TODO: create workflow 
            await _requestActivityAppService.LogRequestActivity(new()
            {
                CivilId = profile.CorporateCivilId,
                CustomerName = profile.EmbossingName,
                IssuanceTypeId = (int)IssuanceTypes.OTHERS,
                RequestActivityStatusId = (int)RequestActivityStatus.New,
                CfuActivityId = (int)CFUActivity.CorporateProfileAdd,
                Details = new()
                    {
                        {DetailKey.CIVIL_ID,  profile.CorporateCivilId} ,
                        {DetailKey.NAME_EN,  profile.CorporateNameEn },
                        {DetailKey.NAME_AR,  profile.CorporateNameAr },
                        {DetailKey.EMBOSSING_NAME,  profile.EmbossingName },
                        {DetailKey.RIM_CODE,  profile.RimCode.ToString() },
                        {DetailKey.CLASS_NAME,  profile.CustomerClass },
                        {DetailKey.GLOBAL_LIMIT,  profile.GlobalLimit.ToString() },
                        {DetailKey.RELATIONSHIP_NO, string.Empty },
                        {DetailKey.KFH_ACCOUNT_NO,  profile.KfhAccountNo??"" },
                        {DetailKey.CUSTOMER_NO,  string.Empty },
                        {DetailKey.BILLING_ACCOUNT_NO,  string.Empty},
                        {DetailKey.ADDRESSLINE1,  profile.AddressLine1??"" },
                        {DetailKey.ADDRESSLINE2,  profile.AddressLine2??"" },
                    },
                WorkflowVariables = new() {
                    { "Description", $"Corporate profile created for civil id {profile.CorporateCivilId}" },
                       {WorkflowVariables.CorporateCivilId,  profile.CorporateCivilId} ,
                        {WorkflowVariables.NameEn,  profile.CorporateNameEn },
                        {WorkflowVariables.NameAr,  profile.CorporateNameAr },
                        {WorkflowVariables.EmbossingName,  profile.EmbossingName },
                        {WorkflowVariables.RimCode,  profile.RimCode.ToString() },
                        {WorkflowVariables.ClassName,  profile.CustomerClass },
                        {WorkflowVariables.GlobalLimit,  profile.GlobalLimit.ToString() },
                        {WorkflowVariables.RelationShipNumber, string.Empty },
                        {WorkflowVariables.KFHAccountNumber,  profile.KfhAccountNo??"" },
                        {WorkflowVariables.CUSTOMERNO,  string.Empty },
                        {WorkflowVariables.BillingAccountNumber,  string.Empty},
                        {WorkflowVariables.AddressLine1,  profile.AddressLine1??"" },
                        {WorkflowVariables.AddressLine2,  profile.AddressLine2??"" },
                }
            }, isNeedWorkflow: true);

            return Success<CorporateProfileDto>(new(), message: GlobalResources.WaitingForApproval);
        }



        [HttpPost]
        public async Task<ApiResponseModel<CorporateProfileDto>> RequestUpdateProfile([FromBody] CorporateProfileDto profile)
        {
            //TODO: use attribute
            if (!_authManager.HasPermission(Permissions.CorporateProfile.Edit()))
                return Failure<CorporateProfileDto>(GlobalResources.UnAuthorized);

            await Validate(profile);

            await _requestActivityAppService.LogRequestActivity(new()
            {
                CivilId = profile.CorporateCivilId,
                CustomerName = profile.EmbossingName,
                IssuanceTypeId = (int)IssuanceTypes.OTHERS,
                RequestActivityStatusId = (int)RequestActivityStatus.New,
                CfuActivityId = (int)CFUActivity.CorporateProfileUpdate,
                Details = new()
                    {
                        {DetailKey.CIVIL_ID,  profile.CorporateCivilId} ,
                        {DetailKey.NAME_EN,  profile.CorporateNameEn },
                        {DetailKey.NAME_AR,  profile.CorporateNameAr },
                        {DetailKey.RIM_CODE,  profile.RimCode.ToString() },
                        {DetailKey.CLASS_NAME,  profile.CustomerClass },
                        {DetailKey.RELATIONSHIP_NO, string.Empty },

                        {DetailKey.OLD_EMBOSSING_NAME,  profile.EmbossingName },
                        {DetailKey.EMBOSSING_NAME,  profile.EmbossingName },

                        {DetailKey.OLD_GLOBAL_LIMIT,  profile.GlobalLimit.ToString() },
                        {DetailKey.GLOBAL_LIMIT,  profile.GlobalLimit.ToString() },

                        {DetailKey.KFH_ACCOUNT_NO,  profile.KfhAccountNo??"" },
                        {DetailKey.CUSTOMER_NO,  string.Empty },
                        {DetailKey.BILLING_ACCOUNT_NO,  string.Empty},
                        {DetailKey.ADDRESSLINE1,  profile.AddressLine1??"" },
                        {DetailKey.ADDRESSLINE2,  profile.AddressLine2??"" },
                    },
                WorkflowVariables = new() {
                        { "Description", $"Corporate profile updated for civil id {profile.CorporateCivilId}" } ,
                        {WorkflowVariables.CorporateCivilId,  profile.CorporateCivilId} ,
                        {WorkflowVariables.NameEn,  profile.CorporateNameEn },
                        {WorkflowVariables.NameAr,  profile.CorporateNameAr },
                        {WorkflowVariables.RimCode,  profile.RimCode.ToString() },
                        {WorkflowVariables.ClassName,  profile.CustomerClass },
                        {WorkflowVariables.RelationShipNumber, string.Empty },
                        {WorkflowVariables.OldEmbossingName,  profile.EmbossingName },
                        {WorkflowVariables.EmbossingName,  profile.EmbossingName },
                        {WorkflowVariables.OldGlobalLimit,  profile.GlobalLimit.ToString() },
                        {WorkflowVariables.GlobalLimit,  profile.GlobalLimit.ToString() },
                        {WorkflowVariables.KFHAccountNumber,  profile.KfhAccountNo??"" },
                        {WorkflowVariables.CUSTOMERNO,  string.Empty },
                        {WorkflowVariables.BillingAccountNumber,  string.Empty},
                        {WorkflowVariables.AddressLine1,  profile.AddressLine1??"" },
                        {WorkflowVariables.AddressLine2,  profile.AddressLine2??"" },
                },
            }, isNeedWorkflow: true);



            return Success<CorporateProfileDto>(new(), message: GlobalResources.WaitingForApproval);
        }

        [HttpPost]
        public async Task<ApiResponseModel<ProcessResponse>> ProcessProfileRequest([FromBody] ProcessCorporateProfileRequest request)
        {
            if (!_authManager.HasPermission(Permissions.CorporateProfile.EnigmaApprove()))
                return Failure<ProcessResponse>(GlobalResources.UnAuthorized);

            var requestActivity = (await _requestActivityAppService.GetRequestActivityById(request.RequestActivityId)).Data ?? throw new ApiException(message: "Unable to find Request");

            string log = $"{(request.ActionType == ActionType.Approved ? "Approving" : "Rejecting")} {requestActivity.CfuActivity.GetDescription()} for corporate civil id {requestActivity.CivilId}";
            StringBuilder logParameters = new();
            request.CardNumber = request.CardNumber.Masked(6, 6);
            request.Activity = requestActivity.CfuActivity;

            if (request.ActionType == ActionType.Rejected)
                return await Reject();

            return await Approve();


            #region local functions

            async Task<ApiResponseModel<ProcessResponse>> Reject()
            {

                requestActivity.RequestActivityStatusId = (int)RequestActivityStatus.Rejected;
                requestActivity.ReasonForRejection = request.ReasonForRejection;
                //await UpdateRequestActivity(requestActivity);
                await _requestActivityAppService.CompleteActivity(request);
                return Success<ProcessResponse>(new(), message: "Done!");

            }

            async Task<ApiResponseModel<ProcessResponse>> Approve()
            {
                await ApprovalValidation();

                _ = decimal.TryParse(requestActivity.GetValue(DetailKey.RIM_CODE), out decimal _rimcode);

                var profileEntity = new CorporateProfileDto()
                {
                    CorporateCivilId = requestActivity.GetValue(DetailKey.CIVIL_ID),
                    CorporateNameEn = requestActivity.GetValue(DetailKey.NAME_EN),
                    CorporateNameAr = requestActivity.GetValue(DetailKey.NAME_AR),
                    RimCode = _rimcode,
                    EmbossingName = requestActivity.GetValue(DetailKey.EMBOSSING_NAME),
                    RelationshipNo = requestActivity.GetValue(DetailKey.RELATIONSHIP_NO),
                    KfhAccountNo = requestActivity.GetValue(DetailKey.KFH_ACCOUNT_NO),
                    BillingAccountNo = requestActivity.GetValue(DetailKey.BILLING_ACCOUNT_NO),
                    CustomerNo = requestActivity.GetValue(DetailKey.CUSTOMER_NO),
                    CustomerClass = requestActivity.GetValue(DetailKey.CLASS_NAME),
                    // global limit is no more needed in our database corporate_profile table.
                    AddressLine1 = requestActivity.GetValue(DetailKey.ADDRESSLINE1),
                    AddressLine2 = requestActivity.GetValue(DetailKey.ADDRESSLINE2),
                };
                await profileEntity.ModelValidationAsync();

                if (requestActivity.CfuActivity is CFUActivity.CorporateProfileAdd)
                    await ApproveProfileAddRequest();

                if (requestActivity.CfuActivity is CFUActivity.CorporateProfileUpdate)
                    await ApproveProfileUpdateRequest();

                _auditLogger.Log.Error("Success to {log} {parameters}", log, logParameters.ToString());

                //TODO: refactor like requestActivity.Approve(), requestActivity.Reject();
                requestActivity.RequestActivityStatusId = (int)RequestActivityStatus.Approved;
                //await UpdateRequestActivity(requestActivity);
                await _requestActivityAppService.CompleteActivity(request);
                return Success(new ProcessResponse(), message: GlobalResources.SuccessApproval);

                #region local functions
                async Task ApproveProfileAddRequest()
                {
                    var profileInPhenix = await customerProfileServiceClient.viewCustomerProfileAsync(new() { civilID = profileEntity.CorporateCivilId });
                    var profileOnboardResponse = await corporateCreditCardServiceClient.performCorporateProfileBoardingAsync(new()
                    {
                        corporateProfileRequest = new()
                        {
                            corporateCivilID = profileEntity.CorporateCivilId,
                            corporateNameEn = profileEntity.CorporateNameEn,
                            corporateNameAr = profileEntity.CorporateNameAr,
                            embossingName = profileEntity.EmbossingName,
                            rimCode = (long)_rimcode,
                            kfhAccountNumber = profileEntity.KfhAccountNo,
                            addressLine1 = profileEntity.AddressLine1,
                            addressLine2 = profileEntity.AddressLine1,
                            countryCode = profileInPhenix != null ? profileInPhenix.viewCustomerProfileResult.country_code.ToString().Trim() : "",
                            organization = 786
                        }
                    });
                    var onboardResult = profileOnboardResponse.createCorporateProfile;
                    logParameters.Append($"relationshipNumber:{onboardResult.relationshipNumber}, ");
                    logParameters.Append($"billingAcctNumber:{onboardResult.billingAcctNumber}, ");
                    logParameters.Append($"customerNumber:{onboardResult.customerNumber}, ");
                    logParameters.Append($"description:{onboardResult.description}, ");
                    logParameters.Append($"status:{onboardResult.status}, ");
                    string jsonLog = JsonConvert.SerializeObject(onboardResult);
                    if (onboardResult.status == enumStatus.FAILED)
                    {
                        if (onboardResult.description.ToLower().Contains("limit is not defined in ibs"))
                            throw new ApiException(message: GlobalResources.CorpLimitNotDefinedInIBS);

                        try
                        {
                            _auditLogger.Log.Error("Failed to {log} {parameters}", log, jsonLog);
                        }
                        catch (System.Exception)
                        {
                            _auditLogger.Log.Error("Failed to {log} {parameters}", log, logParameters.ToString());
                        }
                        throw new ApiException(message: onboardResult.description);
                    }


                    //await _fdrDBContext.CorporateProfiles.AddAsync(profileEntity.Adapt<Data.Models.CorporateProfile>());
                    //await _fdrDBContext.SaveChangesAsync();
                }

                async Task ApproveProfileUpdateRequest()
                {
                    //var updateResponse = await corporateCreditCardServiceClient.updateCorporateProfileAsync(new()
                    //{
                    //    corpCivilID = profileEntity.CorporateCivilId,
                    //    embossingName = profileEntity.EmbossingName,
                    //    organization = 786
                    //});

                    //var updateResult = updateResponse.updateCorporateProfile;
                    //logParameters.Append($"description:{updateResult.description}, ");
                    //logParameters.Append($"status:{updateResult.status}, ");
                    //string jsonLog = JsonConvert.SerializeObject(updateResult);
                    //if (!updateResult.isSuccessful)
                    //{
                    //    try
                    //    {
                    //        _auditLogger.Log.Error("Failed to {log} {parameters}", log, jsonLog);
                    //    }
                    //    catch (System.Exception)
                    //    {
                    //        _auditLogger.Log.Error("Failed to {log} {parameters}", log, logParameters.ToString());
                    //    }

                    //    throw new ApiException(message: updateResult.description);
                    //}


                    _fdrDBContext.CorporateProfiles.Update(profileEntity.Adapt<Data.Models.CorporateProfile>());
                    await _fdrDBContext.SaveChangesAsync();
                }

                async Task ApprovalValidation()
                {
                    //TODO: Uncomment if we need to validate
                    //var globalLimit = await _corporateProfileAppService.GetAndValidateGlobalLimit(requestActivity.CivilId);
                    //await _corporateProfileAppService.IsExpired(globalLimit);
                    await Task.CompletedTask;
                }
                #endregion
            }

            #endregion
        }

        async Task CheckingPendingRequestActivity(string civilId)
        {
            var isHavingPendingActivity = (await _requestActivityAppService.GetAllRequestActivity(new()
            {
                CustomerCivilId = civilId,
                CFUActivities = new() { CFUActivity.CorporateProfileAdd, CFUActivity.CorporateProfileUpdate },
                Status = RequestActivityStatus.New
            }))?.Data?.Any() ?? false;

            if (isHavingPendingActivity)
                throw new ApiException(message: GlobalResources.RequestAlreadySent);
        }

        async Task<ApiResponseModel<ProcessResponse>> UpdateRequestActivity(RequestActivityDto requestActivity)
        {
            await _requestActivityAppService.UpdateRequestActivityStatus(requestActivity);
            return Success(new ProcessResponse() { CardNumber = "" }, message: "Done!");
        }
        async Task Validate(CorporateProfileDto profile)
        {

            await profile.ModelValidationAsync();
            await CheckingPendingRequestActivity(profile.CorporateCivilId);
            var globalLimit = await _corporateProfileAppService.GetAndValidateGlobalLimit(profile.CorporateCivilId);
            await _corporateProfileAppService.IsExpired(globalLimit);
        }
    }
}
