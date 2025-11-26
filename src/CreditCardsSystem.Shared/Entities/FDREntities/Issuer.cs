namespace CreditCardsSystem.Data.Models
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("MIGS_ISSUER")]
    public partial class Issuer
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("ISSUER_BIN")]
        public int Bin { get; set; }

        [Required]
        [StringLength(150)]
        [Column("ISSUER_NAME")]
        public string Name { get; set; }

        [Required]
        [StringLength(3)]
        [Column("ISSUER_COUNTRY")]
        public string CountryCode { get; set; }

        [Column("IS_SUSPICIOUS")]
        public bool IsSuspicious { get; set; }

        public virtual Country Country { get; set; }

        public virtual ICollection<Transaction> Transactions { get; set; }
    }
}
