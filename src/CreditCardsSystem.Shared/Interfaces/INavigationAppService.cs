using CreditCardsSystem.Domain.Models;
using Refit;

namespace CreditCardsSystem.Domain.Interfaces;

public interface INavigationAppService : IRefitClient
{
    const string Controller = "/api/Navigation/";

    /// <summary>
    /// Get Navigation Menu for a user
    /// </summary>
    /// <returns></returns>
    [Get($"{Controller}{nameof(GetUserMenu)}")]
    Task<List<NavigationMenuDto>> GetUserMenu();

    [Get($"{Controller}{nameof(GetClientId)}")]
    Task<string> GetClientId();

    [Post($"{Controller}{nameof(GetUserMenuWithSubMenu)}")]
    Task<List<NavigationMenuDto>> GetUserMenuWithSubMenu(List<DrawerItem>? subMenu);
}
