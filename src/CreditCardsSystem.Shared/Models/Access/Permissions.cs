using CreditCardsSystem.Utility.Extensions;

namespace CreditCardsSystem.Domain.Models.Access;
public static class ActionsExtensions
{
    public static string Access(this string operation) => $"{operation}.access".ToCamelCase();
    public static string Approve(this string operation) => $"{operation}.approve".ToCamelCase();
    public static string Request(this string operation) => $"{operation}.request".ToCamelCase();
    public static string Masked(this string operation) => $"{operation}.masked".ToCamelCase();
    public static string Print(this string operation) => $"{operation}.print".ToCamelCase();
    public static string View(this string operation) => $"{operation}.view".ToCamelCase();
    public static string Create(this string operation) => $"{operation}.create".ToCamelCase();
    public static string Edit(this string operation) => $"{operation}.edit".ToCamelCase();
    public static string Delete(this string operation) => $"{operation}.delete".ToCamelCase();
    public static string Issue(this string operation) => $"{operation}.issue".ToCamelCase();
    public static string Waive(this string operation) => $"{operation}.waive".ToCamelCase();
}

public static class EnigmaActionsExtensions
{
    public static string EnigmaApprove(this string operation) => $"enigma.{operation}.approve".ToCamelCase();
    public static string EnigmaEdit(this string operation) => $"enigma.{operation}.edit".ToCamelCase();

}

public static class Permissions
{
    public static string CreditCheckFirstReview => nameof(CreditCheckFirstReview);
    public static string CreditCheckFinalReview => nameof(CreditCheckFinalReview);


    public const string AccountsBalance = "accounts.balance";
    public const string AccountsStaffBalance = "accounts.staff.balance";
    public const string StaffSalary = nameof(StaffSalary);

    #region Card Issuance
    public const string NormalCard = nameof(NormalCard);
    public const string ExceptionCard = nameof(ExceptionCard);

    public const string Prepaid = nameof(Prepaid);
    public const string PrepaidFC = nameof(PrepaidFC);
    public const string CoBrand = nameof(CoBrand);
    public const string Supplementary = nameof(Supplementary);
    public const string ChargeCard = nameof(ChargeCard);
    public const string TasyeerCard = nameof(TasyeerCard);

    public const string MemberShipDeleteRequest = nameof(MemberShipDeleteRequest);
    #endregion


    public const string CreditCards = nameof(CreditCards);
    public const string CreditCardsNumber = nameof(CreditCardsNumber);


    public const string StopCard = nameof(StopCard);
    public const string CreditReverse = nameof(CreditReverse);

    public const string ReportLostOrStolen = nameof(ReportLostOrStolen);

    public const string Cancel = nameof(Cancel);
    public const string StandingOrder = nameof(StandingOrder);
    public const string CardReActivate = nameof(CardReActivate);
    public const string MigrateCollateral = nameof(MigrateCollateral);
    public const string CardClosure = nameof(CardClosure);

    public const string ChangeLimit = nameof(ChangeLimit);
    public const string ChangeLinkedAccount = nameof(ChangeLinkedAccount);
    public const string ChangeHolderName = nameof(ChangeHolderName);
    public const string ChangeBillingAddress = nameof(ChangeBillingAddress);

    public const string CardPayment = nameof(CardPayment);
    public const string CardActivation = nameof(CardActivation);
    public const string ReplaceOnDamage = nameof(ReplaceOnDamage);
    public const string ReplaceOnLostStolen = nameof(ReplaceOnLostStolen);

    public const string CustomerProfile = nameof(CustomerProfile);

    public const string CorporateProfile = nameof(CorporateProfile);


    #region Card Replacement
    public const string Fees = nameof(Fees);
    public const string PinMailer = nameof(PinMailer);
    #endregion

    //public const string ChangeLimit1500 = nameof(ChangeLimit1500);
    //public const string ChangeLimit5000 = nameof(ChangeLimit5000);
    //public const string ChangeLimit30000 = nameof(ChangeLimit30000);


    public const string CorporateCard = nameof(CorporateCard);

    public const string StatementSingleReport = nameof(StatementSingleReport);
    public const string CreditCardStatement = nameof(CreditCardStatement);



    public const string ReplacementTrackingReport = nameof(ReplacementTrackingReport);
    public const string EndOfDayReport = nameof(EndOfDayReport);

    public const string StatisticalReport = nameof(StatisticalReport);
    public const string LoyaltyStatement = nameof(LoyaltyStatement);
    //public const string ReportLostStolen = nameof(ReportLostStolen);
    public const string SecondaryCardClosure = nameof(SecondaryCardClosure);
    public const string PrimaryCardClosure = nameof(PrimaryCardClosure);


    public const string DirectDebit = nameof(DirectDebit);
    public const string Promotions = nameof(Promotions);
    public const string EligiblePromotions = nameof(EligiblePromotions);
    public const string PromotionGroup = nameof(PromotionGroup);
    public const string GroupAttributes = nameof(GroupAttributes);
    public const string PCT = nameof(PCT);
    public const string Services = nameof(Services);
    public const string PromotionsBeneficiaries = "promotions.beneficiaries";
    public const string CardDefinitions = nameof(CardDefinitions);

    public const string CardEligibilityMatrix = nameof(CardEligibilityMatrix);
    public const string ConfigParameter = nameof(ConfigParameter);
    public const string LoyaltyPoints = nameof(LoyaltyPoints);

    public const string CardExtensions = nameof(CardExtensions);

    public const string ApplicationStatus = nameof(ApplicationStatus);

    public const string AccountBoardingRequest = nameof(AccountBoardingRequest);

    public const string MigsLoadFile = nameof(MigsLoadFile);


    #region DonNot Change
    public const string TAMCardAccess = "tamCard.access";
    //public const string TayseerCardHighPercentageLimit = "tayseerCard.highPercentageLimit";
    //public const string TayseerCardZeroPercentageLimit = "tayseerCard.zeroPercentageLimit";
    //public const string ChargeCardHighPercentageLimit = "chargeCard.highPercentageLimit";
    //public const string ChargeCardLowPercentageLimit = "chargeCard.lowPercentageLimit";
    //public const string ChargeCardException = "chargeCard.exception";
    //public const string ChargeCardMinor = "chargeCard.minor";

    #endregion
    public const string WorkflowCases = nameof(WorkflowCases);

}


