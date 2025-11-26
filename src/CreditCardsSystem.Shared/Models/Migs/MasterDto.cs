using CreditCardsSystem.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace CreditCardsSystem.Domain.Models.Migs;


public class MasterDto
{
    [Key]
    public int Id { get; set; }
    public int LoadId { get; set; }
    public MasterFileStatus Status { get; set; }
    public DateTime? FileCreatedOn { get; set; }
    public decimal TotalDebitTransactionsAmount { get; set; }
    public decimal TotalCreditTransactionsAmount { get; set; }
    public decimal TotalPHXRejectedTransactionsAmount { get; set; }
    public int PHXRejectedTransactionsCount { get; set; }
}


 
public class LoadFileFilter
{
    [Required]
    public DateTime LoadDate { get; set; } = DateTime.Today;

    [Required]
    public int LoadId { get; set; }
}

public record GenerateFileRequestDto(int LoadId, GenerateFileRequestType FileType, bool GenerateAllTrans);


public enum MigsLoadStatus
{
    DataLoaded = 1,
    RulesApplied,
    FDOutputGenerated
}