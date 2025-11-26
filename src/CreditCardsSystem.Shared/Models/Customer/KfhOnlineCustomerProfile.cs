using CreditCardsSystem.Domain.Enums;

namespace CreditCardsSystem.Domain.Models.Customer;

public class KfhOnlineCustomerProfile
{
    public int ProfileId { get; set; }

    public string Username { get; set; } = default!;

    public string Name { get; set; } = default!;

    public string AccountNumber { get; set; } = default!;

    public string CardNumber { get; set; } = default!;

    public string CustomerType { get; set; } = default!;

    public bool IsPasswordExpired { get; set; }

    public bool IsKfhStaff { get; set; }

    public bool IsFullAccess { get; set; }

    public bool IsActive { get; set; }

    public CustomerStatus Status { get; set; }

    public bool SiteKeyQuestionsDisabled { get; set; }

    public DateTime LastLoginDate { get; set; }

    public DateTime PasswordExpiryDate { get; set; }

    public DateTime LastAccessTime { get; set; }
}