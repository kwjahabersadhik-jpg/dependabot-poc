using System.ComponentModel.DataAnnotations;

namespace CreditCardsSystem.Domain.Models.BCDPromotions.Groups;

public class GroupAttributeDto
{
    [Display(Name = "Attribute Id")]
    public long AttributeID { get; set; }

    [Display(Name = "Group Id")]
    public long GroupID { get; set; }

    [Display(Name = "Old Group Id")]
    public long BackupGroupId { get; set; }

    [Display(Name = "Old Attribute Id")]
    public long BackupAttributeId { get; set; }

    public bool? IsLocked { get; set; }

    [Display(Name = "Attribute Type")]
    public string AttributeType { get; set; } = string.Empty;

    [Display(Name = "Attribute Value")]
    public string AttributeValue { get; set; } = string.Empty;

    [Display(Name = "Group Name")]
    public string GroupName { get; set; } = string.Empty;




}