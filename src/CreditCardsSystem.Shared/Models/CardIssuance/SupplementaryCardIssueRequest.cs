using System.ComponentModel.DataAnnotations;

namespace CreditCardsSystem.Domain.Models.CardIssuance
{
    //public class EditSupplementaryCardsRequest
    //{

    //    [Required(ErrorMessage = "You must enter SellerId and confirm seller name")]
    //    public long? SellerId { get; set; } = null;

    //    [Range(typeof(bool), "true", "true", ErrorMessage = "You must confirm the seller name")]
    //    public bool IsConfirmedSellerId { get; set; }
    //    public decimal PrimaryCardRequestID { get; set; }

    //}

    public class SupplementaryCardIssueRequest : BaseCardRequest
    {

        [Required(ErrorMessage = "You must enter SellerId and confirm seller name")]
        public new long? SellerId { get; set; } = null;

        [Range(typeof(bool), "true", "true", ErrorMessage = "You must confirm the seller name")]
        public new bool IsConfirmedSellerId { get; set; }

        [Required(ErrorMessage = "You must pass PrimaryCard RequestID")]
        public decimal PrimaryCardRequestID { get; set; }
        public List<SupplementaryEditModel> SupplementaryCards { get; set; } = null!;

    }
}
