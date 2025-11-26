using Kfh.Aurora.Navigation;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CreditCardsSystem.Application.Navigation;

public class NavigationAppService : INavigationAppService, IAppService
{
    private readonly ILogger<NavigationAppService> _logger;
    private readonly IMemoryCache _memoryCache;
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;

    private readonly INavigationClient _navigationClient;

    public NavigationAppService(ILogger<NavigationAppService> logger, IMemoryCache memoryCache, IConfiguration configuration, INavigationClient navigationClient, IHttpContextAccessor httpContextAccessor)
    {
        _logger = logger;
        _memoryCache = memoryCache;
        _configuration = configuration;
        _navigationClient = navigationClient;
        _httpContextAccessor = httpContextAccessor;
    }

    [HttpGet]
    public async Task<List<NavigationMenuDto>> GetUserMenu()
    {
        var kfhId = GetUserId();
        var menu = new List<NavigationMenuDto>();

        if (string.IsNullOrEmpty(kfhId))
            return menu;

        //if (_memoryCache.TryGetValue($"NavigationAppService.NavMenu.{kfhId}", out menu))
        //{
        //    _logger.LogDebug("Cache hit for navigation menu for user {User}", kfhId);
        //    return menu!;
        //}

        var newMenu = new List<NavigationMenuDto>();
        var response = await _navigationClient.GetUserMenu(kfhId);
        _logger.LogDebug("Call navigation api for user {User}", kfhId);

        newMenu.AddRange(response.Select(item => item.Adapt<NavigationMenuDto>()));

        //_memoryCache.Set($"NavigationAppService.NavMenu.{kfhId}", newMenu, TimeSpan.FromMinutes(60));

        return newMenu;
    }

    [HttpPost]
    public async Task<List<NavigationMenuDto>> GetUserMenuWithSubMenu([FromBody] List<DrawerItem>? subMenu)
    {
        var kfhId = GetUserId();
        var menu = new List<NavigationMenuDto>();

        if (string.IsNullOrEmpty(kfhId))
            return menu;

        //if (_memoryCache.TryGetValue($"NavigationAppService.NavMenu.{kfhId}", out menu))
        //{
        //    _logger.LogDebug("Cache hit for navigation menu for user {User}", kfhId);
        //    return menu!;
        //}

        var newMenu = new List<NavigationMenuDto>();
        var response = await _navigationClient.GetUserMenu(kfhId);
        _logger.LogDebug("Call navigation api for user {User}", kfhId);

        foreach (var menuItem in response.Select(navigationMenu => navigationMenu.Adapt<NavigationMenuDto>()))
        {
            if (menuItem.ClientId == ClientId)
                menuItem.SubItems = GetMenuItems(subMenu);

            newMenu.Add(menuItem);
        }

        //_memoryCache.Set($"NavigationAppService.NavMenu.{kfhId}", newMenu, TimeSpan.FromMinutes(60));

        return newMenu;
    }

    [HttpGet]
    public Task<string> GetClientId()
    {
        var clientId = ClientId;

        return Task.FromResult(clientId);
    }

    private string ClientId
    {
        get
        {
            var clientId = _configuration["AuthServer:ClientId"]!;
            if (clientId.EndsWith(".local"))
                clientId = clientId.Replace(".local", "");
            return clientId;
        }
    }

    private List<DrawerItem> GetMenuItems(List<DrawerItem>? menu)
    {
        if (menu is { Count: <= 0 })
            return new();

        var menuItems = menu!.Where(x => string.IsNullOrEmpty(x.Permission)).ToList();

        var menuWithPermission = menu!.Where(x => !string.IsNullOrEmpty(x.Permission)).ToList();
        if (menuWithPermission.Count <= 0)
            return menuItems;

        var user = _httpContextAccessor.HttpContext.User;
        if (user == null)
            return menuItems;

        var subMenu = new List<DrawerItem>();
        foreach (var item in menuWithPermission.Where(item => user.HasClaim(x => x.Type == "permissions" && x.Value == item.Permission)))
        {
            item.SubItems = GetMenuItems(item.SubItems);
            subMenu.Add(item);
        }

        menuItems.AddRange(subMenu);
        return menuItems;
    }

    private string GetUserId() => _httpContextAccessor.HttpContext.User.Claims.Single(x => x.Type == "sub").Value;
}
