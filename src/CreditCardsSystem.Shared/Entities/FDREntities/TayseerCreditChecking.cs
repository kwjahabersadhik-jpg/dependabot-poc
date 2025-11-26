using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CreditCardsSystem.Data.Models;

[Table("TAYSEER_CREDIT_CHECKING")]
public partial class TayseerCreditChecking
{
    [Key]
    [Column("ID")]
    [Precision(15)]
    public long Id { get; set; }

    [Column("REQUEST_ID", TypeName = "NUMBER(28)")]
    public decimal RequestId { get; set; }

    [Column("CREDIT_CARD_NUMBER")]
    [StringLength(16)]
    [Unicode(false)]
    public string? CreditCardNumber { get; set; }

    [Required]
    [Column("ENTRY_TYPE")]
    public int? EntryType { get; set; }

    [Required]
    [Column("IS_RETIREE")]
    [Precision(1)]
    public bool? IsRetiree { get; set; }

    [Required]
    [Column("IS_THERE_AGUARANTOR")]
    [Precision(1)]
    public bool? IsThereAguarantor { get; set; }

    [Column("CINET_SALARY", TypeName = "NUMBER(18,3)")]
    public decimal? CinetSalary { get; set; }

    [Column("KFH_SALARY", TypeName = "NUMBER(18,3)")]
    public decimal? KfhSalary { get; set; }

    [Column("CINET_INSTALLMENT", TypeName = "NUMBER(18,3)")]
    public decimal? CinetInstallment { get; set; }

    [Column("OTHER_BANK_CREDIT_LIMIT", TypeName = "NUMBER(18,3)")]
    public decimal? OtherBankCreditLimit { get; set; }

    [Column("CAPS_TYPE")]
    [Precision(1)]
    public bool? CapsType { get; set; }

    [Column("CAPS_DATE", TypeName = "DATE")]
    public DateTime? CapsDate { get; set; }

    [Column("IS_IN_DELINQUENT_LIST")]
    [Precision(1)]
    public bool? IsInDelinquentList { get; set; }

    [Column("IS_IN_KFH_BLACK_LIST")]
    [Precision(1)]
    public bool? IsInKfhBlackList { get; set; }

    [Column("IS_IN_CINET_BALCK_LIST", TypeName = "NUMBER")]
    public decimal? IsInCinetBalckList { get; set; }

    [Column("IS_THERE_AN_EXCEPTION")]
    [Precision(1)]
    public bool? IsThereAnException { get; set; }

    [Column("EXCEPTION_DESCRIPTION")]
    [StringLength(150)]
    [Unicode(false)]
    public string? ExceptionDescription { get; set; }

    [Column("STATUS")]
    public int? Status { get; set; }

    [Column("NEW_LIMIT", TypeName = "NUMBER(18,3)")]
    public decimal? NewLimit { get; set; }

    [Column("CREATED_BY", TypeName = "NUMBER")]
    public decimal? CreatedBy { get; set; }

    [Column("CREATED_AT", TypeName = "DATE")]
    public DateTime? CreatedAt { get; set; }
}
