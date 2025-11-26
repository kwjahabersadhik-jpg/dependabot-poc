using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Utility.Extensions;
using Newtonsoft.Json;

namespace CreditCardsSystem.Domain.Shared.Models.RequestActivity;
public class RequestActivityDto
{
    public RequestActivityStatus RequestActivityStatus => (RequestActivityStatus)RequestActivityStatusId;
    public int RequestActivityStatusId { get; set; } = (int)RequestActivityStatus.Pending;

    public decimal TellerId { get; set; }
    public string TellerName { get; set; }
    public string BranchName { get; set; }
    public DateTime LastUpdateDate { get; set; } = DateTime.Now;
    public DateTime? ArchiveDate { get; set; }
    public DateTime CreationDate { get; set; } = DateTime.Now;
    public string CivilId { get; set; }
    public decimal RequestId { get; set; }
    public string CustomerName { get; set; }
    public CFUActivity CfuActivity => (CFUActivity)CfuActivityId;
    public int CfuActivityId { get; set; }
    public int IssuanceTypeId { get; set; }
    public decimal RequestActivityId { get; set; }
    public int? BranchId { get; set; }
    public int ApproverId { get; set; } = 0;
    public string? ApproverName { get; set; } = "";

    //public List<RequestActivityDetailsDto> Details { get; set; } = new();
    public Dictionary<string, string> Details { get; set; } = new();
    public int CardType { get; set; }

    [JsonIgnore]
    public string? CardNumber { get; set; }
    public string? CardNumberDto { get; set; }
    public string? AccountNumber { get; set; }

    public bool IsCorporateActivity { get; set; }
    public ProductTypes ProductType { get; set; }
    public string ProductName { get; set; }
    public string ReasonForRejection { get; set; } = string.Empty;
    public bool IsAllowedToApprove { get; set; }

    public bool OverrideTellerInfo { get; set; } = true;
    public CreditCardStatus CardStatus { get; set; }

    [JsonIgnore]
    public Dictionary<string, object>? WorkflowVariables { get; set; }
    public DateTime? FromDate { get; set; } = DateTime.MinValue;
    public DateTime? ToDate { get; set; } = DateTime.MinValue;

    public bool IsTayseerSalaryException { get; set; }

    public string GetValue(string Key)
    {
        return Details.GetValueOrDefault(Key) ?? string.Empty;
    }

    public string LogString()
    {
        return $"{CivilId} - Card Type: {ProductName}({CardType}) - {CardNumber.Masked(6, 6)} - RequestId: {RequestId} - Activity: {CfuActivity.GetDescription()} - Status: {RequestActivityStatus.ToString()}";
    }
}
