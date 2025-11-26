using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CreditCardsSystem.Data.Models;

[Table("PROFILE")]
public partial class Profile
{
    [Key]
    [Column("CIVIL_ID")]
    [StringLength(12)]
    [Unicode(false)]
    public string CivilId { get; set; } = null!;

    [Column("TITLE")]
    [StringLength(10)]
    [Unicode(false)]
    public string Title { get; set; } = null!;

    [Column("FULL_NAME")]
    [StringLength(120)]
    [Unicode(false)]
    public string? FullName { get; set; }

    [Column("ARABIC_NAME")]
    [StringLength(200)]
    [Unicode(false)]
    public string? ArabicName { get; set; }

    [Column("OTHER_NAME")]
    [StringLength(120)]
    [Unicode(false)]
    public string? OtherName { get; set; }

    [Column("HOLDER_NAME")]
    [StringLength(26)]
    [Unicode(false)]
    public string HolderName { get; set; } = null!;

    [Column("BIRTH", TypeName = "DATE")]
    public DateTime Birth { get; set; }

    [Column("COUNTRY")]
    [Precision(4)]
    public int? Country { get; set; }

    [Column("NATIONALITY")]
    [Precision(4)]
    public int? Nationality { get; set; }

    [Column("EMPLOYER_NAME")]
    [StringLength(40)]
    [Unicode(false)]
    public string? EmployerName { get; set; }

    [Column("GENDER")]
    [Precision(1)]
    public int Gender { get; set; }

    [Column("FIRST_NAME")]
    [StringLength(40)]
    [Unicode(false)]
    public string? FirstName { get; set; }

    [Column("MIDDLE_NAME")]
    [StringLength(40)]
    [Unicode(false)]
    public string? MiddleName { get; set; }

    [Column("LAST_NAME")]
    [StringLength(40)]
    [Unicode(false)]
    public string? LastName { get; set; }

    [Column("FILE_NO")]
    [StringLength(10)]
    [Unicode(false)]
    public string? FileNo { get; set; }

    [Column("EMAIL")]
    [StringLength(50)]
    [Unicode(false)]
    public string? Email { get; set; }

    [Column("AREA")]
    [StringLength(50)]
    [Unicode(false)]
    public string? Area { get; set; }

    [Column("STREET")]
    [StringLength(40)]
    [Unicode(false)]
    public string? Street { get; set; }

    [Column("BLOCK_NO")]
    [StringLength(50)]
    [Unicode(false)]
    public string? BlockNo { get; set; }

    [Column("BUILDNO")]
    [StringLength(10)]
    [Unicode(false)]
    public string? Buildno { get; set; }

    [Column("FLATNO")]
    [StringLength(10)]
    [Unicode(false)]
    public string? Flatno { get; set; }

    [Column("EMPLOYER_SECTION")]
    [StringLength(50)]
    [Unicode(false)]
    public string? EmployerSection { get; set; }

    [Column("EMPLOYER_DEPARTMENT")]
    [StringLength(50)]
    [Unicode(false)]
    public string? EmployerDepartment { get; set; }

    [Column("EMPLOYER_AREA")]
    [StringLength(50)]
    [Unicode(false)]
    public string? EmployerArea { get; set; }

    [Column("DURATION_SERVICE")]
    [StringLength(10)]
    [Unicode(false)]
    public string? DurationService { get; set; }

    [Column("SECONDARY_CIVIL_ID")]
    [StringLength(12)]
    [Unicode(false)]
    public string? SecondaryCivilId { get; set; }

    [Column("CUSTOMER_NO")]
    [StringLength(19)]
    [Unicode(false)]
    public string? CustomerNo { get; set; }

    [Column("YOUTH_CUSTTOMER_NO")]
    [StringLength(19)]
    [Unicode(false)]
    public string? YouthCusttomerNo { get; set; }

    [Column("IS_AUB")]
    [Precision(1)]
    public int IsAUB { get; set; }
}
