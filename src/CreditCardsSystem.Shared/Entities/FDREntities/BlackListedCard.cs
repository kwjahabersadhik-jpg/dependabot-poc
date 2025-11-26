namespace CreditCardsSystem.Data.Models
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("MIGS_BLACK_LIST_CARDS")]
    public partial class BlackListedCard
    {
        [Key]
        [Column("CARD_NO")]
        [StringLength(20)]
        public string CardNo { get; set; }

        [Column("IS_SUSPICIOUS")]
        public bool IsSuspicious { get; set; }
    }
}
