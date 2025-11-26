using CreditCardsSystem.Data.Models;

namespace CreditCardsSystem.Domain.Models.Reports
{
    public class MultipleCreditCardSearchResponse
    {
        public long requestID { get; set; }

        public System.DateTime requestDate { get; set; }

        public bool requestDateFieldSpecified;

        public System.DateTime approveDate { get; set; }

        public bool approveDateFieldSpecified;

        public int branchID { get; set; }

        public int cardType { get; set; }

        public string cardNo { get; set; }

        public int sellerID { get; set; }

        public int tellerID { get; set; }

        public int cardStatus { get; set; }

        public double requestLimit { get; set; }

        public double approvedLimit { get; set; }

        public string civilID { get; set; }

        public string customerName { get; set; }

        public int approvedBy { get; set; }

        public string continuation1 { get; set; }

        public string continuation2 { get; set; }

        public long mobile { get; set; }

        public long homePhone { get; set; }

        public long workPhone { get; set; }

        public string acctNo { get; set; }

        public string gender { get; set; }

        public string fdAccountNumber { get; set; }

        public RequestParameter[] requestParameters { get; set; }
    }
}
