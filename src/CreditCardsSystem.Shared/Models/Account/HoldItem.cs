namespace CreditCardsSystem.Domain.Models.Account;



public class HoldDetailsDTO
{
    public double TotalHoldAmount { get; set; }
    public int NumberOfHolds { get; set; }
    public List<HoldDetailsListDto>? HoldDetailsList { get; set; }
}

public class HoldDetailsListDto
{
    public double Amount { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public string? Description { get; set; }
    public string? Instruction { get; set; }
    public string? Tracer { get; set; }
    public long HoldId { get; set; }
    public long EmployeeId { get; set; }
    public string? HoldType { get; set; }
}

