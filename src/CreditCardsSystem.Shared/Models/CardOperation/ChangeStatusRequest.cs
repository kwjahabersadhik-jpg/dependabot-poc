using CreditCardsSystem.Domain.Enums;

namespace CreditCardsSystem.Domain.Models.CardOperation;


public class ChangeStatusRequest
{
    public decimal RequestID { get; set; }
    public CreditCardStatus NewStatus { get; set; }
}


public class ChangeStatusResponse
{
    public decimal RequestID { get; set; }
    public CreditCardStatus CardStatus { get; set; }
    public string Description { get; set; } = string.Empty;
}


public class UpdateRequestDto
{
    public string CardNo { get; set; } = string.Empty;
    public int CardStatus { get; set; }
    public decimal ApprovedLimit { get; set; }
    public DateTime Expiry { get; set; }
    public string BankAcctNo { get; set; } = string.Empty;
    public decimal RequestID { get; set; }
    public DateTime RequestDate { get; set; }
    public int CardType { get; set; }
    public string CivilID { get; set; } = string.Empty;
    public int BranchID { get; set; }
    public string TellerID { get; set; } = string.Empty;
    public DateTime ApproveDate { get; set; }
    public int ServicePeriod { get; set; }
    public string Remark { get; set; } = string.Empty;
    public int Photo { get; set; }
    public string DepositNumber { get; set; } = string.Empty;
    public int DepositAmount { get; set; }
    public long POBox { get; set; }
    public string City { get; set; } = string.Empty;
    public long PostCode { get; set; }
    public string Street { get; set; } = string.Empty;
    public string Continuation_1 { get; set; } = string.Empty;
    public string Continuation_2 { get; set; } = string.Empty;
    public long Mobile { get; set; }
    public long? HomePhone { get; set; }
    public long WorkPhone { get; set; }
    public decimal Salary { get; set; }
    public int MurabahaInstallments { get; set; }
    public int RealEstateInstallments { get; set; }
    public decimal RequestedLimit { get; set; }
    public long SellerID { get; set; }
    public string FaxReference { get; set; } = string.Empty;
    public string FdAccountNumber { get; set; } = string.Empty;
    public int OldCardStatus { get; set; }
}
