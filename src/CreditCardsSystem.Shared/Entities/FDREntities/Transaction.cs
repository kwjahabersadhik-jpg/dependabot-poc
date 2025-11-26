namespace CreditCardsSystem.Data.Models
{
    using CreditCardsSystem.Domain.Enums;
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("MIGS_TRANSACTIONS")]
    public class Transaction
    {
        [Key, Column("TRANSACTION_ID")]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }

        [Column("MIGS_MASTER_ID")]
        public int MasterId { get; set; }

        [Column("LOAD_DATE")]
        public DateTime LoadDate { get; set; }

        [Column("LOAD_ID")]
        public int LoadId { get; set; }

        [Column("IS_FRAUDULENT")]
        public TransactionStatus IsFraudulent { get; set; }

        [Column("FRAUDULENT_REASON_ID")]
        public int? FraudulentReasonId { get; set; }

        [Column("IS_SENT_TO_FDR")]
        public bool IsSentToFDR { get; set; }

        [Column("SENT_DATE")]
        public DateTime? SentOn { get; set; }

        [Column("MERCHANT_NO")]
        public string MerchantNo { get; set; }

        [Column("EQUIVALENT_AMOUNT_KD")]
        public decimal? EquivelantAmountKD { get; set; }

        [Column("ISSUER_BIN")]
        public int? IssuerBin { get; set; }

        public virtual FraudulentReason FraudulentReason { get; set; }

        public virtual FraudulentStatus FraudulentStatus { get; set; }

        public virtual Master Master { get; set; }

        public virtual Merchant Merchant { get; set; }

        public virtual Issuer Issuer { get; set; }


        [Column("CARD_NO")]
        public string CardNo { get; set; }

        [Column("A_S_P_DATA_MERCHANTTRANSID")]
        public string MerchantTransID { get; set; }

        [Column("CARD_TYPE")]
        public CardTypes? CardType { get; set; }

        [Column("MESSAGE_TYPE")]
        public string MessageType { get; set; }

        [Column("PROCESSING_CODE")]
        public string ProcessingCode { get; set; }

        [Column("AMOUNT_TRANSACTION")]
        public decimal? TransactionAmount { get; set; }

        [Column("CURR_CODE_SETTLEMENT")]
        public string NumericCurrencyCode { get; set; }

        [Column("RECEIPT_NO")]
        public string ReceiptNo { get; set; }

        [Column("RESPONSE_CODE")]
        public string ResponseCode { get; set; }

        [Column("AUTH_ID_RESPONSE")]
        public string AuthCode { get; set; }

        [Column("TRANS_DATE")]
        public DateTime? TransactionDate { get; set; }

        [Column("TRANSMISSION_DATE_TIME")]
        public DateTime? TransmissionDate { get; set; }

        [Column("ELECT_COM_INDIC_SPA")]
        public string ECIFlag { get; set; }

        [Column("VISA_3D_SECURE")]
        public string Visa3DSecure { get; set; }

        [Column("A_S_P_DATA_TICKETNO")]
        public string TicketPNR { get; set; }

        [Column("COMMISION_AMOUNT")]
        public decimal? CommissionAmount { get; set; }

        [Column("POST_DATE")]
        public DateTime? PostDate { get; set; }

        [Column("TRANSACTION_STATUS")]
        public string PhxTransactionStatus { get; set; }

        [Column("REJECT_REASON")]
        public string RejectReason { get; set; }
    }
}
