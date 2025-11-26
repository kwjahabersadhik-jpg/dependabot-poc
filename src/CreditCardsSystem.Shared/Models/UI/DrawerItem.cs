namespace CreditCardsSystem.Domain.Models;

public class DrawerItem
{
    public string Text { get; set; } = default!;
    public string Url { get; set; } = default!;
    public string Icon { get; set; } = default!;
    public string? Permission { get; set; }
    public List<DrawerItem>? SubItems { get; set; }
}
