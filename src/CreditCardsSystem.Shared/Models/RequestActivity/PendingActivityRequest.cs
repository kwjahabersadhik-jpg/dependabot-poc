using CreditCardsSystem.Domain.Enums;

namespace CreditCardsSystem.Domain.Models.RequestActivity;

public record PendingActivityRequest(string CardNumber, CFUActivity[]? activities);


public record PendingRequest(RequestActivityStatus RequestActivityStatus, DateTime CreationDate, Dictionary<string, string> Details);