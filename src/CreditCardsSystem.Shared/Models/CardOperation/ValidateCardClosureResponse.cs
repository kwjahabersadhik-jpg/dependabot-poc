using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Models.CardIssuance;
using CreditCardsSystem.Domain.Models.SupplementaryCard;

namespace CreditCardsSystem.Domain.Models.CardOperation;

public class ValidateCardClosureResponse
{
    public CardCategoryType CardCategory { get; set; } = CardCategoryType.Normal;
    public decimal FeeAmount { get; set; } = 0;
    public decimal TotalFee { get; set; } = 0;
    public decimal Balance { get; set; } = 0;
    public List<SupplementaryCardDetail>? SupplementaryCards { get; set; }
    public List<AccountDetailsDto>? DebitAccounts { get; set; }
    public bool IsHavingInsufficientBalance { get; set; } = false;
    public decimal OriginalFee { get; set; }
    public decimal VATAmount { get; set; } = 0;
    public decimal TotalAmount { get; set; }
    public decimal? BalanceInKWD { get; set; }
    public decimal? TotalAmountInKWD { get; set; }

    
}
