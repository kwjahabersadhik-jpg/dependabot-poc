using CreditCardsSystem.Domain.Attributes;
using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Models.CardOperation;
using CreditCardsSystem.Domain.Models.CoBrand;
using Newtonsoft.Json;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace CreditCardsSystem.Domain.Models.CardIssuance;

public class CardIssueResponse
{
    public decimal RequestId { get; set; }
    public SupplementaryCardIssueResponse? SupplementaryCardResponse { get; set; }
}

public class SupplementaryCardIssueResponse
{
    public List<string> SuccessCards { get; set; } = new();
    public List<ApiResponseModel<string>> FailedCards { get; set; } = new();
}

public class Seller : IDisposable
{
    [Range(typeof(bool), "true", "true", ErrorMessage = "You must confirm the seller name")]
    public bool IsConfirmedSellerId { get; set; }

    [Required(ErrorMessage = "You must enter SellerId and confirm seller name")]
    public long? SellerId { get; set; } = null;

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
public class CardIssueRequest : ValidateModel<CardIssueRequest>
{
    [Required]
    public int BranchId { get; set; }



    //[Required]
    //public int RequestedLimit { get; set; }

    //public decimal ApproveLimit { get; set; }


    [ValidateComplexType]
    public Seller Seller { get; set; } = new();

    [ValidateComplexType]
    public IssueDetailsModel IssueDetailsModel { get; set; } = new();

    [ValidateComplexType]
    public PromotionModel? PromotionModel { get; set; }

    [ValidateComplexType]
    public List<SupplementaryModel>? SupplementaryModel { get; set; }

    [ValidateComplexType]
    public BillingAddressModel BillingAddressModel { get; set; } = new();

    [ValidateComplexType]
    public CustomerProfileInfo Customer { get; set; } = new();

    //public DepositInfo? DepositInfo { get; set; } = new();

    public Installments? Installments { get; set; } = new();


    public string? Remark { get; set; } = string.Empty;

    public string? KFHStaffID { get; set; }

    public bool PinMailer { get; set; } = false; // Move to billing address model
    public bool IsCBKRulesViolated { get; set; }
    public bool IsWithPrimaryCard { get; set; } = false;
    public bool IsSupplementaryRequest
    {
        get { return SupplementaryModel?.Count == 1; }
    }

    public string? FinancialPositionMessage { get; set; }

}

public class CorporateProfileRequest
{
    [Required]
    public string CorporateCivilId { get; set; } = null!;
}



public class IssueDetailsModel : IDisposable
{
    [ValidateComplexType]
    public CardInfo Card { get; set; } = new();

    [ValidateComplexType]
    public CoBrand? CoBrand { get; set; }

    [ValidateComplexType]
    public CorporateProfileRequest? CorporateProfile { get; set; }


    public DeliveryOption DeliveryOption { get; set; } = DeliveryOption.COURIER;
    public int? DeliveryBranchId { get; set; }

    public Collateral? ActualCollateral { get; set; }
    public Collateral? Collateral { get; set; }

    public bool IsExceptional { get; set; }
    public bool IsCBKRulesViolated { get; set; }


    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
public class BillingAddressModel : IDisposable
{
    public string FaxReference { get; set; } = string.Empty;
    [Required(ErrorMessage = "Area is Required")]
    public string City { get; set; } = string.Empty;

    [Required(ErrorMessage = "Postal Code (ZIP) is Required")]
    [Range(1, 99999, ErrorMessage = "Postal Code (ZIP) cannot be more than 5 digits")]
    [RegularExpression(@"^(\d{5})$", ErrorMessage = "Not a valid Postal Code (ZIP)")]
    public int? PostalCode { get; set; } = 0;

    [Range(0, 999999, ErrorMessage = "P.O.Box cannot be more than 6 digits")]
    public int? PostOfficeBoxNumber { get; set; }

    [Required(ErrorMessage = "Full Address is Required")]

    [StringLength(40, ErrorMessage = "Full Address should not exceed {1} characters")]
    [ArabicValidator]
    public string Street { get; set; } = string.Empty;

    [DataType(DataType.PhoneNumber)]
    [Range(1, 99999999, ErrorMessage = "Home Phone Number cannot be more than 8 digits")]
    [RegularExpression(@"^\d{8}$", ErrorMessage = "number should be 8 digit")]
    [Required]
    public long? HomePhone { get; set; }

    [Range(1, 99999999, ErrorMessage = "Home Phone Number cannot be more than 8 digits")]
    [DataType(DataType.PhoneNumber)]
    [RegularExpression(@"^\d{8}$", ErrorMessage = "number should be 8 digit")]
    [Required]
    public long? WorkPhone { get; set; }

    [Required(ErrorMessage = "Mobile Required")]
    [Range(1, 99999999, ErrorMessage = "Mobile cannot be more than 8 digits")]
    [DataType(DataType.PhoneNumber)]
    [RegularExpression(@"^\d{8}$", ErrorMessage = "number should be 8 digit")]
    public long? Mobile { get; set; }

    [StringLength(40, ErrorMessage = "{0} should not exceed {1} characters")]
    [ArabicValidator]
    public string AddressLine1 { get { return PostalCode + " " + City; } }

    [StringLength(40, ErrorMessage = "{0} should not exceed {1} characters")]
    [ArabicValidator]

    public string AddressLine2
    {
        get
        {
            StringBuilder addressLine = new();

            addressLine.Append("TEL:( " + HomePhone);

            if (WorkPhone is not null && WorkPhone != 0)
                addressLine.Append("/" + WorkPhone);

            if (Mobile is not null && Mobile != 0)
                addressLine.Append("/" + Mobile);

            addressLine.Append(" )");

            return addressLine.ToString();
        }
    }

    [NotMapped] public string? Block { get; set; } = string.Empty;

    [StringLength(11, ErrorMessage = "Street should not exceed {1} characters")]
    [NotMapped]
    public string? StreetNo_NM { get; set; } = string.Empty;
    [NotMapped] public string? Jada { get; set; } = string.Empty;
    [NotMapped] public string? House { get; set; } = string.Empty;

    [JsonIgnore]
    public string? AreaId { get; set; } = string.Empty;



    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    public override string ToString()
    {
        return $"POBox: {PostOfficeBoxNumber} City:{City} Post Code:{PostalCode} Street:{Street}";
    }

    public void splitAddress()
    {
        int blockIndex = Street.IndexOf("Blk");
        int streetIndex = Street.IndexOf("st");
        int jdaIndex = Street.IndexOf("Jda");
        int houseIndex = Street.IndexOf("House");

        if (blockIndex >= 0)
            Block = Street.Substring(blockIndex += 3, streetIndex - 3).Trim();

        if (streetIndex >= 0)
            StreetNo_NM = Street.Substring(streetIndex += 2, jdaIndex - streetIndex).Trim();

        if (jdaIndex >= 0)
            Jada = Street.Substring(jdaIndex += 3, houseIndex - jdaIndex).Trim();

        if (houseIndex >= 0)
            House = Street[(houseIndex += 5)..].Trim();
    }

}
public class PromotionModel
{
    [Required(ErrorMessage = "You must choose any Promotion")]
    public int? PromotionId { get; set; }
}
public class SupplementaryModel
{
    [Required]
    public string CivilId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mobile Required")]
    [DataType(DataType.PhoneNumber)]
    [RegularExpression(@"^[4|5|6|9]{1}\d{7}$", ErrorMessage = "Not a valid phone number")]
    public long? Mobile { get; set; } = null;

    public string? FirstName { get; set; } = null;
    public string? MiddleName { get; set; } = null;
    public string? LastName { get; set; } = null;

    [Required(ErrorMessage = "Holder Name Required")]
    [RegularExpression(@"^[a-zA-Z\s]{1,26}$", ErrorMessage = "Holder name must contain alpabetic characters and spaces, with a maximum of 26 characters")]
    public string HolderName { get; set; }

    [Required(ErrorMessage = "Relation is Required")] public decimal RelationId { get; set; }
    [Required(ErrorMessage = "Relation is Required")] public string RelationName { get; set; }
    public string? Remarks { get; set; } = string.Empty;
    public int? KFHCustomer { get; set; } = 0;
    public int? CustomerClassCode { get; set; } = 0;
    public string PrimaryCivilID { get; set; } = null!;
    public string PrimaryCardNo { get; set; } = null!;
    public string PrimaryCardRequestID { get; set; } = null!;
    public string PrimaryCardHolderName { get; set; } = null!;
    public int? PromotionId { get; set; }
    public decimal SpendingLimit { get; set; }
}
public class SupplementaryEditModel : SupplementaryModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "SpendingLimit is Required")]
    [Range(1, double.MaxValue, ErrorMessage = "SpendingLimit is Required")]
    public new decimal SpendingLimit { get; set; }
    public new string? RelationName { get; set; }
}
public class CoBrand
{
    [Required(ErrorMessage = "You must enter MemberShip Id")]
    [DisplayName("MemberShipId")]
    [RegularExpression(@"^[1-9]{1}[0-9]{8}$", ErrorMessage = "Membership ID is not available, please try a different one (ID should be 9 digits)")]
    [SequentialValidator]
    public int? MemberShipId { get; set; } = null;

    public CompanyLookup Company { get; set; }

    public string? OldCivilId { get; set; }

    public string? ReasonForDeleteRequest { get; set; }

    [Range(typeof(bool), "true", "true", ErrorMessage = "You must use a valid membership Id")]
    public bool IsValidMemberShipIdToIssueCard { get; set; } = true;
    public int? NewMemberShipId { get; set; }
}
public class CustomerProfileInfo
{
    [Required]
    public string CivilId { get; set; } = string.Empty;

    public int BranchId { get; set; }

    public string? CustomerClassCode { get; set; }

    public bool IsRetiredEmployee { get; set; } = false;

    public bool IsVIP { get; set; } = false;

    /// <summary>
    /// Auto bind by employee number and account number
    /// </summary>
    public int CustomerBranchId { get; set; }
    public decimal? Salary { get; set; } = 0;

    public decimal? TotalCinet { get; set; }
    public int? CinetId { get; set; }
}
public class CardInfo
{
    public decimal? DepositAmount;

    [Required]
    public int ProductId { get; set; }

    public string? DebitAccountNumber { get; set; }

    //public string? DepositAccountNumber { get; set; }
    //public string? MarginAccountNumber { get; set; }
    ////public decimal? MarginAmount { get; set; }
    //public string? SalaryAccountNumber { get; set; }

    public string? CollateralAccountNumber { get; set; }

    public string? FDAccountNumber { get; set; }

    public bool IsForeignCurrencyCard { get; set; } = false;

    [Range(typeof(bool), "true", "true", ErrorMessage = "You must accept currency rate")]
    [Display(Name = "Agree to Currency Rate *")]
    public bool IsAgreedToCurrencyRate { get; set; } = false;

    [Display(Name = "Required Limit")]
    public decimal RequiredLimit { get; set; }
    public decimal T3MaxLimit { get; set; }
    public decimal T12MaxLimit { get; set; }
    public string? DepositNumber { get; set; }
    public decimal MaxLimit { get; set; }
    public string? Expiry { get; set; }
    public DateTime? ApproveDate { get; set; }
    public decimal MaxPercentage { get; internal set; }
}
public record Installments(int MurabahaInstallments = 0, int RealEstateInstallment = 0);
public record DepositInfo(string DepositNumber = "0", int DepositAmount = 0);

public class BaseCardRequest : ValidateModel<BaseCardRequest>
{
    [Required(ErrorMessage = "ProductId is missing")]
    public int ProductId { get; set; }

    [RegularExpression(@"^\d{1,12}", ErrorMessage = "Invalid DebitAccountNumber")]
    public string? DebitAccountNumber { get; set; }

    [Required(ErrorMessage = "You must enter SellerId and confirm seller name")]
    public long? SellerId { get; set; } = null;

    [Range(typeof(bool), "true", "true", ErrorMessage = "You must confirm the seller name")]
    public bool IsConfirmedSellerId { get; set; }
    public bool PinMailer { get; set; } = false; // Move to billing address model

    public int BranchId { get; set; }

    [ValidateComplexType]
    public CustomerProfileInfo Customer { get; set; } = new();

    [ValidateComplexType]
    public BillingAddressModel BillingAddress { get; set; }

    [ValidateComplexType]
    public PromotionModel? PromotionModel { get; set; }

    [ValidateComplexType]
    public CoBrand? CoBrand { get; set; }
    public string? Remark { get; set; }

    public DeliveryOption DeliveryOption { get; set; } = DeliveryOption.COURIER;
    public int? DeliveryBranchId { get; set; }

    public Installments? Installments { get; set; } = new();

}
public class PrepaidCardRequest : BaseCardRequest
{

}
public class ChargeCardRequest : BaseCardRequest
{
    public Collateral? Collateral { get; set; }
    public Collateral? ActualCollateral { get; set; }

    /// <summary>
    /// This value will be deposit account number if the collateral is "AGAINST_DEPOSIT", the same way margin account number for collateral against margin
    /// </summary>
    public string? CollateralAccountNumber { get; set; }

    [Display(Name = "Required Limit")]
    public decimal RequiredLimit { get; set; }
    public bool IsCBKRulesViolated { get; set; }
    public string? DepositNumber { get; set; }
    public List<SupplementaryModel>? SupplementaryCards { get; set; }

    [JsonIgnore]
    public bool IsValidCoBrand => CoBrand is not null && CoBrand.Company.CardType > 0;

    public decimal MaxLimit { get; set; }
    public decimal TotalFixedDuties { get; set; }
}

public class TayseerCardRequest : BaseCardRequest
{
    public Collateral? Collateral { get; set; }
    public Collateral? ActualCollateral { get; set; }

    /// <summary>
    /// This value will be deposit account number if the collateral is "AGAINST_DEPOSIT", the same way margin account number for collateral against margin
    /// </summary>
    public string? CollateralAccountNumber { get; set; }

    [Display(Name = "Required Limit")]
    public decimal RequiredLimit { get; set; }
    public string? DepositNumber { get; set; }
    public ReplaceCard? ReplaceCard { get; set; }
    public decimal T3MaxLimit { get; set; }
    public decimal T12MaxLimit { get; set; }
    public decimal TotalFixedDuties { get; set; }
    public bool IsCBKRulesViolated { get; set; }
}

public class CorporateCardRequest : BaseCardRequest
{
    [Required]
    public string CorporateCivilId { get; set; } = null!;

    [Display(Name = "Required Limit")]
    [Required]
    public decimal RequiredLimit { get; set; }
}


public record AddressTypeItem(AddressType Value, string Text);