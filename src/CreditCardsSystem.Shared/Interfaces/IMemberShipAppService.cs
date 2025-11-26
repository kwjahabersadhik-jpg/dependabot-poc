using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.CardIssuance;
using CreditCardsSystem.Domain.Models.CardOperation;
using CreditCardsSystem.Domain.Models.CoBrand;
using CreditCardsSystem.Domain.Shared.Models.Membership;
using Refit;

namespace CreditCardsSystem.Domain.Interfaces;

public interface IMemberShipAppService : IRefitClient
{
    const string Controller = "/api/MemberShip/";


    [Post($"{Controller}{nameof(GetMemberShipDeleteRequests)}")]
    Task<ApiResponseModel<IEnumerable<MembershipDeleteRequestDto>>> GetMemberShipDeleteRequests(MemberShipDeleteRequestFilter request);

    [Post($"{Controller}{nameof(UpdateMemberShipDeleteRequests)}")]
    Task<ApiResponseModel<UpdateMembershipDeleteResponse>> UpdateMemberShipDeleteRequests(UpdateMembershipDeleteRequest request);

    [Get($"{Controller}{nameof(GetMemberships)}")]
    Task<ApiResponseModel<List<MemberShipInfoDto>>> GetMemberships(string? civilId, int? companyId);

    [Get($"{Controller}{nameof(GetMemberShipIdConflicts)}")]
    Task<ApiResponseModel<List<MemberShipInfoDto>>> GetMemberShipIdConflicts(string civilId, int companyId, string membershipId);

    [Post($"{Controller}{nameof(RequestDeleteMemberShip)}")]
    Task<ApiResponseModel<RequestingDeleteMemberShipResponse>> RequestDeleteMemberShip(RequestingDeleteMemberShipRequest request);

    [Delete($"{Controller}{nameof(DeleteMemberShip)}")]
    Task<ApiResponseModel<DeleteMemberShipResponse>> DeleteMemberShip(string civilId, int companyId);

    [Post($"{Controller}{nameof(CreateMemberShip)}")]
    Task<ApiResponseModel<CreateMemberShipResponse>> CreateMemberShip(MemberShipInfoDto request);

    [Post($"{Controller}{nameof(DeleteAndCreateMemberShipIfAny)}")]
    Task<ApiResponseModel<DeleteMemberShipResponse>> DeleteAndCreateMemberShipIfAny(string civilId, CoBrand coBrand);

    [Post($"{Controller}{nameof(DeleteAndCreateMemberShipIfAnyById)}")]
    Task<ApiResponseModel<DeleteMemberShipResponse>> DeleteAndCreateMemberShipIfAnyById(string civilId, CoBrand coBrand);

    [Post($"{Controller}{nameof(ProcessMembershipDeleteRequest)}")]
    Task<ApiResponseModel<ProcessResponse>> ProcessMembershipDeleteRequest([Body] ProcessMembershipDeleteRequest request);

    [Post($"{Controller}{nameof(GetMemberShipDeleteRequestById)}")]
    Task<ApiResponseModel<MembershipDeleteRequestDto>> GetMemberShipDeleteRequestById([Body] MemberShipDeleteRequestFilter request);
}
