using CreditCardsSystem.Domain.Common;

namespace CreditCardsSystem.Domain.Models.CardOperation;

public class ChangeLimitHistoryDto
{
    public decimal Id { get; set; }

    public decimal ReqId { get; set; }

    public decimal OldLimit { get; set; }

    public decimal? NewLimit { get; set; }

    public string IsTempLimitChange { get; set; }

    public string ChangeType
    {
        get
        {
            return IsTempLimitChange switch
            {
                "1" => GlobalResources.Temp,
                "0" => GlobalResources.Permanent,
                _ => "N/A"
            };
        }
    }



    public DateTime LogDate { get; set; }

    public string Status { get; set; } = null!;

    public string? RefuseReason { get; set; }

    public string InitiatorId { get; set; } = null!;

    public string? ApproverId { get; set; }

    public DateTime ApproveDate { get; set; }

    public DateTime RejectDate { get; set; }


    public int PurgeDays { get; set; }

    public string? UserComments { get; set; }

    public string? MarginAccount { get; set; }

    public string? DepositAccount { get; set; }

    public decimal? DepositNumber { get; set; }

    public decimal? KfhSalary { get; set; }

    private DateTime _CurrentLimitExpiryDate;

    public DateTime CurrentLimitExpiryDate
    {
        get
        {
            if (this.PurgeDays <= 0)
                return DateTime.MinValue;

            return (ApproveDate == DateTime.MinValue) ? DateTime.MinValue : ApproveDate.AddDays(PurgeDays);
        }
        set { this._CurrentLimitExpiryDate = value; }
    }
}
