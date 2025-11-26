namespace CreditCardsSystem.Domain.Models.CardStatement
{
    public class CustomCardTransactionsDTO
    {
        public System.DateTime? dateField { get; set; }

        public System.DateTime? postingDateField { get; set; }

        public string visaMCIndicatorField { get; set; }

        public string descriptionField { get; set; }

        public decimal amountField { get; set; }

        public decimal foreignAmountField { get; set; }

        public string currencyField { get; set; }

        public string transactionCodeField { get; set; }

        public bool isCreditField { get; set; }

        public string cardTypeField { get; set; }
        public decimal CardCurrentBalance { get; set; }

        public decimal? CreditAmount { get; set; }
        public decimal? DebitAmount { get; set; }
    }
}
