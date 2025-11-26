using CreditCardsSystem.Domain.Enums;
using CreditCardsSystem.Utility.Extensions;

namespace CreditCardsSystem.Domain.Common;

public record RListItem(string? Text, int? Value);

public class ListItem
{
    public string? Text { get; set; }
    public int? Value { get; set; }
}



public record ListItemGroup<T>
{
    public ListItemGroup(T requestType, RequestTypeGroup group, bool isEnabled = true)
    {
        this.Text = requestType.GetDescription();
        this.Value = requestType;
        this.Group = group;
        this.IsEnabled = isEnabled;
    }

    public bool IsEnabled { get; set; }
    public string? Text { get; set; }
    public T? Value { get; set; }
    public RequestTypeGroup Group { get; set; }
}
public record ListItem<T>
{
    public ListItem(string? Text, T? Value)
    {
        this.Text = Text;
        this.Value = Value;
    }
    public ListItem(T requestType)
    {
        this.Text = requestType.GetDescription();
        this.Value = requestType;
    }



    public string? Text { get; set; }
    public T? Value { get; set; }
}
