using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CreditCardsSystem.Data.Models;

public partial class ReportDetail
{
    [Key]
    public long DetailsId { get; set; }

    public long ReportHeaderId { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string FieldName { get; set; } = null!;

    [StringLength(250)]
    public string FieldValue { get; set; } = null!;

    [ForeignKey("ReportHeaderId")]
    [InverseProperty("ReportDetails")]
    public virtual ReportHeader ReportHeader { get; set; } = null!;
}
