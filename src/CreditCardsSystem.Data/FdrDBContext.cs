using CreditCardsSystem.Data.Models;
using CreditCardsSystem.Domain.Entities.FDREntities;
using CreditCardsSystem.Domain.Shared.Entities.PromoEntities;
using Microsoft.EntityFrameworkCore;

namespace CreditCardsSystem.Data;

public partial class FdrDBContext : DbContext
{
    public FdrDBContext()
    {
    }

    public FdrDBContext(DbContextOptions<FdrDBContext> options)
        : base(options)
    {
    }

    public virtual DbSet<DirectdebitGenerationoption> DirectdebitGenerationoptions { get; set; }
    public virtual DbSet<StatementMaster> StatementMasters { get; set; }
    public virtual DbSet<CfuActivity> CfuActivities { get; set; }
    public virtual DbSet<CbkCard> CbkCards { get; set; }

    public virtual DbSet<RequestBoardingLog> RequestBoardingLogs { get; set; }
    public virtual DbSet<CreditReverse> CreditReverses { get; set; }
    public virtual DbSet<AreaCode> AreaCodes { get; set; }
    public virtual DbSet<CardCurrency> CardCurrencies { get; set; }
    public virtual DbSet<CardDefinition> CardDefs { get; set; }
    public virtual DbSet<Promotion> Promotions { get; set; }
    public virtual DbSet<CardDefinitionExtention> CardDefExts { get; set; }
    public virtual DbSet<Request> Requests { get; set; }
    public virtual DbSet<CardtypeEligibilityMatix> CardtypeEligibilityMatixes { get; set; }
    public virtual DbSet<PromotionBeneficiary> PromotionBeneficiaries { get; set; }
    public virtual DbSet<PromotionCard> PromotionCards { get; set; }
    public virtual DbSet<MembershipInfo> MembershipInfos { get; set; }
    public virtual DbSet<ConfigParameter> ConfigParameters { get; set; }
    public virtual DbSet<RequestParameter> RequestParameters { get; set; }
    public virtual DbSet<RequestStatus> RequestStatuses { get; set; }
    public virtual DbSet<RequestDelivery> RequestDeliveries { get; set; }
    public virtual DbSet<PreregisteredPayee> PreregisteredPayees { get; set; }
    public virtual DbSet<PreregisteredPayeeStatus> PreregisteredPayeeStatuses { get; set; }
    public virtual DbSet<PreregisteredPayeeType> PreregisteredPayeeTypes { get; set; }
    public virtual DbSet<Profile> Profiles { get; set; }
    public virtual DbSet<CreditCardsSystem.Data.Models.RequestActivity> RequestActivities { get; set; }
    public virtual DbSet<CorporateProfile> CorporateProfiles { get; set; }
    public virtual DbSet<StatementDetail> StatementDetails { get; set; }
    public virtual DbSet<Company> Companies { get; set; }
    public virtual DbSet<MembershipDeleteRequest> MembershipDeleteRequests { get; set; }
    public virtual DbSet<CreditCardsSystem.Data.Models.RequestActivityDetail> RequestActivityDetails { get; set; }
    public virtual DbSet<Relationship> Relationships { get; set; }
    public virtual DbSet<ExternalStatus> ExternalStatuses { get; set; }
    public virtual DbSet<InternalStatus> InternalStatuses { get; set; }

    //----------------------promo schema tables---------------------------

    public virtual DbSet<Collateral> Collaterals { get; set; }

    public virtual DbSet<GroupAttribute> GroupAttributes { get; set; }

    public virtual DbSet<Loyaltypointssetup> Loyaltypointssetups { get; set; }

    public virtual DbSet<Pct> Pcts { get; set; }

    public virtual DbSet<PromotionGroup> PromotionGroups { get; set; }

    public virtual DbSet<Domain.Shared.Entities.PromoEntities.RequestActivity> PromoRequestActivities { get; set; }

    public virtual DbSet<Domain.Shared.Entities.PromoEntities.RequestActivityDetail> PromoRequestActivityDetails { get; set; }

    public virtual DbSet<Service> Services { get; set; }

    public virtual DbSet<ChangeLimitHistory> ChangeLimitHistories { get; set; }
    public virtual DbSet<TayseerCreditChecking> TayseerCreditCheckings { get; set; }

    public virtual DbSet<Country> Countries { get; set; }
    public virtual DbSet<Currency> Currencies { get; set; }
    public virtual DbSet<FraudulentReason> FraudulentReasons { get; set; }
    public virtual DbSet<FraudulentStatus> FraudulentStatuses { get; set; }
    public virtual DbSet<Issuer> Issuers { get; set; }
    public virtual DbSet<LoadStatus> LoadStatuses { get; set; }
    public virtual DbSet<Master> Masters { get; set; }
    public virtual DbSet<Merchant> Merchants { get; set; }
    public virtual DbSet<MerchantGroup> MerchantGroups { get; set; }
    public virtual DbSet<BlackListedCard> BlackListedCards { get; set; }
    public virtual DbSet<Transaction> Transactions { get; set; }
    public virtual DbSet<GenerateFileRequest> GenerateFileRequests { get; set; }
    public virtual DbSet<AubCardMapping> AubCardMappings { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("FDR");

        modelBuilder.Entity<AubCardMapping>(entity =>
        {
            entity.HasKey(e => new { e.AubCardNo, e.KfhCardNo }).HasName("AUB_CARD_MAPPING_PK");

            entity.Property(e => e.IsRenewed).HasDefaultValueSql("0");
        });
        modelBuilder.HasSequence("STATEMENT_PRINT_SEQ", "VPBCD");

        modelBuilder.Entity<Country>()
             .Property(e => e.Name)
             .IsUnicode(false);

        modelBuilder.Entity<Country>()
            .Property(e => e.Alpha2Code)
            .IsUnicode(false);

        modelBuilder.Entity<Country>()
            .Property(e => e.Alpha3Code)
            .IsUnicode(false);

        modelBuilder.Entity<Country>()
            .Property(e => e.Code)
            .IsUnicode(false);

        modelBuilder.Entity<Country>()
            .HasMany(e => e.Issuers)
            .WithOne(e => e.Country).IsRequired(true)
            .HasForeignKey(e => e.CountryCode)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Currency>()
            .Property(e => e.Code)
            .IsUnicode(false);

        modelBuilder.Entity<Currency>()
            .Property(e => e.NumericCode)
            .IsUnicode(false);

        modelBuilder.Entity<Currency>()
            .Property(e => e.Name)
            .IsUnicode(false);

        modelBuilder.Entity<Currency>()
            .HasMany(e => e.Merchants)
            .WithOne(e => e.Currency).IsRequired(true)
            .HasForeignKey(e => e.CurrencyCode)
               .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<FraudulentReason>()
            .Property(e => e.Description)
            .IsUnicode(false);

        modelBuilder.Entity<FraudulentReason>()
            .HasMany(e => e.Transactions)
            .WithOne(e => e.FraudulentReason).IsRequired(false)
            .HasForeignKey(e => e.FraudulentReasonId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<FraudulentStatus>()
            .Property(e => e.Description)
            .IsUnicode(false);

        modelBuilder.Entity<FraudulentStatus>()
            .HasMany(e => e.Transactions)
            .WithOne(e => e.FraudulentStatus).IsRequired(true)
            .HasForeignKey(e => e.IsFraudulent)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Issuer>()
            .Property(e => e.Name)
            .IsUnicode(false);

        modelBuilder.Entity<Issuer>()
            .HasMany(e => e.Transactions)
            .WithOne(e => e.Issuer).IsRequired(false)
            .HasForeignKey(e => e.IssuerBin)
                .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Issuer>()
            .Property(e => e.CountryCode)
            .IsUnicode(false);

        modelBuilder.Entity<LoadStatus>()
            .Property(e => e.Description)
            .IsUnicode(false);

        modelBuilder.Entity<LoadStatus>()
            .HasMany(e => e.Masters)
            .WithOne(e => e.LoadStatus).IsRequired(true)
            .HasForeignKey(e => e.Status)
                 .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Master>()
            .Property(e => e.TotalDebitTransactionsAmount)
            .HasPrecision(38, 3);

        modelBuilder.Entity<Master>()
            .Property(e => e.TotalCreditTransactionsAmount)
            .HasPrecision(38, 3);

        modelBuilder.Entity<Master>()
            .Property(e => e.TotalPHXRejectedTransactionsAmount)
            .HasPrecision(38, 3);

        modelBuilder.Entity<Master>()
            .HasMany(e => e.Transactions)
            .WithOne(e => e.Master).IsRequired(true)
            .HasForeignKey(e => e.MasterId)
                  .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Merchant>()
            .Property(e => e.MerchantNo)
            .IsUnicode(false);

        modelBuilder.Entity<Merchant>()
            .Property(e => e.FullName)
            .IsUnicode(false);

        modelBuilder.Entity<Merchant>()
            .Property(e => e.ShortName)
            .IsUnicode(false);

        modelBuilder.Entity<Merchant>()
            .Property(e => e.CurrencyCode)
            .IsUnicode(false);

        modelBuilder.Entity<Merchant>()
            .HasMany(e => e.Transactions)
            .WithOne(e => e.Merchant).IsRequired(false)
            .HasForeignKey(e => e.MerchantNo)
               .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MerchantGroup>()
            .Property(e => e.Name)
            .IsUnicode(false);

        modelBuilder.Entity<MerchantGroup>()
            .HasMany(e => e.Merchants)
            .WithOne(e => e.Group).IsRequired(true)
            .HasForeignKey(e => e.GroupId)
                  .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<BlackListedCard>()
            .Property(e => e.CardNo)
            .IsUnicode(false);

        modelBuilder.Entity<DirectdebitGenerationoption>(entity =>
        {
            entity.HasNoKey();
            entity.Property(e => e.IsFileLoadReq).IsFixedLength();
            entity.Property(e => e.IsReversalPayment).IsFixedLength();
        });


        modelBuilder.Entity<StatementDetail>(entity =>
        {
            entity.Property(e => e.Cardholdertype).IsFixedLength();
        });


        modelBuilder.Entity<CfuActivity>(entity =>
        {
            entity.HasKey(e => e.CfuActivityId).HasName("CFU_ACTIVITY_PK");
        });

        modelBuilder.Entity<CbkCard>(entity =>
        {
            entity.HasKey(e => e.RefNo).HasName("SYS_C0011499");

            entity.Property(e => e.RefNo).IsFixedLength();
            entity.Property(e => e.CardType).IsFixedLength();
            entity.Property(e => e.ExternalStatus).IsFixedLength();
        });
        modelBuilder.Entity<RequestBoardingLog>(entity =>
        {
            entity.HasKey(e => e.ReqId).HasName("REQ_BOARDING_LOG_PK");

            entity.Property(e => e.LogDate).HasDefaultValueSql("(systimestamp)");
        });

        modelBuilder.Entity<CreditReverse>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("CREDIT_REVERSE_REQUEST_PK");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Islocked).HasDefaultValueSql("0 ");
            entity.Property(e => e.RequestDate).HasDefaultValueSql("SYSDATE \n");
        });

        modelBuilder.Entity<TayseerCreditChecking>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("TAYSEER_CREDIT_CHECKING_PK");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("sysdate");
            entity.Property(e => e.CreditCardNumber).IsFixedLength();
            entity.Property(e => e.EntryType).HasDefaultValueSql("1 ");
            entity.Property(e => e.IsInCinetBalckList).HasDefaultValueSql("0");
            entity.Property(e => e.IsInDelinquentList).HasDefaultValueSql("0");
            entity.Property(e => e.IsInKfhBlackList).HasDefaultValueSql("0");
            entity.Property(e => e.IsRetiree).HasDefaultValueSql("0 ");
            entity.Property(e => e.IsThereAguarantor).HasDefaultValueSql("0 ");
            entity.Property(e => e.IsThereAnException).HasDefaultValueSql("0");
            entity.Property(e => e.Status).HasDefaultValueSql("2");
        });

        modelBuilder.Entity<ChangeLimitHistory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("CHANGE_LIMIT_HISTORY_PK");
            entity.Property(e => e.Id).HasDefaultValueSql("0 ");
            entity.Property(e => e.IsTempLimitChange).IsFixedLength();
            entity.Property(e => e.LogDate).HasDefaultValueSql("sysdate ");
        });

        modelBuilder.Entity<CreditCardsSystem.Data.Models.RequestActivityDetail>(entity =>
        {
            entity.HasKey(e => new { e.RequestActivityId, e.Paramter });
            entity.HasOne(x => x.RequestActivityDetailNavigation).WithMany(x => x.Details).HasForeignKey(x => x.RequestActivityId);
        });

        modelBuilder.Entity<Pct>(entity =>
        {
            entity.HasKey(e => e.PctId).HasName("PCT_PK");

            entity.Property(e => e.CreateDate).HasDefaultValueSql("sysdate ");
            entity.Property(e => e.IsStaff).HasDefaultValueSql("0");
        });

        modelBuilder.Entity<MembershipDeleteRequest>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.RequestDate).HasDefaultValueSql("sysdate ");
            entity.Property(e => e.Status).HasDefaultValueSql("0 ");
        });

        modelBuilder.Entity<Company>(entity =>
        {
            entity.HasKey(e => new { e.CompanyId, e.CardType }).HasName("PK_COMPANY1");

            entity.Property(e => e.Annual).IsFixedLength();
            entity.Property(e => e.Bonus).IsFixedLength();
        });

        modelBuilder.Entity<CorporateProfile>(entity =>
        {
            entity.HasKey(e => e.CorporateCivilId).HasName("CORPORATE_PROFILE_PK");
        });

        modelBuilder.Entity<CreditCardsSystem.Data.Models.RequestActivity>(entity =>
        {
            entity.HasKey(e => e.RequestActivityId).HasName("REQUEST_ACTIVITY_PK");
            entity.Property(e => e.RequestActivityId).ValueGeneratedOnAdd();
        });
        modelBuilder.Entity<Profile>(entity =>
        {
            entity.Property(e => e.CivilId).IsFixedLength();
            entity.Property(e => e.Country).HasDefaultValueSql("40");
        });

        modelBuilder.Entity<PreregisteredPayeeStatus>(entity =>
        {
            entity.HasKey(e => e.StatusId).HasName("PK_PREREG_PAYEE_STATUS");
        });

        modelBuilder.Entity<PreregisteredPayeeType>(entity =>
        {
            entity.HasKey(e => e.TypeId).HasName("PK_PREREG_PAYEE_TYPE");
        });

        modelBuilder.Entity<PreregisteredPayee>(entity =>
        {
            entity.HasKey(e => new { e.CivilId, e.CardNo }).HasName("PK_PREREG_PAYEE");
            entity.Property(e => e.CivilId).IsFixedLength();
        });

        modelBuilder.Entity<RequestDelivery>(entity =>
        {
            entity.HasKey(e => e.RequestDeliveryId).HasName("REQUEST_DELIVERY_PK");

            entity.Property(e => e.RequestDeliveryId).ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<RequestStatus>(entity =>
        {
            entity.HasKey(e => e.StatusId).HasName("REQUEST_STATUS_PK");
        });

        modelBuilder.Entity<RequestParameter>(entity =>
        {
            entity.HasKey(e => new { e.ReqId, e.Parameter, e.Value });
            entity.HasOne(x => x.RequestParameterNavigation).WithMany(x => x.Parameters).HasForeignKey(x => x.ReqId);
        });

        modelBuilder.Entity<ConfigParameter>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("CONFIG_PARAMETERS_PK");
        });

        modelBuilder.Entity<MembershipInfo>(entity =>
        {
            entity.Property(e => e.CivilId).ValueGeneratedOnAdd();
            entity.Property(e => e.CompanyId).ValueGeneratedOnAdd();
            entity.Property(e => e.ClubMembershipId).ValueGeneratedOnAdd();
            entity.Property(e => e.DateCreated).ValueGeneratedOnAdd();
            entity.Property(e => e.DateUpdated).ValueGeneratedOnAdd();
            entity.Property(e => e.FileName).ValueGeneratedOnAdd();
        });


        modelBuilder.Entity<AreaCode>(entity =>
        {
            entity.HasKey(e => e.AreaId).HasName("SYS_IOT_TOP_24873");
        });

        modelBuilder.Entity<CardDefinition>(entity =>
        {
            entity.HasKey(e => e.CardType).HasName("CARD_DEF_PK31048589907375");
        });

        modelBuilder.Entity<CardDefinitionExtention>(entity =>
        {
            entity.HasOne(d => d.CardTypeNavigation).WithMany(p => p.CardDefExts)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CARD_DEF_EXT");
        });

        modelBuilder.Entity<CreditCardsSystem.Data.Models.CardtypeEligibilityMatix>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("CARDTYPE_ELIGIBILITY_MATIX_PK");

            entity.Property(e => e.Islocked).HasDefaultValueSql("0");
        });

        modelBuilder.Entity<Promotion>(entity =>
        {
            entity.HasKey(e => e.PromotionId).HasName("PK_PROMOTIONS_PRODUCTS");
        });

        modelBuilder.Entity<PromotionBeneficiary>(entity =>
        {
            entity.HasKey(e => new { e.CivilId, e.PromotionId }).HasName("PK_PROMO_BENEFS");

            entity.Property(e => e.CivilId).IsFixedLength();
            entity.Property(e => e.ApplicationDate).HasDefaultValueSql("sysdate ");
        });

        modelBuilder.Entity<PromotionCard>(entity =>
        {
            entity.HasKey(e => e.PromotionCardId).HasName("PK_PROMO_CARDS");

            entity.Property(e => e.PromotionCardId).ValueGeneratedNever();

            entity.HasOne(d => d.Promotion).WithMany(p => p.PromotionCards).HasConstraintName("FK_PROMO_CARDS_PROMO_PRODUCTS");
        });

        modelBuilder.Entity<Request>(entity =>
        {
            entity.HasKey(e => e.RequestId).HasName("SYS_C0011536");

            entity.Property(e => e.CardNo).IsFixedLength();
            entity.Property(e => e.CivilId).IsFixedLength();
            entity.Property(e => e.Expiry).HasDefaultValueSql("0000");
            entity.Property(e => e.ServicePeriod).HasDefaultValueSql("0 ");
        });

        modelBuilder.Entity<StatementDetail>(entity =>
        {
            entity.HasKey(e => new { e.AccountNo, e.RecordSequence, e.TransCardNo, e.StatementDate });

            entity.ToTable("STATEMENT_DETAILS", "VPBCD");

            entity.HasIndex(e => e.CategoryCode, "IX_SD_CATEGORY_CODE");

            entity.HasIndex(e => e.TransDescription, "IX_SD_TRANS_DESCRIPTION");

            entity.HasIndex(e => new { e.AccountNo, e.StatementDate }, "IX_STMNT_FDACCTNO_STMNTDATE");

            entity.Property(e => e.AccountNo)
                .HasMaxLength(19)
                .IsUnicode(false)
                .HasColumnName("ACCOUNT_NO");
            entity.Property(e => e.RecordSequence)
                .HasPrecision(11)
                .HasColumnName("RECORD_SEQUENCE");
            entity.Property(e => e.TransCardNo)
                .HasMaxLength(19)
                .IsUnicode(false)
                .HasColumnName("TRANS_CARD_NO");
            entity.Property(e => e.StatementDate)
                .HasMaxLength(4)
                .IsUnicode(false)
                .HasColumnName("STATEMENT_DATE");
            entity.Property(e => e.BankNetDate)
                .HasColumnType("DATE")
                .HasColumnName("BANK_NET_DATE");
            entity.Property(e => e.BankNetNo)
                .HasMaxLength(9)
                .IsUnicode(false)
                .HasColumnName("BANK_NET_NO");
            entity.Property(e => e.CardType)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("CARD_TYPE");
            entity.Property(e => e.Cardholdertype)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("CARDHOLDERTYPE");
            entity.Property(e => e.Category)
                .HasPrecision(1)
                .HasColumnName("CATEGORY");
            entity.Property(e => e.CategoryCode)
                .HasPrecision(5)
                .HasColumnName("CATEGORY_CODE");
            entity.Property(e => e.CategoryCodeSign)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("CATEGORY_CODE_SIGN");
            entity.Property(e => e.DuplicateStmntIndic)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("DUPLICATE_STMNT_INDIC");
            entity.Property(e => e.ForeignCurrency)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasColumnName("FOREIGN_CURRENCY");
            entity.Property(e => e.ForeignCurrencyAmount)
                .HasColumnType("NUMBER(21,6)")
                .HasColumnName("FOREIGN_CURRENCY_AMOUNT");
            entity.Property(e => e.ForeignCurrencyDesc)
                .HasMaxLength(40)
                .IsUnicode(false)
                .HasColumnName("FOREIGN_CURRENCY_DESC");
            entity.Property(e => e.InterchangeFee)
                .HasColumnType("NUMBER(19,6)")
                .HasColumnName("INTERCHANGE_FEE");
            entity.Property(e => e.MerchantOrg)
                .HasPrecision(3)
                .HasColumnName("MERCHANT_ORG");
            entity.Property(e => e.MerchantOrgSign)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("MERCHANT_ORG_SIGN");
            entity.Property(e => e.MerchantStore)
                .HasPrecision(9)
                .HasColumnName("MERCHANT_STORE");
            entity.Property(e => e.MerchantStoreSign)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("MERCHANT_STORE_SIGN");
            entity.Property(e => e.OrganizationLogoNo)
                .HasPrecision(6)
                .HasColumnName("ORGANIZATION_LOGO_NO");
            entity.Property(e => e.Product)
                .HasPrecision(5)
                .HasColumnName("PRODUCT");
            entity.Property(e => e.ProductGroupSign)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("PRODUCT_GROUP_SIGN");
            entity.Property(e => e.RecordSequenceSign)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("RECORD_SEQUENCE_SIGN");
            entity.Property(e => e.RecordTypeIndic)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("RECORD_TYPE_INDIC");
            entity.Property(e => e.RelationshipNo)
                .HasMaxLength(19)
                .IsUnicode(false)
                .HasColumnName("RELATIONSHIP_NO");
            entity.Property(e => e.ReqId)
                .HasColumnType("NUMBER(28)")
                .HasColumnName("REQ_ID");
            entity.Property(e => e.StmntExchangeRate)
                .HasColumnType("NUMBER(14,6)")
                .HasColumnName("STMNT_EXCHANGE_RATE");
            entity.Property(e => e.TransAmount)
                .HasColumnType("NUMBER(21,6)")
                .HasColumnName("TRANS_AMOUNT");
            entity.Property(e => e.TransAuthCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("TRANS_AUTH_CODE");
            entity.Property(e => e.TransCode)
                .HasPrecision(5)
                .HasColumnName("TRANS_CODE");
            entity.Property(e => e.TransCodeSign)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("TRANS_CODE_SIGN");
            entity.Property(e => e.TransDescription)
                .HasMaxLength(40)
                .IsUnicode(false)
                .HasColumnName("TRANS_DESCRIPTION");
            entity.Property(e => e.TransEffectiveDate)
                .HasColumnType("DATE")
                .HasColumnName("TRANS_EFFECTIVE_DATE");
            entity.Property(e => e.TransLogicModule)
                .HasPrecision(3)
                .HasColumnName("TRANS_LOGIC_MODULE");
            entity.Property(e => e.TransPlanNo)
                .HasPrecision(5)
                .HasColumnName("TRANS_PLAN_NO");
            entity.Property(e => e.TransPlanNoSign)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("TRANS_PLAN_NO_SIGN");
            entity.Property(e => e.TransPostDate)
                .HasColumnType("DATE")
                .HasColumnName("TRANS_POST_DATE");
            entity.Property(e => e.TransRefNo)
                .HasMaxLength(23)
                .IsUnicode(false)
                .HasColumnName("TRANS_REF_NO");
            entity.Property(e => e.TransTicketNo)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasColumnName("TRANS_TICKET_NO");
            entity.Property(e => e.TransType)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("TRANS_TYPE");
            entity.Property(e => e.VisaTransId)
                .HasPrecision(15)
                .HasColumnName("VISA_TRANS_ID");
            entity.Property(e => e.ZipSortCode)
                .HasMaxLength(4)
                .IsUnicode(false)
                .HasColumnName("ZIP_SORT_CODE");
        });

        modelBuilder.HasSequence("CHANGE_LIMIT_HISTORY_SEQ");
        modelBuilder.HasSequence("CREDIT_CARD_SOL_SEQ");
        modelBuilder.HasSequence("STATEMENT_PRINT_SEQ", "VPBCD");
        modelBuilder.HasSequence("CARDTYPE_ELIGIBILITY_MATIX_SEQ", "PROMO");
        modelBuilder.HasSequence("CONFIG_PARAMETERS_SEQ", "PROMO");
        modelBuilder.HasSequence("PCT_SEQ", "PROMO");
        modelBuilder.HasSequence("PROMOTION_CARD_SEQ", "PROMO");
        modelBuilder.HasSequence("PROMOTION_CLASS_SEQ", "PROMO");
        modelBuilder.HasSequence("PROMOTION_GROUP_SEQ", "PROMO");
        modelBuilder.HasSequence("PROMOTION_PRODUCT_SEQ", "PROMO");
        modelBuilder.HasSequence("REQUEST_ACTIVITY_DETAILS_SEQ", "PROMO");
        modelBuilder.HasSequence("REQUEST_ACTIVITY_SEQ", "PROMO");
        modelBuilder.HasSequence("REQUEST_PARAMTERS_LOOKUP_SEQ");
        modelBuilder.HasSequence("REQUEST_DELIVERY_SEQ");
        modelBuilder.HasSequence("SERVICES_SEQ", "PROMO");
        modelBuilder.HasSequence("SEQ_TAYSEER_CREDIT_CHECKING");

        modelBuilder.HasSequence("SEQ");

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
