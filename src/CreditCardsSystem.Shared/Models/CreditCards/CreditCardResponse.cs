namespace CreditCardsSystem.Domain.Models.CreditCards;

public class CreditCardResponse
{

    public string CardNo { get; set; }

    public int CardStatus { get; set; }

    public decimal ApprovedLimit { get; set; }

    public System.DateTime? Expiry { get; set; }

    public bool ExpiryFieldSpecified;

    public string? BankAcctNo { get; set; }

    public long RequestID { get; set; }

    public System.DateTime RequestDate { get; set; }

    public bool RequestDateFieldSpecified;

    public int CardType { get; set; }


    public string? CivilID { get; set; }

    public int BranchID { get; set; }

    public string? TellerID { get; set; }

    public System.DateTime ApproveDate { get; set; }

    public bool ApproveDateFieldSpecified;

    public int ServicePeriod { get; set; }

    public string? Remark { get; set; }

    public int Photo { get; set; }

    public string? DepositNumber { get; set; }

    public int DepositAmount { get; set; }

    public long POBox { get; set; }

    public string? City { get; set; }

    public long PostCode { get; set; }

    public string? Street { get; set; }

    public string? Continuation_1 { get; set; }

    public string? Continuation_2 { get; set; }

    public long Mobile { get; set; }

    public long HomePhone { get; set; }

    public long WorkPhone { get; set; }

    public decimal Salary { get; set; }

    public int MurabahaInstallments { get; set; }

    public int RealEstateInstallments { get; set; }

    public int RequestedLimit { get; set; }

    public long SellerID { get; set; }

    public string? FaxReference { get; set; }

    public string? FdAccountNumber { get; set; }
    public string? ProductName { get; set; } = null!;
}
