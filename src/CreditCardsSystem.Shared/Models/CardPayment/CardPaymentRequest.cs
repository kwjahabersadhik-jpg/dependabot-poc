using CreditCardsSystem.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace CreditCardsSystem.Domain.Models.CardPayment;

public class CardPaymentRequest : ValidateModel<CardPaymentRequest>
{
    [Required(ErrorMessage = "Enter CivilId")]
    [RegularExpression(@"^\d{1,12}", ErrorMessage ="Invalid Civil ID")]
    public string CivilId { get; set; } = null!;

    [Required(ErrorMessage = "Select beneficiary card number")]
    [RegularExpression(@"^\d{1,50}", ErrorMessage = "Invalid BeneficiaryCardNumber")]
    public string? BeneficiaryCardNumber { get; set; } = null!;

    [Required(ErrorMessage = "Select debit account number")]
    [RegularExpression(@"^\d{1,12}", ErrorMessage = "Invalid DebitAccountNumber")]
    public string DebitAccountNumber { get; set; } = null!;

    [Required(ErrorMessage = "Enter Amount")]
    [Range(1, double.MaxValue, ErrorMessage = "Enter valid amount, should be grater than 0")]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(3, ErrorMessage = "Invalid Currency")]
    public string? Currency { get; set; }

    public decimal ForeignAmount { get; set; } = 0;

    [MaxLength(3, ErrorMessage = "Invalid TransferAmountCurrency")]
    public string? TransferAmountCurrency { get; set; }


    [RegularExpression(@"^\d{1,12}", ErrorMessage = "Invalid ChargeAccountNumber")]
    public string? ChargeAccountNumber { get; set; } = null!;

    [Required]
    public decimal RequestId { get; set; }

    [Required]
    public int BranchNumber { get; set; }



}
public class CardPaymentResponse
{
    public string Message { get; set; }
}