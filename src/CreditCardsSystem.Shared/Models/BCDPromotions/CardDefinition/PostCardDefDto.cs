using System.ComponentModel.DataAnnotations;

namespace CreditCardsSystem.Domain.Shared.Models.BCDPromotions.CardDefinition;

public class PostCardDefDto
{
    [Display(Name = "Card Type")]
    [Required]
    [RegularExpression("^[1-9]\\d?$", ErrorMessage = "Card Type cannot be zero or more than 2 digits")]
    public int? CardType { get; set; }

    [Display(Name = "Card Name")]
    [Required]
    [StringLength(50)]
    public string Name { get; set; } = null!;

    [Display(Name = "Bin No")]
    [Required]
    [RegularExpression("^(?!0{2,}$)0$|^[1-9]\\d{0,5}$", ErrorMessage = "bin no cannot be sequence of zeros or more than 6 digits")]
    public int? BinNo { get; set; }

    [Display(Name = "System No")]
    [Required]
    [RegularExpression("^(?!0{2,}$)0$|^[1-9]\\d{0,3}$", ErrorMessage = "system no cannot be sequence of zeros or more than 4 digits")]
    public int? SystemNo { get; set; }

    [Display(Name = "Duality")]
    [Required]
    [RegularExpression("^[1-9]$", ErrorMessage = "duality cannot be zero or more than 1 digit")]
    public int? Duality { get; set; }

    [Display(Name = "Merchant Account")]
    [StringLength(19)]
    [RegularExpression("^\\d+$", ErrorMessage = "please enter a valid merchant account")]
    public string? MerchantAcct { get; set; }

    [Display(Name = "Min Limit")]
    [StringLength(10)]
    [RegularExpression("^\\d+$", ErrorMessage = "please enter a valid min limit")]
    public string? MinLimit { get; set; }

    [Display(Name = "Max Limit")]
    [StringLength(10)]
    [RegularExpression("^\\d+$", ErrorMessage = "please enter a valid max limit")]
    public string? MaxLimit { get; set; }

    [Display(Name = "Installments")]
    [RegularExpression("^\\d+$", ErrorMessage = "please enter a valid installments")]
    public decimal? Installments { get; set; }

    [Display(Name = "Monthly Max Due")]
    [RegularExpression("^\\d+$", ErrorMessage = "please enter a valid monthly max due")]
    public decimal? MonthlyMaxDue { get; set; }

    [Display(Name = "Fees")]
    [RegularExpression("^\\d+$", ErrorMessage = "please enter a valid fees")]
    public decimal? Fees { get; set; }

    public bool? IsLocked { get; set; }

    public ICollection<CardDefExtDto> CardDefExts { get; set; } = new List<CardDefExtDto>();

}