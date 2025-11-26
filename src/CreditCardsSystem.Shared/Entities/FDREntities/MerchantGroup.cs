namespace CreditCardsSystem.Data.Models
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("MIGS_MERCHANT_GROUP")]
    public partial class MerchantGroup
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public MerchantGroup()
        {
            Merchants = new HashSet<Merchant>();
        }

        [Key, Column("ID")]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Column("NAME")]
        public string Name { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Merchant> Merchants { get; set; }
    }
}
