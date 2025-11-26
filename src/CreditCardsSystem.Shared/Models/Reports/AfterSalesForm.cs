using CreditCardsSystem.Domain.Models.CardOperation;
using System.ComponentModel.DataAnnotations;

namespace CreditCardsSystem.Domain.Models.Reports;

public class AfterSalesForm : RebrandDto
{
    [RegularExpression(@"^\d{1,8}", ErrorMessage = "Invalid KfhId")]
    public string? KfhId { get; set; }

    public decimal RequestId { get; set; }

    public string? RequestType { get; set; }

    [RegularExpression(@"^\d{1,17}", ErrorMessage = "Invalid CardNo")]
    public string? CardNo { get; set; }
    public string? ExpiryDate { get; set; }

    [RegularExpression(@"^[a-zA-Z\s]{1,100}$",ErrorMessage = "Invalid CustomerName")]
    public string? CustomerName { get; set; }

    [RegularExpression(@"^[a-zA-Z\s]{1,26}$", ErrorMessage = "Holder name must contain alpabetic characters and spaces, with a maximum of 26 characters")]
    public string? HolderName { get; set; }

    [RegularExpression(@"^\d{1,12}", ErrorMessage = "Invalid Civil ID")]
    public string? CivilID { get; set; }

    [RegularExpression(@"^\d{1,12}", ErrorMessage = "Invalid AccountNo")]
    public string? AccountNo { get; set; }

    [RegularExpression(@"^[4|5|6|9]{1}\d{7}$", ErrorMessage = "Not a valid MobileNo")]
    public string? MobileNo { get; set; }

    [RegularExpression(@"^[4|5|6|9]{1}\d{7}$", ErrorMessage = "Not a valid Tel")]
    public string? Tel { get; set; }

    [RegularExpression(@"^[a-zA-Z\s\d]{1,100}$", ErrorMessage = "Invalid CustomerName")]
    public string? Address { get; set; }
    public string? TodayDate { get; set; } = DateTime.Now.ToString("dd/MM/yyyy");

    [RegularExpression(@"^[a-zA-Z\s]{1,100}$", ErrorMessage = "Invalid ActionType")]
    public string? ActionType { get; set; }

    [RegularExpression(@"^[a-zA-Z\s]{1,6}$", ErrorMessage = "Invalid CardType")]
    public string? CardType { get; set; }

    [RegularExpression(@"^\d{1,3}", ErrorMessage = "Invalid DeliveryBranchId")]
    public string? DeliveryBranchId { get; set; }

    [RegularExpression(@"^[a-zA-Z\s]{1,100}$", ErrorMessage = "Invalid DeliveryBranchName")]
    public string? DeliveryBranchName { get; set; }

    [RegularExpression(@"^[a-zA-Z\s]{1,25}$", ErrorMessage = "Invalid DeliveryType")]
    public string? DeliveryType { get; set; }

    [RegularExpression(@"^\d", ErrorMessage = "Invalid OldLimit")]
    public string? OldLimit { get; set; }

    [RegularExpression(@"^\d", ErrorMessage = "Invalid NewLimit")]
    public string? NewLimit { get; set; }
    public bool IsTemporaryLimitChange { get; set; } = false;

    #region Collateral Data
    [RegularExpression(@"^[a-zA-Z\s]{1,200}$", ErrorMessage = "Invalid IssueOption")]
    public string? IssueOption { get; set; }


    [RegularExpression(@"^\d{1,12}", ErrorMessage = "Invalid CollateralAccountNo")]
    public string? CollateralAccountNo { get; set; }

    [RegularExpression(@"^\d", ErrorMessage = "Invalid CollateralAmount")]
    public string? CollateralAmount { get; set; }

    public decimal SalaryAmount { get; set; } = 0;
    #endregion
    public string? Currency
    {
        get
        {
            if (IssueOption is null) return "د.ك";

            return IssueOption.Contains("USD") ? "دولار" : "د.ك";
        }
    }

    public void MapRequestData(RequestDto request)
    {
        CardNo = request.CardNo;
        ExpiryDate = request.Expiry;
        CivilID = request.CivilId;

        MobileNo ??= request.BillingAddress.Mobile.ToString();
        Tel ??= $"Home Phone:{request.BillingAddress.HomePhone} Work Phone:{request.BillingAddress.WorkPhone}";
        CardType = request.CardNo is null ? string.Empty : (request.CardNo!.StartsWith("4") ? "Visa" : "Master");
        IssueOption = request.Parameters.Collateral;
        Address = string.IsNullOrEmpty(Address) ? $"POBox:{request.BillingAddress.PostOfficeBoxNumber} City:{request.BillingAddress.City} Post Code:{request.BillingAddress.PostalCode} Street:{request.BillingAddress.Street}" : Address;

        AccountNo ??= request.AcctNo;
    }

    public ReplaceOn ReplaceOn { get; set; } = ReplaceOn.Damage;

}
