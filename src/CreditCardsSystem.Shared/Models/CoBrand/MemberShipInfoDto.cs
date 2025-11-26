using System.ComponentModel.DataAnnotations;

namespace CreditCardsSystem.Domain.Models.CoBrand;

public class MemberShipInfoDto
{
    public string CivilId { get; set; } = null!;

    [Required]
    [MaxLength(9, ErrorMessage = "MemberShipId Cannot exceed 9 digits")]
    public string ClubMembershipId { get; set; } = null!;
    public int CompanyId { get; set; }
    public string? FileName { get; set; }
    public DateTime? DateCreated { get; set; }
    public DateTime? DateUpdated { get; set; }
}
