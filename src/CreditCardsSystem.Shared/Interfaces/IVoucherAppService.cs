using CreditCardsSystem.Domain.Shared.Models.Voucher;
using Refit;

namespace CreditCardsSystem.Domain.Interfaces;

public interface IVoucherAppService : IRefitClient
{
    const string Controller = "/api/Voucher/";

    [Post($"{Controller}{nameof(CreateVoucher)}")]
    Task<Models.ApiResponse<string>> CreateVoucher(VoucherRequest request);
}
