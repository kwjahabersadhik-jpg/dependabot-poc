using CreditCardsSystem.Domain.Models.Card;
using Microsoft.AspNetCore.Components;

namespace CreditCardsSystem.Web.Client.Components.Dialog;

public class DialogBoxService
{
    public event Action<DialogBoxOptions> Instance = null!;
    public event Action HideDialog = null!;
    public void Open(DialogBoxOptions options)
    {
        Instance.Invoke(options);
    }

    public void Close()
    {
        HideDialog.Invoke();
    }
}

public class DialogBoxOptions
{
    public DialogBoxOptions(CreditCardDto? SelectedCard)
    {
        this.SelectedCard = SelectedCard;
    }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public CreditCardDto? SelectedCard { get; set; }
    public string ConfirmLabel { get; set; } = "Confirm";
    public string ConfirmClass { get; set; } = "btn-primary btn-sm";
    public EventCallback<object> ConfirmCallback { get; set; }
    public string CancelLabel { get; set; } = "Keep it";
    public string ExtraButtonLabel { get; set; } = string.Empty;
    public EventCallback<object> ExtraButtonCallback { get; set; }
    public bool ShowCardDetail { get; set; } = true;
}
