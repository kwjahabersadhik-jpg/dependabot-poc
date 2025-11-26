namespace CreditCardsSystem.Domain.Models;

public class NavigationMenuDto
{

    public string ApplicationName { get; set; } = default!;
    public string? ClientId { get; set; }
    public string Url { get; set; } = default!;
    public string Icon { get; set; } = default!;
    public int Order { get; set; }
    public bool IsAuroraApplication { get; set; }
    public List<DrawerItem> SubItems { get; set; } = new();
}
