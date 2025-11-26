using System.ComponentModel.DataAnnotations;

namespace CreditCardsSystem.Domain.Shared.Models.Membership
{
    public class RequestingDeleteMemberShipRequest
    {

        //[Precision(18)]

        public long Id { get; set; }

        [MaxLength(12)]
        public string CivilId { get; set; } = null!;

        [MaxLength(30)]
        public string ClubMembershipId { get; set; } = null!;

        //[Precision(2)]
        public int CompanyId { get; set; }

        //[Precision(6)]
        public int RequestedBy { get; set; }

        //[Precision(6)]
        public int? ApprovedBy { get; set; }

        public DateTime? RequestDate { get; set; }

        public DateTime? ApproveDate { get; set; }

        public DateTime? RejectDate { get; set; }

        [MaxLength(250)]
        public string RequestorReason { get; set; } = null!;

        [MaxLength(250)]
        public string? ApproverReason { get; set; }

        //[Precision(2)]
        public int? Status { get; set; }

        public bool ReturnExistingId { get; set; }
    }
    public class RequestingDeleteMemberShipResponse
    {
        public long Id { get; set; }
    }
}
