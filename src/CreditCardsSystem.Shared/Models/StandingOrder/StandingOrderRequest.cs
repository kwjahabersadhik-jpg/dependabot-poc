using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace CreditCardsSystem.Domain.Models.StandingOrder;

public class StandingOrderRequest : ValidateModel<StandingOrderRequest>
{
    [Required(ErrorMessage = "Please select an account")]
    public string DebitAccountNumber { get; set; }

    [Required(ErrorMessage = "Please enter amount")]
    public decimal Amount { get; set; }
    public decimal VatAmount { get; set; }
    public decimal ChargeAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = null!;

    [Required(ErrorMessage = "Please select beneficiary card number")]
    public string BeneficiaryCardNumber { get; set; }

    public string? ChargeAccountNumber { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? LastPayment { get; set; }
    public decimal? RequestId { get; set; }
    public int? NumberOfTransfer { get; set; } = default!;
    public int BranchNumber { get; set; }
    public int? StandingOrderId { get; set; }
    public bool AllowDelete { get; set; } = false;
    public bool AllowUpdate { get; set; } = false;
    public bool IsApproved { get; set; } = false;
    public DurationTypes? OrderDuration { get; set; } = null;
}
public class StandingOrderResponse
{
    public string Status { get; set; }
}


