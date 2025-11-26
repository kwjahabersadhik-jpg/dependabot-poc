using CreditCardsSystem.Data;
using CreditCardsSystem.Data.Models;
using CreditCardsSystem.Domain.Models.BCDPromotions.Groups;
using CreditCardsSystem.Domain.Models.BCDPromotions.Requests;
using CreditCardsSystem.Domain.Shared.Interfaces;
using CreditCardsSystem.Domain.Shared.Models.BCDPromotions.CardDefinition;
using Kfh.Aurora.Organization;
using System.Collections.Specialized;
using System.Text.RegularExpressions;
using ActivityForm = CreditCardsSystem.Domain.Shared.Enums.ActivityForm;
using RequestActivity = CreditCardsSystem.Domain.Shared.Entities.PromoEntities.RequestActivity;
using RequestActivityDetail = CreditCardsSystem.Domain.Shared.Entities.PromoEntities.RequestActivityDetail;
using RequestStatus = CreditCardsSystem.Domain.Shared.Enums.RequestStatus;

namespace CreditCardsSystem.Application.BCDPromotions.Requests
{
    public class RequestMaker<T>(FdrDBContext fdrDbContext,
            IHttpContextAccessor contextAccessor,
            IOrganizationClient organizationClient,
            IRequestsHelperMethods requestsHelperMethods)
        : IRequestMaker<T>
    {

        public async Task<ApiResponseModel<AddRequestResponse>> AddRequest([FromBody] RequestDto<T> request)
        {

            var requestId = await requestsHelperMethods.GetNewRequestId("promo.request_activity_seq");
            var requestDetails = await GetRequestDetailsListFromDataObject(request, requestId);
            var userId = contextAccessor.HttpContext?.User?.Claims.Single(x => x.Type == "sub").Value;
            var user = await organizationClient.GetUser(userId!);

            var requestHeader = new RequestActivity
            {
                RequestActivityId = requestId,
                ActivityStatusId = (int)RequestStatus.Pending,
                CreationDate = DateTime.Now,
                LastUpdateDate = DateTime.Now,
                MakerId = int.Parse(userId!),
                MakerName = user!.FirstName + " " + user.LastName,
                ActivityFormId = request.ActivityForm,
                ActivityTypeId = request.ActivityType,
                RequestActivityDetails = requestDetails
            };

            await fdrDbContext.PromoRequestActivities.AddAsync(requestHeader);
            await fdrDbContext.SaveChangesAsync();

            return new ApiResponseModel<AddRequestResponse>()
            {
                Data = new AddRequestResponse() { IsSaved = true },
                IsSuccess = true
            };
        }

        private async Task<List<RequestActivityDetail>> GetRequestDetailsListFromDataObject(RequestDto<T> request, long reqId)
        {
            var requestParams = new List<RequestActivityDetail>();
            var newDataAsCollection = GetCollectionFromObject(request.NewData!);

            if (request.ActivityForm == (int)ActivityForm.CardDef)
            {
                var i = 1;
                dynamic cardDef = request.NewData!;

                foreach (var extension in cardDef.CardDefExts)
                {
                    var extensionsDetails = GetCollectionFromObject(extension);
                    foreach (var item in extensionsDetails.AllKeys)
                    {
                        var key = item + "_ext_" + i;
                        var val = extensionsDetails[item];
                        newDataAsCollection.Add(key, val);
                    }
                    i++;
                }
            }

            newDataAsCollection["Title"] = request.Title;
            newDataAsCollection["Description"] = request.Description;

            foreach (string parameter in newDataAsCollection)
            {

                var requestDetailsId = await requestsHelperMethods.GetNewRequestId("promo.REQUEST_ACTIVITY_DETAILS_SEQ");

                requestParams.Add(new RequestActivityDetail
                {
                    Id = requestDetailsId,
                    RequestActivityId = reqId,
                    Parameter = parameter!,
                    Value = newDataAsCollection[parameter]!
                });

            }


            if (request.OldData != null!)
            {
                var oldDataAsCollection = GetCollectionFromObject(request.OldData);

                foreach (string parameter in oldDataAsCollection)
                {
                    var requestDetailsId = await requestsHelperMethods.GetNewRequestId("promo.REQUEST_ACTIVITY_DETAILS_SEQ");

                    requestParams.Add(new RequestActivityDetail
                    {
                        Id = requestDetailsId,
                        RequestActivityId = reqId,
                        Parameter = "Old_" + parameter!,
                        Value = oldDataAsCollection[parameter]!
                    });

                }
            }

            return requestParams;
        }

        private NameValueCollection GetCollectionFromObject(object obj)
        {
            string fieldName;
            string fieldValue;
            var details = new NameValueCollection();

            var fields = obj.GetType().GetFields();

            if (fields.Length != 0)
            {
                foreach (var field in fields)
                {
                    fieldName = field.Name;
                    fieldValue = string.Empty;

                    if (fieldName.Equals("IsLocked", StringComparison.OrdinalIgnoreCase)) continue;
                    if (fieldName.Equals(nameof(PostCardDefDto.CardDefExts), StringComparison.OrdinalIgnoreCase)) continue;
                    if (fieldName.Equals(nameof(CardDefExtDto.ExtensionId), StringComparison.OrdinalIgnoreCase)) continue;
                    if (fieldName.Equals(nameof(GroupAttributeDto.BackupAttributeId), StringComparison.OrdinalIgnoreCase)) continue;
                    if (fieldName.Equals(nameof(GroupAttributeDto.BackupGroupId), StringComparison.OrdinalIgnoreCase)) continue;

                    if (field.GetValue(obj) != null)
                    {
                        fieldValue = field.GetValue(obj)?.ToString() ?? "";
                        fieldValue = Regex.Replace(fieldValue, @"\s*,\s*", ",");
                        details.Add(fieldName, fieldValue);
                    }

                }
            }
            else
            {
                var properties = obj.GetType().GetProperties();

                foreach (var property in properties)
                {
                    fieldName = property.Name;
                    fieldValue = string.Empty;

                    if (fieldName.Equals("IsLocked", StringComparison.OrdinalIgnoreCase)) continue;
                    if (fieldName.Equals(nameof(CardDefinition.CardDefExts), StringComparison.OrdinalIgnoreCase)) continue;
                    if (fieldName.Equals(nameof(CardDefExtDto.ExtensionId), StringComparison.OrdinalIgnoreCase)) continue;
                    if (fieldName.Equals(nameof(GroupAttributeDto.BackupAttributeId), StringComparison.OrdinalIgnoreCase)) continue;
                    if (fieldName.Equals(nameof(GroupAttributeDto.BackupGroupId), StringComparison.OrdinalIgnoreCase)) continue;

                    if (property.GetValue(obj, null) != null)
                    {
                        fieldValue = property.GetValue(obj, null)?.ToString() ?? "";
                        fieldValue = Regex.Replace(fieldValue, @"\s*,\s*", ",");
                        details.Add(fieldName, fieldValue);
                    }
                }
            }

            return details;
        }


    }
}
