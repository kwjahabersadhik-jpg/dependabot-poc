using CreditCardsSystem.Data;
using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Entities.Admin;
using CreditCardsSystem.Domain.Models.Card;
using CreditCardsSystem.Domain.Models.Reports;
using CreditCardTransactionInquiryServiceReference;
using Kfh.Aurora.Integration;
using Kfh.Aurora.Utilities.FileValidation;
using Microsoft.EntityFrameworkCore;
using Refit;

namespace CreditCardsSystem.Application.Admin.CreditCard
{
    public class CreditCardArtworkAppService(ApplicationDbContext context, IIntegrationUtility integrationUtility, IOptions<IntegrationOptions> options, IFileValidator fileValidator) : ICreditCardArtworkAppService, IAppService
    {
        private readonly ApplicationDbContext _context = context;
        private readonly IFileValidator _fileValidator = fileValidator;
        private readonly CreditCardInquiryServicesServiceClient _cardInquiryServicesServiceClient = integrationUtility.GetClient<CreditCardInquiryServicesServiceClient>(options.Value.Client, options.Value.Endpoints.CreditCardTransactionInquiry, options.Value.BypassSslValidation);

        [HttpGet]
        public async Task<CardTypeDto?> GetCreditCardType(Guid id)
        {
            var classes = await _context.CardTypes.SingleAsync(x => x.Id == id);
            return classes.Adapt<CardTypeDto>();
        }

        [HttpGet]
        public async Task<List<CardTypeDto>> GetCreditCardsTypes()
        {
            var classes = await _context.CardTypes
            .OrderBy(x => x.Name)
            .AsNoTracking()
            .Select(x => new CardTypeDto() { Id = x.Id, FileName = x.FileName, IsActive = x.IsActive, Name = x.Name, NameAr = x.NameAr, Type = x.Type, ProductId = Convert.ToInt32(x.Type) })
            .OrderBy(x => x.ProductId)
            .ToListAsync();
            return classes;
        }

        [HttpPost]
        public async Task UpdateCreditCardType([FromBody] CardTypeDto input)
        {
            var existing = await _context.CardTypes.SingleOrDefaultAsync(x => x.Id == input.Id);
            if (existing == null)
                return;
            existing.Name = input.Name;
            existing.NameAr = input.NameAr;
            existing.Type = input.Type;
            existing.IsActive = input.IsActive;
            await _context.SaveChangesAsync();
        }

        [HttpPost]
        public async Task DeleteCreditCardType(Guid id)
        {
            await _context.CardTypes.Where(x => x.Id == id).ExecuteDeleteAsync();
        }

        [HttpPost]
        public async Task<ApiResponseModel> UploadCreditCardImage(Guid id, StreamPart imageFile)
        {
            var byteArray = new byte[imageFile.Value.Length];
            await using MemoryStream ms = new MemoryStream(byteArray);
            await imageFile.Value.CopyToAsync(ms);
            var fileByteArray = ms.ToArray();

            var isValidExtension = await _fileValidator.IsValidExtension(fileByteArray, ["jpg", "jpeg", "png", "svg"]);

            if (!Helpers.IsValidFile(imageFile.FileName, fileByteArray, isValidExtension.IsValid, 2))
                return new ApiResponseModel() { IsSuccess = false, Message = GlobalResources.InvalidFile };

            var creditCard = await _context.CardTypes.SingleOrDefaultAsync(x => x.Id == id);
            if (creditCard == null)
                return new ApiResponseModel() { IsSuccess = false, Message = GlobalResources.InvalidRequest };

            creditCard.Image = fileByteArray;
            creditCard.FileName = imageFile.FileName;
            creditCard.Extension = imageFile.ContentType;
            await _context.SaveChangesAsync();

            return new ApiResponseModel() { IsSuccess = true, Message = GlobalResources.SuccessUpdate };
        }



        [HttpPost]
        public async Task<ApiResponseModel> UploadCreditCardImageByFile([FromBody] DocumentDto file)
        {
            var isValidExtension = await _fileValidator.IsValidExtension(file.Content, ["jpg", "jpeg", "png", "svg"]);

            if (!Helpers.IsValidFile(file.FileName, file.Content, isValidExtension.IsValid,2))
                return new ApiResponseModel() { IsSuccess = false, Message = GlobalResources.InvalidFile };

            string cardType = file.FileName.Split(".")[0];
            var creditCard = await _context.CardTypes.SingleOrDefaultAsync(x => x.Type == cardType);

            if (creditCard == null)
                return new ApiResponseModel() { IsSuccess = false, Message = GlobalResources.InvalidRequest };


            creditCard.Image = file.Content;
            creditCard.FileName = file.FileName;
            creditCard.Extension = file.FileExtension.ToString();
            await _context.SaveChangesAsync();

            return new ApiResponseModel() { IsSuccess = true, Message = GlobalResources.SuccessUpdate };
        }



        [HttpPost]
        public async Task RemoveCreditCardImage(Guid id)
        {
            var creditCard = await _context.CardTypes.SingleOrDefaultAsync(x => x.Id == id);
            if (creditCard == null)
                return;

            creditCard.Image = null;
            creditCard.FileName = null;
            creditCard.Extension = null;
            await _context.SaveChangesAsync();
        }

        [NonAction]
        public async Task<CardType?> GetCardImage(string type)
        {
            return await _context.CardTypes.AsNoTracking()
                .Where(x => x.Type == type).FirstOrDefaultAsync();
        }

        [NonAction]
        public async Task<IEnumerable<CardImage?>> GetAllCardImages()
        {
            var cards = _context.CardTypes.AsNoTracking()
               .Where(x => x.Image != null).ToList();


            var cardImages = new List<CardImage>();

            foreach (var card in cards)
            {
                cardImages.Add(new CardImage()
                {
                    CardType = card.Type,
                    ImageBase64 = Convert.ToBase64String(card.Image),
                    Extension = card.Extension
                });
            }

            return cardImages;
        }

        [HttpGet]
        public async Task PopulateCreditCards()
        {
            var creditCardsList = await _context.CardTypes.ToListAsync();

            var allProducts = await _cardInquiryServicesServiceClient.getAllProductsAsync(new getAllProductsRequest());

            if (allProducts.getAllProductsResult != null)
            {
                foreach (var c in allProducts.getAllProductsResult)
                {
                    var card = creditCardsList.SingleOrDefault(x => x.Type == c.cardType.ToString());

                    if (card is null)
                    {
                        var newCard = new CardType()
                        {
                            Name = c.name,
                            NameAr = c.arabicName,
                            Type = c.cardType,
                            IsActive = true,
                        };

                        _context.CardTypes.Add(newCard);
                    }
                    else
                    {
                        card.Name = c.name;
                        card.NameAr = c.arabicName;
                        card.Type = c.cardType;
                        card.IsActive = true;
                    }
                }
                await _context.SaveChangesAsync();
            }
        }
    }
}
