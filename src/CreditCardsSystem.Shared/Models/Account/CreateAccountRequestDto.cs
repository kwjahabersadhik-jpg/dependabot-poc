namespace CreditCardsSystem.Domain.Models.Account
{
    public class CreateCustomerAccountRequest(string branchNo, string acctType, int acctClassCode, int rimNo, string title1, string title2, string description, double faceAmount, string? TranSequence = "", int? empID = null)
    {
        public string BranchNo { get; } = branchNo;
        public string AcctType { get; } = acctType;
        public int AcctClassCode { get; } = acctClassCode;
        public int RimNo { get; } = rimNo;
        public string Title1 { get; } = title1;
        public string Title2 { get; } = title2;
        public string Description { get; } = description;
        public double FaceAmount { get; } = faceAmount;
        public string? TranSequence { get; } = TranSequence;
        public int? EmpID { get; } = empID;
    }
}
