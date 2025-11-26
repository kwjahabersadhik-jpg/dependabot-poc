using System.ComponentModel.DataAnnotations;

namespace CreditCardsSystem.Domain.Shared.Models.BCDPromotions.CardDefinition;

public class CardMatrixDto
{
    [Display(Name = "Id")]
    public int ID { get; set; }

    [Display(Name = "Card Type Id")]
    public int CardType { get; set; }

    [Display(Name = "Card Type Name")]
    public string CardTypeName { get; set; } = string.Empty;

    //[Display(Name = "Card Type Name")]
    //public string CardDisplayName => "(" + CardType.ToString() + ") " + CardTypeName;

    [Display(Name = "Allowed Branches")]
    public string AllowedBranches { get; set; } = string.Empty;

    [Display(Name = "Is Disabled")]
    public bool IsDisabled { get; set; }

    [Display(Name = "Allowed Class Codes")]
    public string AllowedClassCode { get; set; } = string.Empty;

    [Display(Name = "Is Cobrand Prepaid")]
    public bool IsCobrandPrepaid { get; set; }

    [Display(Name = "Allowed For Non Kfh")]
    public bool AllowedNonKFH { get; set; }

    [Display(Name = "Is Corporate")]
    public bool IsCorporate { get; set; }

    [Display(Name = "Is Cobrand Credit")]
    public bool ISCobrandCredit { get; set; }

    [Display(Name = "Allowed Branches Descriptions")]
    public string AllowedBranchesDesc { get; set; } = string.Empty;

    [Display(Name = "Allowed Class Codes Descriptions")]
    public string AllowedClassCodeDesc { get; set; } = string.Empty;

    public bool? Islocked { get; set; }

}