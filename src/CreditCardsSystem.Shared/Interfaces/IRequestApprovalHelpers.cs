using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.BCDPromotions.Requests;

namespace CreditCardsSystem.Domain.Interfaces;

public interface IRequestApprovalHelpers
{
    Task<ApiResponseModel<object>> Approve(ApproveRequestDto approveRequestDto);

    void SetFields(dynamic fdrDbContext, IRequestsHelperMethods requestsHelperMethods);
}