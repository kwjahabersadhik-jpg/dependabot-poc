namespace CreditCardsSystem.Domain.Models.Workflow
{
    public class GetCasesResponse
    {
        public List<CaseDto> Cases { get; set; } = default!;
    }

    public class GetCaseDetailResponse
    {
        public List<CaseDetailDto> Cases { get; set; } = default!;
    }
}
