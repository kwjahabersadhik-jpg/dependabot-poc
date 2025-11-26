using CreditCardsSystem.Domain.Models.Promotions;
using System.ComponentModel.DataAnnotations.Schema;

namespace CreditCardsSystem.Domain.Models.CardIssuance
{
    public class RequestParameterDto : IDisposable
    {
        [Column("CANCEL_CHANGE_LIMIT_REQUEST")]
        public string? CancelChangeLimitRequest { get; set; }

        [Column("ISSUING_OPTION")]
        public string? Collateral { get; set; }

        [Column("EMPLOYMENT")]
        public string? Employment { get; set; }

        [Column("PCT_FLAG")]
        public string? PCTFlag { get; set; }

        [Column("bcdFlag")]
        public string? BCDFlag { get; set; }

        [Column("MARGIN_AMOUNT")]
        public string? MarginAmount { get; set; }

        [Column("SO_START_DATE")]
        public string? SoStartDate { get; set; }

        [Column("OLD_CARD_NO")]
        public string? OldCardNumberEncrypted { get; set; }

        [Column("MIGRATED_BY_ID")]
        public string? MigratedById { get; set; }

        [Column("CURRENT_BALANCE")]
        public string? CurrentBalance { get; set; }

        [Column("Service_No_Months")]
        public string? ServiceNumberInMonths { get; set; }

        [Column("collateralId")]
        public string? CollateralId { get; set; }

        [Column("earlyClosureMonths")]
        public string? EarlyClosureMonths { get; set; }

        [Column("CLUB_NAME")]
        public string? ClubName { get; set; }

        [Column("KFH_CUSTOMER")]
        public string? KFHCustomer { get; set; }

        [Column("CHANGE_LIMIT_REQUEST")]
        public string? ChangeLimitRequest { get; set; }

        [Column("MIGRATOR_BRANCH_NAME")]
        public string? MigratorBranchName { get; set; }

        [Column("earlyClosureFees")]
        public string? EarlyClosureFees { get; set; }

        [Column("pctId")]
        public string? PCTId { get; set; }

        [Column("CardType")]
        public string? CardType { get; set; }

        [Column("SO_CREATE_DATE")]
        public string? SoCreatedDate { get; set; }

        [Column("RELATION")]
        public string? Relation { get; set; }

        [Column("PRIMARY_CARD_NO")]
        public string? PrimaryCardNumber { get; set; }

        [Column("PRIMARY_CIVILID")]
        public string? PrimaryCardCivilId { get; set; }

        [Column("PRIMARY_CARD_HOLDER_NAME")]
        public string? PrimaryCardHolderName { get; set; }

        [Column("TARGET_ISSUING_OPTION")]
        public string? TargetIssuingOption { get; set; }

        [Column("T3MAXLIMIT")]
        public string? T3MaxLimit { get; set; }

        [Column("T12MAXLIMIT")]
        public string? T12MaxLimit { get; set; }

        [Column("CINET")]
        public string? CINET { get; set; }

        [Column("IS_VIP")]
        public string? IsVIP { get; set; }

        [Column("Service_No")]
        public string? ServiceNumber { get; set; }

        [Column("earlyClosurePercentage")]
        public string? EarlyClosurePercentage { get; set; }

        [Column("promotionName")]
        public string? PromotionName { get; set; }

        [Column("SECONDARY_CARD_NO")]
        public string? SecondaryCardNumber { get; set; }

        [Column("so_count")]
        public string? SoCount { get; set; }

        [Column("MIGRATED_BY_NAME")]
        public string? MigratedByName { get; set; }

        [Column("DEPOSIT_AMOUNT")]
        public string? DepositAmount { get; set; }

        [Column("DEPOSIT_NUMBER")]
        public string? DepositNumber { get; set; }

        [Column("MARGIN_ACCOUNT_NO")]
        public string? MarginAccountNumber { get; set; }

        [Column("MARGIN_REFERENCE_NO")]
        public string? MarginTransferReferenceNumber { get; set; }

        [Column("CLUB_MEMBERSHIP_ID")]
        public string? ClubMembershipId { get; set; }

        [Column("COMPANY_NAME")]
        public string? CompanyName { get; set; }

        [Column("IsSupplementaryOrPrimaryChargeCard")]
        public string? IsSupplementaryOrPrimaryChargeCard { get; set; }

        [Column("ISSUING_CHANNEL")]
        public string? IssuingChannel { get; set; }

        [Column("MIGRATOR_BRANCH_ID")]
        public string? MigratorBranchId { get; set; }

        [Column("SELLER_GENDER_CODE")]
        public string? SellerGenderCode { get; set; }

        [Column("SO_EXPIRY_DATE")]
        public string? SOExpiryDate { get; set; }

        [Column("PRIMARY_CARD_REQUEST_ID")]
        public string? PrimaryCardRequestId { get; set; }

        [Column("CINET_ID")]
        public string? CINET_ID { get; set; }

        [Column("corporate_civil_id")]
        public string? CorporateCivilId { get; set; }

        [Column("KFH_STAFF_ID")]
        public string? KFHStaffID { get; set; }

        [Column("DEPOSIT_ACCOUNT_NO")]
        public string? DepositAccountNumber { get; set; }

        [Column("CUSTOMER_CLASS_CODE")]
        public string? CustomerClassCode { get; set; }

        [Column("IssuePinMailer")]
        public string? IssuePinMailer { get; set; }

        [Column("OLD_FD_ACCT_NO")]
        public string? OldFixedDepositAccountNumber { get; set; }

        //[Column("PRIMARY_REQ_ID")]
        //public string? PrimaryRequestId { get; set; }

        [Column("PENDING_COLLATERAL_MIGRATION")]
        public string? PendingCollateralMigration { get; set; }

        [Column("SOID_FOR_MARGIN")]
        public string? StandingOrderIDForMargin { get; set; }

        [Column("KD_GUARANTEE_SALARY_ACCT_FOR_USD_CARD")]
        public string? KDGuaranteeSalaryAccountForUSDCard { get; set; }

        [Column("DEPOSIT_REFERNCE_NO")]
        public string? DepositReferenceNumber { get; set; }

        [Column("ACTUAL_COLLATETAL")]
        public string? ActualCollateral { get; set; }

        [Column("cmt_type")]
        public string? CommitmentType { get; set; }

        [Column("amt")]
        public string? Amount { get; set; }

        [Column("commitment_no")]
        public string? CommitmentNo { get; set; }

        [Column("mat_dt")]
        public string? MaturityDate { get; set; }

        [Column("status")]
        public string? Status { get; set; }

        [Column("undisbursed")]
        public string? Undisbursed { get; set; }

        [Column("Application")]
        public string? Application { get; set; } = "Aurora";

        [Column("PREVIOUS_ISSUING_OPTION")]
        public string? PreviousCollateral { get; set; }

        [Column("MIGRATED_DATE")]
        public string? MigratedDate { get; set; }

        [Column("MAXLIMIT")]
        public string? MaxLimit { get; set; }

        [Column("IsCBKRulesViolated")]
        public string IsCBKRulesViolated { get; set; }
        public string TotalFixedDuties { get; set; }

        [Column("WorkFlowInstanceId")]
        public string WorkFlowInstanceId { get; set; }

        [Column("AUB_BLOCK_ACCOUNT_NO")]
        public string? AubBlockAccountNumber { get; set; }

        [Column("DeliveryOption")]
        public string? DeliveryOption { get; set; }

        [Column("DeliveryBranchId")]
        public string? DeliveryBranchId { get; set; }

        [Column("VoucherAmount")]
        public string? VoucherAmount { get; set; }

        public void Dispose()
        {
            GC.Collect(3);
            GC.SuppressFinalize(this);
        }

        public void SetPromotion(CreditCardPromotionDto promotion)
        {
            ServiceNumber = promotion?.serviceNo.ToString();
            ServiceNumberInMonths = promotion?.numberOfMonths.ToString();
            CollateralId = promotion?.collateralId.ToString();
            EarlyClosureFees = promotion?.earlyClosureFees;
            EarlyClosureMonths = promotion?.earlyClosureMonths;
            EarlyClosurePercentage = promotion?.earlyClosurePercentage;
            PCTId = promotion?.pctId;
            BCDFlag = promotion?.flag;
            PromotionName = promotion?.promoName;
            PCTFlag = promotion?.pctFlag;
        }
    }
}
