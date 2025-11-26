using CreditCardsSystem.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace CreditCardsSystem.Domain.Shared.Models.Membership
{
    public class MemberShipDeleteRequestFilter
    {
        [MaxLength(12)]
        public string? CivilId { get; set; } = null!;

        [MaxLength(30)]
        public string? ClubMembershipId { get; set; } = null!;

        public int? CompanyId { get; set; }

        public long? Id { get; set; }


    }
    public class GetMemberShipDeleteResponse
    {
        public long Id { get; set; }
    }



    public class MembershipDeleteRequestDto
    {
        public long Id { get; set; }
        public string CivilId { get; set; } = null!;
        public string ClubMembershipId { get; set; } = null!;
        public int CompanyId { get; set; }
        public string CompanyName { get; set; }
        public int? ApprovedBy { get; set; }
        public DateTime? RequestDate { get; set; }
        public DateTime? ApproveDate { get; set; }
        public DateTime? RejectDate { get; set; }
        public string RequestorReason { get; set; } = null!;
        public string? ApproverReason { get; set; }
        public DeleteRequestStatus? Status { get; set; }
        public decimal RequestId { get; set; }
        public int RequestedBy { get; set; }
    }

    public class UpdateMembershipDeleteRequest
    {
        public List<UpdateMembershipDeleteDto> Items { get; set; }
    }

    public class UpdateMembershipDeleteDto
    {
        public long Id { get; set; }
        public string? ApproverReason { get; set; }
        public DeleteRequestStatus? Status { get; set; }
        public decimal RequestId { get; set; }
    }

    public class UpdateMembershipDeleteResponse
    {
        public List<UpdateMembershipDeleteDto> FailedItems { get; set; }

    }
}
