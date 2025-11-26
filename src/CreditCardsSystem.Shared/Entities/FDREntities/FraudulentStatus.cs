namespace CreditCardsSystem.Data.Models
{
    using CreditCardsSystem.Domain.Enums;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("MIGS_FRAUDULENT_STATUS")]
    public partial class FraudulentStatus
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public FraudulentStatus()
        {
            Transactions = new HashSet<Transaction>();
        }

        [Key, Column("ID")]
        public TransactionStatus Id { get; set; }

        [Required]
        [StringLength(20)]
        [Column("DESCRIPTION")]
        public string Description { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Transaction> Transactions { get; set; }
    }
}
