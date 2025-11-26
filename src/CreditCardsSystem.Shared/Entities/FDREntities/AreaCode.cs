using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CreditCardsSystem.Data.Models;

[Table("AREA_CODES")]
public partial class AreaCode
{
    [Key]
    [Column("AREA_ID")]
    public int AreaId { get; set; }

    [Column("AREA_NAME")]
    [StringLength(50)]
    [Unicode(false)]
    public string AreaName { get; set; } = null!;

    [Column("AREA_ZIP_CODE")]
    public short? AreaZipCode { get; set; }

    [Column("BLOCK01_ZIP_CODE")]
    public short? Block01ZipCode { get; set; }

    [Column("BLOCK02_ZIP_CODE")]
    public short? Block02ZipCode { get; set; }

    [Column("BLOCK03_ZIP_CODE")]
    public short? Block03ZipCode { get; set; }

    [Column("BLOCK04_ZIP_CODE")]
    public short? Block04ZipCode { get; set; }

    [Column("BLOCK05_ZIP_CODE")]
    public short? Block05ZipCode { get; set; }

    [Column("BLOCK06_ZIP_CODE")]
    public short? Block06ZipCode { get; set; }

    [Column("BLOCK07_ZIP_CODE")]
    [Precision(5)]
    public short? Block07ZipCode { get; set; }

    [Column("BLOCK08_ZIP_CODE")]
    public short? Block08ZipCode { get; set; }

    [Column("BLOCK09_ZIP_CODE")]
    public short? Block09ZipCode { get; set; }

    [Column("BLOCK10_ZIP_CODE")]
    public short? Block10ZipCode { get; set; }

    [Column("BLOCK11_ZIP_CODE")]
    public short? Block11ZipCode { get; set; }

    [Column("BLOCK12_ZIP_CODE")]
    public short? Block12ZipCode { get; set; }

    [Column("BLOCK13_ZIP_CODE")]
    public short? Block13ZipCode { get; set; }

    [Column("BLOCK14_ZIP_CODE")]
    public short? Block14ZipCode { get; set; }

    [Column("BLOCK15_ZIP_CODE")]
    public short? Block15ZipCode { get; set; }

    [Column("PROVINCE_ID")]
    [Precision(2)]
    public int? ProvinceId { get; set; }
}
