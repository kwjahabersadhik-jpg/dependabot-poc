using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Enums;
using System.Text.Json.Serialization;

namespace CreditCardsSystem.Domain.Models.Card;


//TODO : Remove this class and use Card Detail Response class
public class CreditCardDto
{
    public string ProductType { get; set; } = default!;

    [JsonIgnore]
    public string CardNumber { get; set; } = default!;
    public string CardNumberDto { get; set; } = default!;


    public string CardType { get; set; } = default!;

    public string AccountNumber { get; set; } = default!;

    public int BranchId { get; set; } = default!;

    public string BranchName { get; set; } = default!;

    public decimal MonthlyLimit { get; set; }

    public decimal? AvailableLimit { get; set; }

    public decimal? CardLimit { get; set; }

    public DateTime OpenedDate { get; set; }

    public DateTime? ExpirationDate { get; set; }

    public int StatusId { get; set; }

    public string Status { get; set; } = default!;

    public decimal MaxMonthlyInstallments { get; set; }

    public string CbkClass { get; set; } = default!;

    public decimal? BalanceAmount { get; set; }

    public decimal? HoldAmount { get; set; }

    public decimal? OverDueAmount { get; set; }

    public int InstallmentCount { get; set; }
    public decimal ApprovedLimit { get; set; }


    public CardCategoryType CardCategory { get; set; }
    public DataItem<CardDetailsResponse> CardBalance { get; set; } = new();
    public decimal RequestId { get; set; }
    public string Category { get; set; }
    public string? MemberShipId { get; set; }


    public bool IsAllowStandingOrder { get; set; }
    public string Collateral { get; set; } = null!;
    public string CurrencyISO { get; set; }
    public int IsAUB { get; set; }
    [JsonIgnore]
    public string? AUBCardNumber { get; set; }
    public string AUBCardNumberDto { get; set; }
    public string CivilId { get; set; }
    public long? MobileNumber { get; set; }
}




public class CreditCardLiteDto
{
    public decimal RequestId { get; set; }
    [JsonIgnore]
    public string CardNumber { get; set; }
    public string CardNumberDto { get; set; }
    public int CardType { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public DateTime OpenedDate { get; set; }
    public int StatusId { get; set; }
    public string Status { get; set; }
    public string Collateral { get; set; }
    public string CurrencyISO { get; set; }
    public CardCategoryType CardCategory { get; set; }
    public string ProductType { get; set; }
    public string CivilId { get; set; }
    public int IsAUB { get; set; }

    [JsonIgnore]
    public string? AUBCardNumber { get; set; }
    public string? AUBCardNumberDto { get; set; }

}

