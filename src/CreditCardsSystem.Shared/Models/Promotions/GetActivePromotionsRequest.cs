using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Enums;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace CreditCardsSystem.Domain.Models.Promotions;

public class GetActivePromotionsRequest : ValidateModel<GetActivePromotionsRequest>
{
    [RegularExpression(@"^\d{1,12}", ErrorMessage = "Invalid AccountNumber")]
    public string? AccountNumber { get; set; }

    [RegularExpression(@"^\d{1,12}", ErrorMessage = "Invalid Civil ID")]
    public string? CivilId { get; set; }
    public int ProductId { get; set; }

    [JsonIgnore]
    public int UserBranch { get; set; }
    public Collateral? Collateral { get; set; }

    public int? PromotionId { get; set; }

}
