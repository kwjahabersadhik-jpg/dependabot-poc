using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Models.CardIssuance;

namespace CreditCardsSystem.Domain.Models.Request;

public class RequestFilter
{
    public string CardNumber { get; set; } = string.Empty;
    public string? CustomerCivilId { get; set; }
    public int? ProductId { get; set; }//Card Type
    public int? SellerId { get; set; }

    public decimal? RequestId { get; set; }
    public DateTime? RequestedDateFrom { get; set; }
    public DateTime? RequestedDateTo { get; set; }
    public Collateral? Collateral { get; set; }
    public Applications? Application { get; set; }
    public ProductCategory? Category { get; set; }


    public DateTime? ApprovedDateFrom { get; set; }
    public DateTime? ApprovedDateTo { get; set; }

    public CreditCardStatus? RequestStatus { get; set; }
    public int CardStatusId { get; set; } = -1;
    public CreditCardStatus? CardStatus => (CreditCardStatus)CardStatusId;

    public string BranchId { get; set; }
    public string? CustomerClass { get; set; }
    public int? Gender { get; set; }
    public bool IsSupplementaryCard { get; set; }
    public RequestParameterDto RequestParameter { get; set; }

}
