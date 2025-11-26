namespace CreditCardsSystem.Domain.Models.BCDPromotions.Groups;

public class GroupAttributeLookupDto
{
    public string Attribute { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string CodeType { get; set; } = string.Empty;
    public GroupAttributeType? AttributeType { get; set; }

    public enum GroupAttributeType
    {
        NotSet = -1,
        Nationality = 0,
        Religion = 1,
        CustomerClass = 2,
        CustomerType = 3,
        ApplType = 4,
        AcctType = 5,
        Currency = 6,
        BranchNo = 7,
        CardType = 8,
        Location = 9,
        Gender = 10,
        RimType = 11,
        Status = 12,
        Joint = 13,
    }
}

public class Attribute
{
    public string DisplayedName { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public long Id { get; set; }
}
