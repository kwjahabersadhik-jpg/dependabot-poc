using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Models.CardIssuance;
using Newtonsoft.Json;

namespace CreditCardsSystem.Domain.Models;

public partial class RequestDto
{
    public decimal RequestId { get; set; }
    public DateTime ReqDate { get; set; } = DateTime.Now;
    public int ReqStatus { get; set; } = (int)CreditCardStatus.Pending;
    public int CardType { get; set; }
    public string? CardNo { get; set; }
    public string? Expiry { get; set; }
    public string? AcctNo { get; set; }
    public string CivilId { get; set; } = null!;
    public int BranchId { get; set; }
    public decimal? TellerId { get; set; }
    public decimal? Limit { get; set; }
    public decimal RequestedLimit { get; set; }
    public decimal? ApproveLimit { get; set; }
    public DateTime? ApproveDate { get; set; } = DateTime.Now;
    public byte ServicePeriod { get; set; } = 0;
    public string? Remark { get; set; }
    public bool Photo { get; set; }
    public string? DepositNo { get; set; }
    public decimal? DepositAmount { get; set; }
    public int? PostOfficeBoxNumber { get; set; }
    public string City { get; set; }
    public int PostalCode { get; set; }
    public string Street { get; set; }
    public string AddressLine1 { get; set; }
    public string AddressLine2 { get; set; }
    public long? Mobile { get; set; }
    public long? HomePhone { get; set; }
    public long? WorkPhone { get; set; }
    public decimal? Salary { get; set; }
    public int? MurInstallments { get; set; }
    public int? ReInstallments { get; set; }

    public long? SellerId { get; set; }
    public string? FaxReference { get; set; }
    public string? FdAcctNo { get; set; }
    public bool? IsAUB { get; set; }
    public RequestParameterDto Parameters { get; set; } = new();

    public string? ProductName { get; set; }

    [JsonIgnore]
    public BillingAddressModel BillingAddress
    {
        get => new()
        {
            City = City,
            FaxReference = FaxReference ?? "",
            HomePhone = HomePhone,
            PostOfficeBoxNumber = PostOfficeBoxNumber,
            PostalCode = PostalCode,
            Mobile = Mobile,
            Street = Street,
            WorkPhone = WorkPhone,
        };
    }
    public CFUActivity CFUActivity { get; set; }
}


public class RequestResponse
{
    public decimal ReqId { get; set; }
}
