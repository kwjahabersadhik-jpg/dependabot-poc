using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.BCDPromotions.Requests;

namespace CreditCardsSystem.Domain.Shared.Interfaces
{
    public interface IRequestMaker<T>
    {
        Task<ApiResponseModel<AddRequestResponse>> AddRequest(RequestDto<T> request);
    }
}
