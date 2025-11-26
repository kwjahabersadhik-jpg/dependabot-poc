namespace CreditCardsSystem.Domain.Enums;

public enum BaseActivityStatus
{
    New = 1,
    Pending = 2,
    Approved = 4,
    Rejected = 5,
}



[Serializable]
public enum RequestActivityStatus
{
    New = 1,
    Pending = 2,
    Achieved = 3,
    Approved = 4,
    Rejected = 5,
    Deleted = 6
}
