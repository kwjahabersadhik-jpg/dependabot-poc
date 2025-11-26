namespace CreditCardsSystem.Domain.Models.Migs
{
    [Serializable]
    public class ProgressReport
    {
        [ReportHeader("Merchant Name")]
        public string MerchantName { get; set; }

        [ReportHeader("Merchant Number")]
        public string MerchantNo { get; set; }

        [ReportHeader("Total Amount")]
        public decimal TotalAmount { get; set; }

        [ReportHeader("Total Amount Of Prev. Day")]
        public decimal PrevDayAmount { get; set; }

        [ReportHeader("Variance")]
        public decimal Variance
        {
            get
            {
                return TotalAmount - PrevDayAmount;
            }
        }
    }
}
