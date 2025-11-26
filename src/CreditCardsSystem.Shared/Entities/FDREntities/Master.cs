namespace CreditCardsSystem.Data.Models
{
    using CreditCardsSystem.Domain.Enums;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("MIGS_MASTER")]
    public class Master
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Master()
        {
            Transactions = new HashSet<Transaction>();
        }

        [Key, Column("ID")]
        public int Id { get; set; }

        [Column("LOAD_DATE")]
        public DateTime LoadDate { get; set; }

        [Column("LOAD_ID")]
        public int LoadId { get; set; }

        [Column("LOAD_STATUS")]
        public MasterFileStatus Status { get; set; }

        [Column("FILE_CREATED_ON")]
        public DateTime? FileCreatedOn { get; set; }

        /// <summary>
        /// Total amount of debit transactions (Master & Visa)
        /// </summary>
        [Column("TOTAL_DR_TRN_AMT_M_V")]
        public decimal TotalDebitTransactionsAmount { get; set; }

        /// <summary>
        /// Total amount of credit transactions (Master & Visa)
        /// </summary>
        [Column("TOTAL_CR_TRN_AMT_M_V")]
        public decimal TotalCreditTransactionsAmount { get; set; }

        /// <summary>
        /// Total amount of Phoenix Rejected Transactions
        /// </summary>
        [Column("TOTAL_PHX_RJCT_TRN_AMT_M_V")]
        public decimal TotalPHXRejectedTransactionsAmount { get; set; }

        /// <summary>
        /// Count of Phoenix Rejected Transactions
        /// </summary>
        [Column("TOTAL_PHX_RJCT_TRN_M_V")]
        public int PHXRejectedTransactionsCount { get; set; }

        public virtual LoadStatus LoadStatus { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Transaction> Transactions { get; set; }
    }
}
