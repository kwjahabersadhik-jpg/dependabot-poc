namespace CreditCardsSystem.Data.Models
{
    using CreditCardsSystem.Domain.Enums;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("MIGS_LOAD_STATUS")]
    public partial class LoadStatus
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public LoadStatus()
        {
            Masters = new HashSet<Master>();
        }

        [Key, Column("ID")]
        public MasterFileStatus Id { get; set; }

        [Required]
        [StringLength(20)]
        [Column("DESCRIPTION")]
        public string Description { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Master> Masters { get; set; }
    }
}
