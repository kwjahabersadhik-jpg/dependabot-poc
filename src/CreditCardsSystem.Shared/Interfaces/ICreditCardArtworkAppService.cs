using CreditCardsSystem.Domain.Entities.Admin;
using CreditCardsSystem.Domain.Models;
using CreditCardsSystem.Domain.Models.Card;
using CreditCardsSystem.Domain.Models.Reports;
using Refit;

namespace CreditCardsSystem.Domain.Interfaces
{
    public interface ICreditCardArtworkAppService : IRefitClient
    {
        const string Controller = "/api/CreditCardArtwork/";
        Task<CardType?> GetCardImage(string type);
        Task<IEnumerable<CardImage?>> GetAllCardImages();

        [Get($"{Controller}{nameof(GetCreditCardType)}")]
        Task<CardTypeDto?> GetCreditCardType([Query] Guid id);

        [Get($"{Controller}{nameof(GetCreditCardsTypes)}")]
        Task<List<CardTypeDto>> GetCreditCardsTypes();

        [Post($"{Controller}{nameof(UpdateCreditCardType)}")]
        Task UpdateCreditCardType([Body] CardTypeDto CreditCard);

        [Post($"{Controller}{nameof(DeleteCreditCardType)}")]
        Task DeleteCreditCardType([Query] Guid id);

        [Multipart]
        [Post($"{Controller}{nameof(UploadCreditCardImage)}")]
        Task<ApiResponseModel> UploadCreditCardImage([Query] Guid id, StreamPart imageFile);

        [Post($"{Controller}{nameof(RemoveCreditCardImage)}")]
        Task RemoveCreditCardImage([Query] Guid id);

        [Get($"{Controller}{nameof(PopulateCreditCards)}")]
        Task PopulateCreditCards();

        [Post($"{Controller}{nameof(UploadCreditCardImageByFile)}")]
        Task<ApiResponseModel> UploadCreditCardImageByFile(DocumentDto imageFile);
    }
}
