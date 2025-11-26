using CreditCardsSystem.Domain.Models;
using Kfh.Aurora.Blazor.Components.UI;

namespace CreditCardsSystem.Web.Client.Components;

public class Notification
{
    public NotificationNew? Instance { get; set; }

    /// <summary>
    /// Showing TelerikNotification
    /// </summary>
    /// <param name="alertType">Success, Info, Warning, Error </param>
    /// <param name="message"></param>
    public void Show(AlertType alertType, string message)
    {
        if (alertType == AlertType.Success)
            Success(message);

        if (alertType is AlertType.Error or AlertType.Warning or AlertType.Info)
            Failure(message);
    }

    public bool IsProcessing { get; set; }

    public bool HasOffCanvas { get; set; } = false;
    public void Clear() => Hide();
    public void Hide()
    {
        IsProcessing = false;
        Instance?.NotificationComponent.HideAll();
    }
    public void Loading(string message = "Loading...")
    {
        IsProcessing = true;
        Instance?.ShowNotifications(OperationStatus.Loading, description: message);
    }

    public void Processing(ActionStatus actionStatus)
    {
        IsProcessing = true;
        Instance!.Title = actionStatus.Title;
        Instance!.Description = actionStatus.Message;
        Instance!.Class = "loader";
        Instance!.IconName = "";
        Instance?.NotificationComponent.Show(new Telerik.Blazor.Components.NotificationModel() { CloseAfter = 0, Text = actionStatus.Message });
    }
    public void Success(string message = "")
    {
        Clear();
        IsProcessing = false;
        Instance?.ShowNotifications(OperationStatus.Success, description: message);
    }
    public void Failure(string message = "")
    {
        Clear();
        IsProcessing = false;
        Instance?.ShowNotifications(OperationStatus.Failure, description: message);
    }

    public void Info(string message = "")
    {
        Clear();
        IsProcessing = false;
        Instance?.ShowNotifications(OperationStatus.None, description: message);
    }
}
public enum AlertType
{
    Success,
    Info,
    Warning,
    Error
}
