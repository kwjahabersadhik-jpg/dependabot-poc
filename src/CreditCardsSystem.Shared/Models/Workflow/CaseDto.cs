using Kfh.Aurora.Organization;

namespace CreditCardsSystem.Domain.Models.Workflow
{
    public class CaseDto
    {
        public string Source { get; set; } = default!;

        public string Id { get; set; } = default!;

        public string Title { get; set; } = default!;

        public string? Description { get; set; }

        public string InitiatedBy { get; set; } = default!;

        public User? InitiatingUser { get; set; }

        public string? Url { get; set; } = default!;

        public DateTimeOffset CreatedOn { get; set; }

        public DateTimeOffset? UpdatedOn { get; set; }

        public string ClientId { get; set; } = default!;
    }

    public class CaseDetailDto
    {
        public string Source { get; set; } = default!;

        public string Id { get; set; } = default!;

        public string Title { get; set; } = default!;

        public string? Description { get; set; }
        public string? Status { get; set; }
        public Dictionary<string, object> Payload { get; set; }
        public string InitiatedBy { get; set; } = default!;

        public User? InitiatingUser { get; set; }

        public string? Url { get; set; } = default!;

        public DateTimeOffset CreatedOn { get; set; }

        public DateTimeOffset? UpdatedOn { get; set; }

        public string ClientId { get; set; } = default!;
    }
}
