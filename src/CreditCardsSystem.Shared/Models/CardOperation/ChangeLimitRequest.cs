using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Models.Account;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CreditCardsSystem.Domain.Models.CardOperation;

public class ChangeLimitRequest : ValidateModel<ChangeLimitRequest>
{
    public long Id { get; set; }

    [Required(ErrorMessage = "Please enter new limit")]
    [Range(1, double.MaxValue, ErrorMessage = "Enter valid amount, should be grater than 0")]
    public decimal NewLimit { get; set; }
    public bool IsTemporary { get; set; } = false;
    public int PurgeDays { get; set; }

    public bool IsRetiree { get; set; } = false;
    public bool IsGuarantor { get; set; } = false;
    public bool InDelinquent { get; set; } = false;
    public bool InKFHBlackList { get; set; } = false;
    public bool InCinetBlackList { get; set; } = false;
    public bool IsException { get; set; } = false;
    public decimal? CinetSalary { get; set; }
    public decimal KFHSalary { get; set; }
    public decimal? CinetInstallment { get; set; }
    //public decimal? OtherBankCreditLimit { get; set; }
    public int CapsType { get; set; }
    public DateTime? CapsDate { get; set; }


    public string? CardAccount { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
    public string Comments { get; set; } = string.Empty;

    [Required]
    public string RequestIdString { get; set; }

    [JsonIgnore]
    public decimal RequestId { get => Convert.ToDecimal(RequestIdString); }

    public HoldDetailsListDto? SelectedHold { get; set; }
    public decimal? MarginAmount { get; set; }

    public EntryType EntryType { get; set; }
}

public enum EntryType
{
    CreditChecking = 1,
    LimitChange = 2
}
public enum CapsType
{
    ConsumerFinance = 1,
    HousingFinance = 2
}
