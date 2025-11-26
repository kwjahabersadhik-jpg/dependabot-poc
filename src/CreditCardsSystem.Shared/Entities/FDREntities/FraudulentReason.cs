namespace CreditCardsSystem.Data.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("MIGS_FRAUDULENT_REASON")]
    public partial class FraudulentReason
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public FraudulentReason()
        {
            Transactions = new HashSet<Transaction>();
        }

        [Key, Column("ID")]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Column("DESCRIPTION")]
        public string Description { get; set; }

        [Column("NUMBER_OF_OCCURRENCE")]
        public int Occurance { get; set; }

        [Column("NEW_NUMBER_OF_OCCURRENCE")]
        public int? NewOccurance { get; set; }

        [Column("IS_APPROVED")]
        public bool IsApproved { get; set; }

        [Column("APPROVED_BY")]
        public int? ApprovedBy { get; set; }

        [Column("APPROVED_ON")]
        public DateTime? ApprovedOn { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Transaction> Transactions { get; set; }
    }
}
