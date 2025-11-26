namespace CreditCardsSystem.Domain.Models.CardPayment;

public class DebitVoucher
{
    public DebitVoucher()
    {

    }
    public string BranchName { get; set; }
    public string DebitAcctNo { get; set; }
    public string AcctName { get; set; }
    public string Crncy { get; set; }
    public string IBAN { get; set; }
    public string ParamFooter { get; set; }
    public string ParamHeader { get; set; }
    public DateTime PrintDate { get; set; }
    public string CreditAcctNo { get; set; }

    public string TransferDesc { get; set; }
    public int CardLimit { get; set; }

    public string Tafkeet { get; set; }
    public int Amt { get; set; }

    public string CivilID { get; set; }
    public string MaskedCardNumber { get; set; }
    public required decimal RequestId { get; set; }

}


public class DepositVoucher
{
    public DepositVoucher()
    {

    }

    public string BranchName { get; set; }
    public string DebitAcctNo { get; set; }
    public string AcctName { get; set; }
    public string Crncy { get; set; }
    public string IBAN { get; set; }
    public string ParamFooter { get; set; }
    public string ParamHeader { get; set; }
    public DateTime PrintDate { get; set; }
    public double HoldAmount { get; set; }
    public string HoldExpiry { get; set; }
    public string HoldDesc { get; set; }

    public string CivilID { get; set; }

    public required decimal RequestId { get; set; }
}



public class PaymentVoucher
{
    public string AcctNo { get; set; }
    public string MaskedCardNumber { get; set; }
    public string Amount { get; set; }
    public string CurrencyISO { get; set; }
    public object CivilID { get; set; }
}
