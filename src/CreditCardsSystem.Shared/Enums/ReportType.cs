using System.ComponentModel;

namespace CreditCardsSystem.Domain.Enums;

public enum ReportType
{
    [Description("Cycle")]
    CycleToDate = 1,
    [Description("Month")]
    MonthYear = 2,
    [Description("Period")]
    FromToDate = 3,

}


