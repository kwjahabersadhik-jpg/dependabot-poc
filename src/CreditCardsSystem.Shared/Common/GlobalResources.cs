namespace CreditCardsSystem.Domain.Common;

public partial class GlobalResources
{
    public static string NotAllowedChangeLinkeAccountForNonKFH => "Change link account is not allowed to Non-KFH Customers";
    public static string NotFoundInMQ => "This credit card is not in MQ";
    public static string CorporateRimNotActive => "Corporate Rim No. is not active on phoenix";
    public static string EnterCinetSalary => "Please enter salary";
    public static string EnterOtherBankLimit => "Please enter other bank limit";
    public static string EnterCapsDate => "Please enter caps date";
    public static string CorpLimitNotDefinedInIBS => "No corporate commitment is created in Ethix Credit";

    public static string NewLimitShouldBeInTayseerCardLimitRange => "New limit should be within tayseer card limit range";

    public static string Temp => "Temporary";
    public static string Permanent => "Permanent";
    public static string NotAllowedToAddLimitChangeRequest => "it is not allowed to add one more limit change request because you already have pending one";
    public static string EnterKfhSalary => "Kindly, enter kfh salary";
    public static string TempLimitDecreaseChargeCards => "Temporary Limit decrease is not allowed for Charged Cards issued against margin or deposit";
    public static string NewLimitIsWrong => "New limit amount is wrong";
    public static string PurgeDaysAreRequired => "Purge days are required in case of temporary limit change";
    public static string LimitIncreaseInActiveCardsBlock => "You can't increase in active cards limit";
    public static string LimitIncreaseDelCreditCards => "Limit increase for Delinquent credit cards is not allowed";
    public static string PurgeDaysShouldNotBeSelected => "Purge days should not be selected with permanent limit change";
    public static string TempLimitCorporatreChargeCards => "Temporary Limit change not allowed for Charged Cards issued against Corporate";

    public static string NoMarginAccountSelected => "No margin account selected";
    public static string NoHoldSelected => "No hold Selected";
    public static string LimitMarginBalance => "New Limit is greater than margin account balance";
    public static string LimitHoldAmount => "New Limit is greater than hold amount";

    public static string LimitIncreaseCorporate => "Corporate balance cannot support a limit increase this high";

    public static string EnterCinetInstallment => "Please enter CINET installment";
    public static string EnterExceptionDescription => "Please enter description";
    public static string NewPrimaryofSuplLimitNotCover => "The new limit is not covering the total of supplementary cards limits , Reduce the supplementary cards limits first to be able to update the primary card limit";
    public static string SuplLimitMoreThanPrimLimit => "The total of supplementary cards limits is more than the primary card limit. The primary card limit is {0} and Total Supl cards Limits are {1}";

    public static string UnAuthorized => "UnAuthorized";

    public static string DualPrivilegesIssue => "You have both maker and checker privileges. you have to remove one of them.";

    public static string NoPrivilegesonCorporateProfile => "You can't access this page without maker or checker privileges";

    public static string DataNotFoundCorporate => "No Data Found for this corporate";

    public static string DuplicateMemberShipID => "Duplicate Membership ID, You are not allowed to issue Co-Brand card for same company and same membership with different civil id; Create Request to delete";
    public static string InvalidSellerId => "Invalid SellerId";
    public static string UnableToCalculateCurrencyRate => "Error happened while calculating the exchange amount";
    public static string AssignToBCDTeam => "Task Assign To BCD Team";
    public static string RedirectToTaskListForBCDUser => "Sorry unable to approve, so we created BCD Task for you. please check your pending task list to approve/ change card status";
    public static string EnigmaAPiStatus => "Unable to connect Enigma API !";

    public static string Installment => "Installment";

    public static string CardNotFoundInRemoteHost => "Card not found in remote host!";
    public static string CardNumberNotFoundInRemoteHost => "Card number not found in remote host!";

    public static string PrimaryCardUpgrade => "This is a primary card, please close all supplementary cards linked to this account to be able to upgrade.";

    public static string NotAuthorized => "Sorry, you don't have permission";

    public static string MakerCheckerAreSame => "Sorry, you cannot be a maker and checker!";

    public static string NoActionRequired => "No action required !";

    public static string DirectDebitSameOption => "You already have pending operation today with same options";
    public static string PendingDirectDebitOption => "You already have pending operation";

    public static string InvalidMembershipId => "invalid membership id";
    public static string RequestAlreadySent => "Request already sent for approval, please contact the approver";
    public static string ReplaceCardDamageRequest => "Replace Card for Damage request has been sent for approval";
    public static string ReplaceCardLostOrStolenRequest => "Replace Card for Lost or Stolen request has been sent for approval";

    public static string NoMasterCardForTayseer => "Master Card No. is mandatory for Tayseer cards, it does not exist";
    public static string CardReplacementFailed => "Failure occurred during card replacement";

    public static string UnableToLoadData => "Unable to load data!, try again";

    public static string InvalidBrandId => "Branch Id or Branch name was not set while the delivery type was branch.";
    public static string InvalidFile => "Please upload the valid file.";


    public static string InvalidOperation => "Not allowed to change status!";
    public static string LimitChangeNotAllowedForNonActiveCards => "Limit change is not allowed for inactive cards";

    public static string LimitChangeNotAllowedForTheseCards => "Card is neither Tayseer nor charge, so you are not allowed to change the limit";

    public static string NoAuthorized => "You are not allowed to Approve this request !";

    public static string RequiredDebitAccount => "Please select debit account!";
    public static string RequiredCardAccount => "Please select an account!";

    public static string CalculatingFinancialPosition => "Please wait to complete financial position calculation!";
    public static string InvalidRequest => "Invalid Request!";
    public static string InvalidInput => "Invalid input. please validate your inputs!";
    public static string NoChanges => "No changes in input. please validate your inputs!";


    public static string InvalidTask => "This task has been cancelled/removed, back to Task list..!";
    public static string InvalidCardDetail => "Unable to find card detail";

    public static object Yes => "Yes";

    public static string RequiredAmount => "Amount is required";

    public static string StandingOrderExceedAmount => "Standing order should not exceed {0} KD";
    public static string StandingOrderLessAmount => "Standing order should not be less than {0} KD";

    public static string RequiredBeneficiaryCardNumber => "Please choose any Owned or Supplementary card";

    public static string StartDateCannotBeOld => "Start date can not be an old date";

    public static string StandingOrderStartDate => "Start date must be after 7 days from today";

    public static string RequiredStandingOrderDurationEnd => "Select EndDate or Count";

    public static string InvalidEndDate => "End date must not greater than start date";

    public static string StandingOrderDatesMustBetween => "Difference between two dates must be at least 30 days";

    public static string InvalidStandingOrderCount => "Count must not exceeds 800";

    public static string InvalidMinimalStandingOrderCount => "Count is incorrect";

    public static string InsufficientChargeAccountBalance => "The charge account balance is insufficient";

    public static string PrimaryIsNotActive => "Sorry, you cannot create supplementary card, due to primary card status is not in active!";
    public static string NoSupplementaryOnClosedCard => "Sorry, we cannot add supplementary to closed or charge off card !";
    public static string WaitingForApproval => "Successfully sent your request for approval; kindly await further updates.";

    public static string PendingRequest => "You are having pending request to get approval; kindly await further updates.";

    public static string WaitingForTayseerApproval => "Successfully sent your request for {0} approval; kindly await further updates.";
    public static string CannotIssueNewCard => "The customer has existing pending cards, so a new card cannot be issues at this time";

    public static string SuccessApproval => "Successfully Approved";

    public static string SuccessIssue => "Successfully issued a new card";
    public static string SuccessUpdate => "Successfully Updated";


    public static string PleaseSelectDeliveryBranch => "Please select the delivery branch";

    public static string BioMetricRestriction => "The customer has no registered biometric fingerprint. Please ensure it is completed before serving the customer";
    public static string KYCExpired => "The customer kyc data has been expired. Please ensure it is updated before serving the customer";

    /// <summary>
    /// {civilid} - {action} - {status} - description
    /// </summary>
    public static string LogReqActivityTemplate => "{requestId} {civilid} - {requectActivityId} - {productName} ({productId}) - {cardnumber} - {action} - {status} - {description}";
    public static string LogTemplate => "{requestId} {civilid} - {action} - {description}";
    public static string LogCardTemplate => "{requestId} {cardNumber} - {action} - {description}";


}

