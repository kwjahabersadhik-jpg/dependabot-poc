namespace CreditCardsSystem.Data.Models
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("MIGS_COUNTRY")]
    public partial class Country
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Country()
        {
            Issuers = new HashSet<Issuer>();
        }

        [Key]
        [StringLength(3)]
        [Column("NUMERIC_CODE")]
        public string Code { get; set; }

        [Required]
        [StringLength(100)]
        [Column("NAME_EN")]
        public string Name { get; set; }

        [Required]
        [StringLength(2)]
        [Column("ALPHA_2_CODE")]
        public string Alpha2Code { get; set; }

        [Required]
        [StringLength(3)]
        [Column("ALPHA_3_CODE")]
        public string Alpha3Code { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Issuer> Issuers { get; set; }
    }
}
