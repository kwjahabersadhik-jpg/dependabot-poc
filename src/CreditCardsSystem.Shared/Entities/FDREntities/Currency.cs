namespace CreditCardsSystem.Data.Models
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("MIGS_CURRENCY")]
    public partial class Currency
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Currency()
        {
            Merchants = new HashSet<Merchant>();
        }

        [Key]
        [StringLength(3)]
        [Column("CODE")]
        public string Code { get; set; }

        [Required]
        [StringLength(3)]
        [Column("NUMERIC_CODE")]
        public string NumericCode { get; set; }

        [Required]
        [StringLength(100)]
        [Column("NAME_EN")]
        public string Name { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Merchant> Merchants { get; set; }
    }
}
