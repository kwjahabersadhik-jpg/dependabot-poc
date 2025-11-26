using CreditCardsSystem.Domain.Enums;
using Microsoft.AspNetCore.Components;
using System.Linq.Expressions;

namespace CreditCardsSystem.Domain.Models;
public record ActionStatus(bool IsSuccess = false,
    DataStatus ProcessStatus = DataStatus.Uninitialized,
    string Message = "",
    bool CloseDialog = true,
    bool ShowConfirmation = false, string Title = "Title")
{
    public bool IsAccessDenied { get; set; }
}


public record ActionMessageStatus(DataStatus ProcessStatus = DataStatus.Uninitialized, string Message = "");
 