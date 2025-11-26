using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.Migs;
using Refit;

namespace CreditCardsSystem.Domain.Interfaces;

public interface IMigsAppService : IRefitClient
{
    const string Controller = "/api/Migs/";

    [Get($"{Controller}{nameof(GetLoadIds)}")]
    Task<ApiResponseModel<IEnumerable<MasterDto>>> GetLoadIds(string loadDate);

    [Post($"{Controller}{nameof(GenerateFile)}")]
    Task<ApiResponseModel> GenerateFile(GenerateFileRequestDto request);

    [Get($"{Controller}{nameof(GetMasterDataById)}")]
    Task<MasterDto?> GetMasterDataById(int loadId);
}