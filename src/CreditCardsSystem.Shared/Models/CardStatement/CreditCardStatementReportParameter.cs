namespace CreditCardsSystem.Domain.Models.CardStatement
{
    public class CreditCardStatementReportParameter
    {
        public decimal RequestId { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public bool IsCredit { get; set; }
        public bool IsDebit { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}
