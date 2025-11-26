using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Models.CardIssuance;
using System.ComponentModel.DataAnnotations;

namespace CreditCardsSystem.Domain.Models.CardOperation;

public class ChangeOfAddressRequest : ValidateModel<ChangeOfAddressRequest>
{
    public decimal RequestId { get; set; }
    public BillingAddressModel BillingAddress { get; set; } = null!;

    [RegularExpression(@"^[a-zA-Z\s]{0,26}$", ErrorMessage = "Holder name must contain alpabetic characters and spaces, with a maximum of 26 characters")]
    public string NewCardHolderName { get; set; } = string.Empty;

    public string OldCardHolderName { get; set; } = string.Empty;
    public string NewLinkedAccountNumber { get; set; } = string.Empty;
    public string OldLinkedAccountNumber { get; set; } = string.Empty;

    public bool IsSupplementaryCard { get; set; }
    public CFUActivity CFUActivity { get; set; }
}

public class ChangeHolderNameRequest : ValidateModel<ChangeHolderNameRequest>
{
    public decimal RequestId { get; set; }

    [RegularExpression(@"^[a-zA-Z\s]{0,26}$", ErrorMessage = "Holder name must contain alpabetic characters and spaces, with a maximum of 26 characters")]
    public string NewCardHolderName { get; set; } = string.Empty;
    public string OldCardHolderName { get; set; } = string.Empty;
    public bool IsSupplementaryCard { get; set; }
    public CFUActivity CFUActivity => CFUActivity.CHANGE_CARDHOLDERNAME;
}

public class ChangeLinkedAccountRequest : ValidateModel<ChangeLinkedAccountRequest>
{
    public decimal RequestId { get; set; }

    public string NewLinkedAccountNumber { get; set; } = string.Empty;
    public string OldLinkedAccountNumber { get; set; } = string.Empty;

    public bool IsSupplementaryCard { get; set; }
    public CFUActivity CFUActivity => CFUActivity.CHANGE_CARD_LINKED_ACCT;
}

public class ChangeOfDetailResponse
{

}

public class EmbosserUpdateRequest
{

}

public record UpdateBillingAddressRequest(string CardNumber, BillingAddressModel BillingAddress);

