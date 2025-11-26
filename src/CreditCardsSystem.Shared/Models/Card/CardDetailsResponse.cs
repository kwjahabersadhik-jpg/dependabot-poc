using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Models.CardIssuance;
using System.Text.Json.Serialization;

namespace CreditCardsSystem.Domain.Models.Card;

public class CardDetailsResponse
{
    public string? Expiry { get; set; }
    public string? HolderNamePrefix { get; set; }
    public string? HolderName { get; set; }
    public string? HolderEmbossName { get; set; }
    public string? PrimaryCardHolderName { get; set; }
    public string CivilId { get; set; }
    public string? CustomerCrossRefID { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? SavingAccountNumber { get; set; }
    public string? HolderAddressLine1 { get; set; }
    public string? HolderAddressLine2 { get; set; }
    public string? HolderAddressLine3 { get; set; }
    public string? HolderAddressCity { get; set; }
    public string? HolderPostalCode { get; set; }
    public string? HolderCountryCode { get; set; }
    public string? PlasticsMailCode { get; set; }
    public string? HolderBusinessPhone { get; set; }
    public string? HolderHomePhone { get; set; }
    public string? HolderMobilePhone { get; set; }
    public string? InternalStatus { get; set; }
    public string? ExternalStatus { get; set; }
    public DateTime DateLastStatusChanged { get; set; }
    public bool DateLastStatusChangedSpecified { get; set; }
    public string AccountActiviationFlag { get; set; } = string.Empty;
    public DateTime OpenDate { get; set; }
    public bool OpenDateSpecified { get; set; }
    public DateTime DateAccountOpened { get; set; }
    public bool DateAccountOpenedSpecified { get; set; }
    public bool DateAccountTransferedSpecified { get; set; }
    public DateTime DateAnnualCharge { get; set; }
    public bool DateAnnualChargeSpecified { get; set; }
    public DateTime DateCreditLineChanged { get; set; }
    public bool DateCreditLineChangedSpecified { get; set; }
    public decimal Balance { get; set; }
    public decimal AvailableLimit { get; set; }
    public decimal Limit { get; set; }

    public decimal CurrentMinPaymentDue { get; set; }
    public decimal DelinquentAmount { get; set; }
    public int DaysDelinquent { get; set; }
    public decimal DisputeAmount { get; set; }
    public decimal LastPayAmount { get; set; }
    public DateTime LastPayDate { get; set; }
    public bool LastPayDateSpecified { get; set; }
    public DateTime DateNextPayment { get; set; }
    public bool DateNextPaymentSpecified { get; set; }
    public decimal LastStatementBalance { get; set; }
    public DateTime DateLastStatement { get; set; }
    public bool DateLastStatementSpecified { get; set; }
    public DateTime CompleteDateOfBirth { get; set; }
    public bool CompleteDateOfBirthSpecified { get; set; }
    public string? CardBlockStatus { get; set; }
    public string PlasticAction { get; set; }
    public PlasticActions? PlasticActionEnum
    {
        get
        {
            bool isIntValue = int.TryParse(PlasticAction, out int _plasticAction);
            if (!isIntValue)
            {
                _plasticAction = PlasticAction switch
                {
                    "A" => 10,
                    "R" => 11,
                    "L" => 12,
                    "B" => 13,
                    _ => _plasticAction
                };

            }
            return (PlasticActions)(int)_plasticAction;
        }
    }
    [JsonIgnore]
    public string? SecondaryCardNo { get; set; }
    public string? SecondaryCardNoDto { get; set; }

    public string? KfhStaff { get; set; }
    public string? FdrAccountNumber { get; set; }
    public DeliveryStatus DeliveryStatus { get; set; }
    public string? PcdFlag { get; set; }
    public string? PromotionName { get; set; }

    public string? VisaStatus { get; set; }
    public string? MasterCardStatus { get; set; }
    public decimal? MasterCardRequestId { get; set; }

    public bool IsCorporateCard { get; set; }
    public string? CorporateCivilId { get; set; }
    [JsonIgnore]
    public string? CardNumber { get; set; }
    public string? CardNumberDto { get; set; }

    public bool IsPrimaryCard { get; set; }
    public bool IsSupplementaryCard { get; set; }

    public decimal RequestId { get; set; }
    public string? PrimaryCardRequestId { get; set; }
    public string? PrimaryCardCivilId { get; set; }
    public CreditCardStatus CardStatus { get; set; }
    public string? Collateral { get; set; }
    public RequestParameterDto? Parameters { get; set; }

    private int _CardType;
    public int CardType
    {
        get { return _CardType; }// _CardType == ConfigurationBase.AlOsraPrimaryCardTypeId ? ConfigurationBase.AlOsraSupplementaryCardTypeId : _CardType; }
        set { _CardType = value; }
    }




    public int BranchId { get; set; }
    public string TellerId { get; set; } = string.Empty;
    public int ServicePeriod { get; set; }
    public DateTime ReqDate { get; set; }
    public decimal RequestedLimit { get; set; }
    public string Street { get; set; } = string.Empty;
    public int Photo { get; set; }
    public decimal ApproveLimit { get; set; }
    public int? DepositAmount { get; set; }
    public int Salary { get; set; }
    public bool IsExternalStatusLoaded { get; set; } = false;
    public int SupplementaryCardCount { get; set; }
    public ProductTypes ProductType { get; set; }
    public IssuanceTypes IssuanceType { get; set; }
    public string? EarlyClosurePercentage { get; set; }
    public string? PCTId { get; set; }
    public string? EarlyClosureMonths { get; set; }
    public string? EarlyClosureFees { get; set; }
    public string ProductName { get; set; } = null!;
    public string? ExternalStatusCode { get; set; }
    public string? InternalStatusCode { get; set; }
    public Dictionary<CFUActivity, bool>? PendingActivities { get; set; }
    public List<ListItemGroup<RequestType>> EligibleActions { get; set; } = new();
    public bool IsAllowStandingOrder { get; set; }
    public bool IsCardNotFound { get; set; } = false;
    public bool EligibleActionsLoaded { get; set; }
    public CardCurrencyDto? Currency { get; set; }
    public decimal Installment { get; set; }
    public decimal PreviousKFHCardLimit { get; set; }
    public decimal PrevKFHCardInstallment { get; set; }

    public BillingAddressModel BillingAddress { get; set; }
    [JsonIgnore]
    public string? AUBCardNumber { get; set; }
    public string? AUBCardNumberDto { get; set; }

    public string? MemberShipId { get; set; }
    public string? MinLimit { get; set; }
    public string? MaxLimit { get; set; }
}
