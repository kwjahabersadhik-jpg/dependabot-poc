namespace CreditCardsSystem.Data.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("MIGS_MERCHANT")]
    public partial class Merchant
    {
        [Key, Column("MERCHANT_NO")]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [StringLength(15)]
        public string MerchantNo { get; set; }

        [Column("MERCHANT_GROUP_ID")]
        public int GroupId { get; set; }

        [Required]
        [StringLength(50)]
        [Column("NAME_FULL")]
        public string FullName { get; set; }

        [Required]
        [StringLength(50)]
        [Column("NAME_SHORT")]
        public string ShortName { get; set; }

        [Required]
        [StringLength(3)]
        [Column("CURRENCY_CODE")]
        public string CurrencyCode { get; set; }

        [Column("CREATED_DATE")]
        public DateTime CreatedOn { get; set; }

        [Required]
        [Column("IS_ACTIVE")]
        public Boolean IsActive { get; set; }

        [Column("IS_SUSPICIOUS")]
        public bool IsSuspicious { get; set; }

        public virtual Currency Currency { get; set; }

        public virtual MerchantGroup Group { get; set; }

        public virtual ICollection<Transaction> Transactions { get; set; }
    }
}
