namespace CreditCardsSystem.Domain.Models.Reports;

public class DocumentDetailDto
{
    public string RequestId { get; set; }
    public string DocId { get; set; }

    public string FileName { get { return $"RequestID:- {RequestId}_DocumentID:- {DocId}"; } }
}
