using CreditCardsSystem.Domain.Interfaces;
using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.BCDPromotions.Groups;
using CreditCardsSystem.Domain.Models.BCDPromotions.Requests;
using Refit;
using static CreditCardsSystem.Domain.Models.BCDPromotions.Groups.GroupAttributeLookupDto;
using Attribute = CreditCardsSystem.Domain.Models.BCDPromotions.Groups.Attribute;

namespace CreditCardsSystem.Domain.Shared.Interfaces;

public interface IGroupsAppService : IRefitClient
{
    const string Controller = "/api/Groups/";

    [Get($"{Controller}{nameof(GetGroups)}")]
    Task<ApiResponseModel<List<PromotionGroupDto>>> GetGroups();

    [Get($"{Controller}{nameof(GetGroupsWithAttributes)}")]
    Task<ApiResponseModel<List<GroupAttributeDto>>> GetGroupsWithAttributes();

    [Get($"{Controller}{nameof(GetAttributesList)}")]
    Task<List<Attribute>> GetAttributesList();

    [Put($"{Controller}{nameof(UpdateLockStatus)}")]
    Task<ApiResponseModel<AddRequestResponse>> UpdateLockStatus(long id);

    [Get($"{Controller}{nameof(UpdateAttributeLockStatus)}")]
    Task<ApiResponseModel<AddRequestResponse>> UpdateAttributeLockStatus(long id);

    [Get($"{Controller}{nameof(GetAttributeLookupByType)}")]
    Task<List<GroupAttributeLookupDto>> GetAttributeLookupByType(GroupAttributeType type);

    [Get($"{Controller}{nameof(GetAllAttributesLookups)}")]
    Task<List<GroupAttributeLookupDto>> GetAllAttributesLookups();

    [Post($"{Controller}{nameof(ValidateGroupDesc)}")]
    Task<bool> ValidateGroupDesc([Body] PromotionGroupDto group);

    [Post($"{Controller}{nameof(AddGroupRequest)}")]
    Task<ApiResponseModel<AddRequestResponse>> AddGroupRequest([Body] RequestDto<PromotionGroupDto> request);

    [Post($"{Controller}{nameof(AddGroupAttributeRequest)}")]
    Task<ApiResponseModel<AddRequestResponse>> AddGroupAttributeRequest([Body] RequestDto<GroupAttributeDto> request);

    [Post($"{Controller}{nameof(ValidateGroupAttribute)}")]
    Task<bool> ValidateGroupAttribute([Body] GroupAttributeDto groupAttribute);

    [Get($"{Controller}{nameof(GetCustomerClass)}")]
    Task<List<GroupAttributeLookupDto>> GetCustomerClass();

    [Get($"{Controller}{nameof(GetLocations)}")]
    Task<List<GroupAttributeLookupDto>> GetLocations();
}