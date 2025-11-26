using CreditCardsSystem.Domain.Enums;

namespace CreditCardsSystem.Domain.Models.Customer;

public class CustomerClassDto
{
    public Guid Id { get; set; }

    public int ClassCode { get; set; }

    public string? ClassDescriptionEn { get; set; }

    public string? ClassDescriptionAr { get; set; }

    public RimTypes RimType { get; set; }

    public byte[]? Image { get; set; }

    public string? FileName { get; set; }

    public string? Extension { get; set; }

    public bool IsActive { get; set; }
}