namespace CreditCardsSystem.Domain.Shared.Models.Voucher;

public class VoucherRequest
{

    public long ReportHeaderId { get; set; }


    public DateTime PrintDate { get; set; }


    public int ApplicationId { get; set; }


    public string UserId { get; set; }


    public int LocationId { get; set; }


    public string LocationName { get; set; }


    public string UserName { get; set; }


    public string CivilID { get; set; }


    public string AccountNo { get; set; }


    public string IBAN { get; set; }


    public string AccountName { get; set; }


    public string AccountCurrency { get; set; }


    public List<VoucherDetails> VoucherDetails { get; set; }
}

public class VoucherDetails
{
    public string FieldName { get; set; }

    public string FielValue { get; set; }
}
