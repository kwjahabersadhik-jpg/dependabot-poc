using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Models.Card;
using CreditCardsSystem.Domain.Models.CardOperation;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CreditCardsSystem.Domain.Models.CreditReverse;


public class CreditReverseRequest : ValidateModel<CreditReverseRequest>
{
    [Required]
    public string CivilId { get; set; } = null!;

    [Required(ErrorMessage = "Select beneficiary card number")]
    public string? BeneficiaryCardNumber { get; set; } = null!;

    [Required(ErrorMessage = "Select debit account number")]
    public string DebitAccountNumber { get; set; } = null!;

    [Required(ErrorMessage = "Enter Amount")]
    [Range(1, double.MaxValue, ErrorMessage = "Enter valid amount, should be grater than 0")]
    public decimal Amount { get; set; }

    //[Required]
    //public string? Currency { get; set; }

    public decimal AmountInKWD { get; set; } = 0;
    //public decimal TransferAmount { get; set; } = 0;
    public string? TransferAmountCurrency { get; set; }

    public string? ChargeAccountNumber { get; set; } = null!;

    [Required]
    public decimal RequestId { get; set; }

    [Required]
    public int BranchNumber { get; set; }

    [JsonIgnore]
    public int? IssuanceTypeId { get; set; }
    public string? ExternalStatus { get; set; }

    [JsonIgnore]
    public CardDetailsResponse? CardInfo { get; set; }
}
public class CreditReverseResponse
{
    public string Message { get; set; }
}

public class ProcessCreditReverseRequest : ActivityProcessRequest
{
}