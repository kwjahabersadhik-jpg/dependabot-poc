using CreditCardsSystem.Domain.Enums;

namespace CreditCardsSystem.Domain.Models.Migs;

public class FilterWrapper
{
    public List<TransactionsFilterResult> Transactions { get; set; }
    public int DataCount { set; get; }

}
public class MigsTransactionsFilter
{
    public TransactionStatus? FraudStatus { get; set; }

    public int? MerchantGroupId { get; set; }

    public string IssuerName { get; set; }

    public string IssuerCountry { get; set; }

    public bool? SentToFDR { get; set; }

    // sybase fields

    public DateTime? TransactionDateFrom { get; set; }

    public DateTime? TransactionDateTo { get; set; }

    public string ECIIndicator { get; set; }

    public int LoadId { get; set; }

    public DateTime? LoadDate { get; set; }

    public decimal? KWDAmount { get; set; }

    public string MerchantTransactionId { get; set; }

    public string CardNo { get; set; }

    public string PhxTransactionStatus { get; set; }

    public string MerchantNo { get; set; }

    public int? IssuerBin { get; set; }

    public int PageSize { get; set; }

    public int PageNumber { get; set; }
}

public class TransactionsFilterResult
{
    public int Id { get; set; }

    [ReportHeader("Message Type")]
    public string MessageType { get; set; }

    [ReportHeader("PCode")]
    public string ProcessingCode { get; set; }

    [ReportHeader("Card No.")]
    public string CardNo { get; set; }

    [ReportHeader("Trans Amount")]
    public decimal? TransactionAmount { get; set; }

    [ReportHeader("Numeric Currency Code")]
    public int NumericCurrencyCode { get; set; }

    [ReportHeader("Receipt No.")]
    public string ReceiptNo { get; set; }

    [ReportHeader("Response Code")]
    public string ResponseCode { get; set; }

    [ReportHeader("Auth Code")]
    public string AuthCode { get; set; }

    [ReportHeader("Trans Date")]
    public string TransactionDate { get; set; }

    [ReportHeader("Transmission Date")]
    public string TransmissionDate { get; set; }

    [ReportHeader("ECIFlag")]
    public string ECIFlag { get; set; }

    [ReportHeader("Visa 3D Secure")]
    public string Visa3DSecure { get; set; }

    [ReportHeader("Merchant ID")]
    public string MerchantNo { get; set; }

    [ReportHeader("Issuer BIN")]
    public int? IssuerBin { get; set; }

    [ReportHeader("Issuer Name")]
    public string IssuerName { get; set; }

    [ReportHeader("Issuer Country")]
    public string IssuerCountry { get; set; }

    [ReportHeader("Merchant Trans ID")]
    public string MerchantTransID { get; set; }

    [ReportHeader("Ticket PNR")]
    public string TicketPNR { get; set; }

    [ReportHeader("KD Amount")]
    public decimal? EquivelantAmountKD { get; set; }

    [ReportHeader("Commission")]
    public decimal? CommissionAmount { get; set; }

    [ReportHeader("Post Date")]
    public string PostDate { get; set; }

    [ReportHeader("Trans Status")]
    public string PhxTransactionStatus { get; set; }

    [ReportHeader("Reject Reason")]
    public string RejectReason { get; set; }

    [ReportHeader("File ID")]
    public int LoadId { get; set; }

    [ReportHeader("Fraud Status")]
    public string FraudStatus { get; set; }

    [ReportHeader("Alert Reason")]
    public string FraudulentReason { get; set; }

    [ReportHeader("Resend Date if it is re-instated")]
    public string ResendDate { get; set; }

    public override string ToString()
    {
        return $"{MessageType},{ProcessingCode}, {CardNo},{TransactionAmount},{NumericCurrencyCode}, {ReceiptNo},{ResponseCode},{AuthCode},{TransactionDate},{TransmissionDate},{ECIFlag},{Visa3DSecure}," +
            $" {MerchantNo},{IssuerBin},{IssuerName},{IssuerCountry},{MerchantTransID},{TicketPNR},{EquivelantAmountKD},{CommissionAmount},{PostDate},{PhxTransactionStatus},{RejectReason},{LoadId}," +
            $"{FraudStatus},{FraudulentReason},{ResendDate}\n";
    }
}



public class TransactionsStatus
{
    public TransactionStatus FraudStatus { get; set; }
    public int[] TransactionsNos { get; set; }
    public bool SendToFdr { get; set; }
}
