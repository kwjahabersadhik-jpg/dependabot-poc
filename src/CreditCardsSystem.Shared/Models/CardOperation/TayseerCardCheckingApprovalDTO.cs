using System.ComponentModel.DataAnnotations;

namespace CreditCardsSystem.Domain.Models.CardOperation;

public class PreviousKFHLimitAndInstallments
{
    public decimal? ReqID { get; set; }

    public string CID { get; set; }

    public decimal? CreditLimitAmount { get; set; }

    public DateTime? IssueDate { get; set; }

    public string CardNo { get; set; }

    public decimal? GrossBalance { get; set; }

    public string ExternalStatus { get; set; }

    public string IssuingOption { get; set; }

    public int CardType { get; set; }

    public int Duality { get; set; }

    public decimal? Installments { get; set; }
}
public class PreviousInstallments
{
    public IEnumerable<PreviousKFHLimitAndInstallments>? PreviousKFHLimitAndInstallments { get; set; }
    public decimal PrevKFHCardLimit { get; set; }
    public decimal PrevKFHCardInstallment { get; set; }
}

public partial class TayseerCreditCheckingData
{
    public List<TayseerCreditCheckingDto> Logs { get; set; }
    public List<TayseerCreditCheckingDto> Footer { get; set; }
    public TayseerCreditCheckingDto? ApprovedLog { get; set; }
}
public partial class TayseerCreditCheckingDto
{
    public long Id { get; set; } = -1;

    public decimal RequestId { get; set; }

    public string? CreditCardNumber { get; set; }

    [Required(ErrorMessage = "Please select EntryType")]
    public int? EntryType { get; set; }

    public bool IsRetiree { get; set; } = false;

    public bool IsThereAguarantor { get; set; } = false;

    [Required(ErrorMessage = "Please enter CINET Salary")]
    [Range(1, double.MaxValue, ErrorMessage = "CINET salary should be grater than 0")]
    public decimal CinetSalary { get; set; }


    [Required(ErrorMessage = "Please enter KFH Salary")]
    [Range(1, double.MaxValue, ErrorMessage = "KFH salary should be grater than 0")]
    public decimal KfhSalary { get; set; }

    [Required(ErrorMessage = "Please enter Cinet Installment")]
    [Range(1, double.MaxValue, ErrorMessage = "Cinet Installment should be grater than 0")]
    public decimal CinetInstallment { get; set; }

    public decimal OtherBankCreditLimit { get; set; } = 0;

    public int CapsType { get; set; }

    public DateTime? CapsDate { get; set; }

    public bool IsInDelinquentList { get; set; } = false;

    public bool IsInKfhBlackList { get; set; } = false;

    public bool IsInCinetBalckList { get; set; } = false;

    public bool IsThereAnException { get; set; } = false;

    public string? ExceptionDescription { get; set; }

    public int? Status { get; set; }

    public decimal? NewLimit { get; set; }

    public decimal? CreatedBy { get; set; }

    public DateTime? CreatedAt { get; set; }
}


public class CreditCheckCalculationResponse
{
    public int Age { get; set; }
    public string DBR { get; set; }
    public string isThereException { get; set; }
    public string isThereDBRException { get; set; }
    public int checkLimit { get; set; }
    public bool DBRValue { get; set; }
    public string PercentageValue { get; set; }
    public bool LimitGreaterThan10000Value { get; set; }
    public bool LimitSalaryX10Value { get; set; }
    public bool OverAgeValue { get; set; }
    public bool GuarantorValue { get; set; }
    public bool RetireeValue { get; set; }
    public bool CapsValue { get; set; }
    public TayseerCreditCheckingDto LastApproved { get; set; }
    public bool IsValidCreditCheckData { get; set; } = true;
}
public class CreditCheckCalculationRequest
{
    public DateTime DateOfBirth { get; set; }
    public decimal RequestId { get; set; }
    public PreviousInstallments? PreviousInstallments { get; set; }
    public TayseerCreditCheckingDto? NewCreditCheck { get; set; }
}