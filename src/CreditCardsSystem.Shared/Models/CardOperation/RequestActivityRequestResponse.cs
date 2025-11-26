using CreditCardsSystem.Domain.Attributes;
using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Domain.Models.Card;
using CreditCardsSystem.Domain.Models.CoBrand;
using Newtonsoft.Json;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CreditCardsSystem.Domain.Models.CardOperation;

public class CreditCardCustomerProfileResponse
{
    public string civilID { get; set; }
    public string arabicName { get; set; }
    public string area { get; set; }
    public DateTime birth { get; set; }
    public bool birthSpecified { get; set; }
    public string blockNo { get; set; }
    public string buildNo { get; set; }
    public int country { get; set; }
    public bool countrySpecified { get; set; }
    public string durationService { get; set; }
    public string email { get; set; }
    public string employerArea { get; set; }
    public string employerDepartment { get; set; }
    public string employerName { get; set; }
    public string employerSection { get; set; }
    public object fileNo { get; set; }
    public string firstName { get; set; }
    public string flatNo { get; set; }
    public string fullName { get; set; }
    public int gender { get; set; }
    public bool genderSpecified { get; set; }
    public string holderName { get; set; }
    public string lastName { get; set; }
    public string middleName { get; set; }
    public int nationality { get; set; }
    public bool nationalitySpecified { get; set; }
    public object otherName { get; set; }
    public string secondaryCivilID { get; set; }
    public string street { get; set; }
    public string title { get; set; }
    public string customerNumber { get; set; }
}


public record class CompleteActivityRequest(decimal RequestActivityId, RequestActivityStatus Status = RequestActivityStatus.Approved, Guid? TaskId = null, Guid? InstanceId = null, string ReasonForRejection = "");

public class ProcessCardRequest : ActivityProcessRequest
{
    public required decimal RequestId { get; set; }
    public DateTime DateOfBirth { get; set; }
    public decimal ApprovedLimit { get; set; }

    [ValidateComplexType]
    [JsonProperty]
    public BCDParameters? BCDParameters { get; set; }

    [ValidateComplexType]
    [JsonProperty]
    public TayseerCreditCheckingDto? CreditCheckModel { get; set; }
}

public class BCDParameters
{
    public CreditCardStatus NewStatus { get; set; } = CreditCardStatus.Approved;

    [RegularExpression(@"^(\d{16})$", ErrorMessage = "Not a valid CardNumber (should have 16 digits)")]
    public required string CardNumber { get; set; }

    [RegularExpression(@"^(\d{16})$", ErrorMessage = "Not a valid Master Number (should have 16 digits)")]
    public string MasterCardNumber { get; set; }

    [RegularExpression(@"^(\d{19})$", ErrorMessage = "Not a valid Account Number (should have 19 digits)")]
    public required string FdrAccountNumber { get; set; }
    public string CustomerNumber { get; set; }
    public bool CloseOldCardNumber { get; set; } = false;
    public string OldCardNumber { get; set; }
}


public class BulkCardActivationRequest
{
    public List<string> CardNumbers { get; set; } = null!;
    public required string CivilId { get; set; } = null!;
}
public class ProcessMigrateCollateralRequest : ActivityProcessRequest
{
    public required decimal RequestId { get; set; }
}

public class ProcessChangeLimitRequest : ActivityProcessRequest
{
    public Collateral Collateral { get; set; }
    public string CollateralAccountNumber { get; set; } = string.Empty;
    public int CollateralAmount { get; set; }
    public string CollateralNumber { get; set; } = string.Empty;
}

public class CardActivationRequest
{
    public required decimal RequestId { get; set; }

}
public class CardReActivationRequest : CardActivationRequest
{
    public required int BranchId { get; set; }
    public decimal? KfhId { get; set; }

    public bool IsMasterCard { get; set; } = false;
}

public class ActivityProcessRequest
{
    public required ActionType ActionType { get; set; } = ActionType.Approved;
    public required decimal RequestActivityId { get; set; }

    [RegularExpression(@"^[a-zA-Z\s]{1,100}$", ErrorMessage = "Invalid ReasonForRejection")]
    public string ReasonForRejection { get; set; } = string.Empty;


    [RegularExpression(@"^[a-zA-Z\s]{1,100}$", ErrorMessage = "Invalid ApproverReason")]
    public string ApproverReason { get; set; } = string.Empty;

    public Guid? TaskId { get; set; } = null!;
    public Guid? WorkFlowInstanceId { get; set; } = null!;
    public decimal? KfhId { get; set; }

    [JsonIgnore]
    public CFUActivity Activity { get; set; }

    [JsonIgnore]
    public string CardNumber { get; set; } = string.Empty;
}

public class CardClosureRequest
{
    public required decimal RequestId { get; set; }
    public required int BranchId { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public bool IncludeValidation { get; set; } = true;

    [JsonIgnore]
    public CardDetailsResponse? CardInfo { get; set; }
}

public class ProcessCardClosureRequest : ActivityProcessRequest
{
    public string AccountNumber { get; set; } = string.Empty;
    public bool IncludeValidation { get; set; } = true;
}



public class ProcessChangeOfAddressRequest : ActivityProcessRequest
{
}

public class StopCardRequest : ValidateModel<StopCardRequest>
{
    [Required]
    [RegularExpression(@"^\d{1,20}$|^\d{1,20}.0$", ErrorMessage = "Invalid RequestId")]
    public decimal RequestId { get; set; }

    [RegularExpression(@"^\d{1,8}", ErrorMessage = "Invalid KfhId")]
    public decimal? KfhId { get; set; }
}

public interface ICardRequestResponse { }
public class ProcessResponse : ICardRequestResponse
{
    public string CardNumber { get; set; }
    public string Message { get; set; }
}

public class CardReplacementRequest : ValidateModel<CardReplacementRequest>
{
    [RegularExpression(@"^\d{1,8}", ErrorMessage = "Invalid KfhId")]

    public decimal? KfhId { get; set; }

    [Required]
    [RegularExpression(@"^\d{1,20}$|^\d{1,20}.0$", ErrorMessage = "Invalid RequestId")]
    public decimal RequestId { get; set; }

    [Required]
    [RegularExpression(@"^\d{1,3}", ErrorMessage = "Invalid BranchId")]
    public int BranchId { get; set; }

    [RegularExpression(@"^\d{1,12}", ErrorMessage = "Invalid AccountNumber")]
    public string AccountNumber { get; set; } = string.Empty;

    [DisplayName("MemberShipId")]
    [RegularExpression(@"^[1-9]{1}[0-9]{8}$", ErrorMessage = "Membership ID is not available, please try a different one (ID should be 9 digits)")]
    [SequentialValidator]
    public long? NewMemberShipId { get; set; } = null;

    [RegularExpression(@"^[1-9]{1}[0-9]{8}$", ErrorMessage = "Membership ID is not available, please try a different one (ID should be 9 digits)")]
    [SequentialValidator]
    public long? OldMembershipId { get; set; } = null!;
    public ReplaceOn ReplaceOn { get; set; } = ReplaceOn.Damage;

    [Required(ErrorMessage = "Please select delivery option")]
    public DeliveryOption DeliveryOption { get; set; } = DeliveryOption.COURIER;
    public int DeliveryBranchId { get; set; }

    //Only English
    [ArabicValidator]
    [RegularExpression(@"^[a-zA-Z\s]{1,26}$", ErrorMessage = "Holder name must contain alpabetic characters and spaces, with a maximum of 26 characters")]
    public string HolderName { get; set; }
    public bool IsNoFees { get; set; } = false;
    public bool IssueWithPinMailer { get; set; } = false;

    public string? Reason { get; set; }
    public string? BranchName { get; set; }
    public string ProductName { get; set; } = null!;
    public string CardNumber { get; set; } = null!;
    public string CardNumberDto { get; set; } = null!;

    public string ReasonForDeleteRequest { get; set; }


}

public class CardReplacementResponse
{
}

public enum ReplaceOn
{
    Damage,
    LostOrStolen
}
public enum DeliveryOption
{
    BRANCH,
    COURIER
}

public class ProcessCorporateProfileRequest : ActivityProcessRequest
{
    public CFUActivity Activity { get; set; }
}

public class ProcessMembershipDeleteRequest : ActivityProcessRequest
{
    public CFUActivity Activity { get; set; }
}



public class CoBrandRequest
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