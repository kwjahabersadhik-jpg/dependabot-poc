namespace CreditCardsSystem.Domain.Models;

public class BalanceCardStatusDetails
{
    public string Expiry { get; set; }

    public string HolderNamePrefix { get; set; }

    public string HolderName { get; set; }

    public string HolderEmbossName { get; set; }

    public string CivilId { get; set; }

    public string CustomerCrossRefId { get; set; }

    public string BankAccountNumber { get; set; }

    public string SavingAccountNumber { get; set; }

    public string HolderAddressLine1 { get; set; }

    public string HolderAddressLine2 { get; set; }

    public string HolderAddressLine3 { get; set; }

    public string HolderAddressCity { get; set; }

    public string HolderPostalCode { get; set; }

    public string HolderCountryCode { get; set; }

    public string PlasticsMailCode { get; set; }

    public string HolderBusinessPhone { get; set; }

    public string HolderHomePhone { get; set; }

    public string HolderMobilePhone { get; set; }

    public string InternalStatus { get; set; }

    public string ExternalStatus { get; set; }

    public DateTime DateLastStatusChanged { get; set; }

    public bool DateLastStatusChangedFieldSpecified { get; set; }

    public string AccountActiviationFlag { get; set; }

    public DateTime OpenDate { get; set; }

    public bool OpenDateFieldSpecified { get; set; }

    public DateTime DateAccountOpened { get; set; }

    public bool DateAccountOpenedFieldSpecified { get; set; }

    public DateTime DateAccountTransfered { get; set; }

    public bool DateAccountTransferedFieldSpecified { get; set; }

    public DateTime DateAnnualCharge { get; set; }

    public bool DateAnnualChargeFieldSpecified { get; set; }

    public DateTime DateCreditLineChanged { get; set; }

    public bool DateCreditLineChangedFieldSpecified { get; set; }

    public decimal Balance { get; set; }

    public decimal AvailableLimit { get; set; }


    public decimal CurrentMinPaymentDue { get; set; }

    public decimal DelinquentAmount { get; set; }

    public int DaysDelinquent { get; set; }

    public decimal DisputeAmount { get; set; }

    public decimal LastPayAmount { get; set; }

    public DateTime LastPayDate { get; set; }

    public bool LastPayDateFieldSpecified { get; set; }

    public DateTime DateNextPayment { get; set; }

    public bool DateNextPaymentFieldSpecified { get; set; }

    public decimal LastStatementBalance { get; set; }

    public DateTime DateLastStatement { get; set; }

    public bool DateLastStatementFieldSpecified { get; set; }

    public DateTime CompleteDateOfBirth { get; set; }

    public bool CompleteDateOfBirthFieldSpecified { get; set; }

    public string CardBlockStatus { get; set; }

    public string PlasticAction { get; set; }

    public string SecondaryCardNo { get; set; }

    public string KfhStaff { get; set; } = string.Empty;

    public string FdrAccountNumber { get; set; }
    public bool IsCardNotFound { get; set; }
}