using Bloc.Models;
using CreditCardsSystem.Domain.Interfaces;
using CreditCardsSystem.Domain.Models.Access;
using CreditCardsSystem.Web.Client.Components;
using CreditCardsSystem.Web.Client.Pages.CustomerProfile;
using Kfh.Aurora.Auth;
using Kfh.Aurora.Blazor.Components.ViewModels.NavigationBar;
using Kfh.Aurora.Common.Components.UI.Search.AdvancedSearch.States;
using Kfh.Aurora.Common.Components.UI.Settings.Cubits;
using Kfh.Aurora.Common.Components.UI.Settings.States;
using Kfh.Aurora.Common.Shared.Models.Search;
using Kfh.Aurora.Utilities;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using System.Net.Http.Json;

namespace CreditCardsSystem.Web.Client.Shared
{
    public partial class MainLayout
    {
        [Inject]
        public IHttpClientFactory HttpClientFactory { get; set; } = default!;

        [Inject]
        public NavigationManager NavigationManager { get; set; } = default!;

        [Inject]
        public INavigationAppService NavigationAppService { get; set; } = default!;

        [Inject]
        public IRequestActivityAppService RequestActivityAppService { get; set; } = default!;

        [Inject]
        public IAuthManager AuthManager { get; set; } = default!;

        [Inject]
        public AppState CurrentState { get; set; } = default!;

        [Inject]
        public ApplicationState ApplicationState { get; set; } = default!;

        [Inject]
        public BlocBuilder<UserSettingsCubit, UserSettingsState> UserSettingsBuilder { get; set; } =
            default!;
        private bool NoPermissionDialog { get; set; }
        private string SelectedBranchName { get; set; } = string.Empty;

        [CascadingParameter]
        RouteData? RouteData { get; set; }

        [Parameter]
        [SupplyParameterFromQuery]
        public string CivilId { get; set; } = string.Empty;

        Notification? Notification { get; set; } = new();

        private AuroraUser? User { get; set; }
        private string? DashboardUrl { get; set; }

        private void ListenToUserSettings(UserSettingsState userSettingsState)
        {
            if (
                userSettingsState is UserSettingsLoaded
                && userSettingsState is not UserSettingsSelectingBranch
            )
            {
                var selectedBranch = userSettingsState.Settings.UserBranches.FirstOrDefault(
                    e => e.BranchId == Convert.ToInt32(userSettingsState.BranchId)
                );
                SelectedBranchName = selectedBranch == null ? "" : selectedBranch.Name;
                StateHasChanged();
            }
        }

        private async Task OnFoundCustomer(AdvancedSearchDataFound advancedSearchDataFound)
        {
            var customerProfile = advancedSearchDataFound.Customer;

            if (customerProfile.RimNumber == 0)
                throw new Exception("Profile not found in phenix , there is no rim number");

            CurrentState.CurrentCivilId = customerProfile.CivilId;
            CurrentState.CustomerProfile = customerProfile;

            if (advancedSearchDataFound.SearchPattern == UserSearchPattern.ByCreditCard)
            {
                NavigationManager.NavigateTo(
                    $"credit-card-statement?cardNumber={advancedSearchDataFound.SearchQuery.Encode()}"
                );
                return;
            }

            var queryParams = new Dictionary<string, string>()
            {
                ["civilId"] = customerProfile!.CivilId?.Encode()!,
                ["state"] = Guid.NewGuid().ToString()
            };

            bool isPersonalCustomer = customerProfile.CustomerType
                .Trim()
                .Equals("Personal", StringComparison.InvariantCultureIgnoreCase);

            string profileViewPage = isPersonalCustomer ? "customer-view" : "corporate-view";

            NavigationManager.NavigateTo(QueryHelpers.AddQueryString(profileViewPage, queryParams));
        }

        protected override async Task OnParametersSetAsync()
        {
            await GettingCivilIdFromUrlAsync();
        }

        protected override async Task OnInitializedAsync()
        {
            UserSettingsBuilder.Bloc.OnStateChanged += ListenToUserSettings;

            await GetDashboardUrl();

            var isAllowed = AuthManager.HasPermission("creditCards.access");
            if (!isAllowed)
            {
                NoPermissionDialog = true;
                StateHasChanged();
                return;
            }

            await GettingCivilIdFromUrlAsync();
            await GetUserMenu();

            User = AuthManager.GetUser();
        }

        private async Task GettingCivilIdFromUrlAsync()
        {
            if (
                (Body!.Target as RouteView)?.RouteData?.RouteValues?.TryGetValue(
                    "CivilId",
                    out object? _civilIdFromRoute
                ) == true
                && _civilIdFromRoute != null
            )
                CivilId = _civilIdFromRoute.ToString() ?? "";

            var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);
            if (CivilId == null)
                if (
                    QueryHelpers
                        .ParseQuery(uri.Query)
                        .TryGetValue("CivilId", out var _civilIdFromQuery) == true
                )
                    CivilId = _civilIdFromQuery.ToString();

            await Task.CompletedTask;
        }

        private async Task GetDashboardUrl()
        {
            if (!string.IsNullOrEmpty(DashboardUrl))
                return;

            var client = HttpClientFactory.CreateClient("backend");
            var request = await client.GetFromJsonAsync<Dictionary<string, string>>(
                "api/discovery"
            );
            DashboardUrl = request!["aurora"];
        }

        private async Task GetUserMenu()
        {
            SetApplicationLevelMenu();

            StateHasChanged();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }


        private void SetApplicationLevelMenu()
        {
            SideMenuItems = new List<SideMenuItem>
             {
                 new(id: "home", text: "Home", url: "/", icon: "icon-Home-2 homeiconsize", isSelected: true),
                 new(id: "pending", text: "Tasks", url: "cc-tasks", icon: "icon-Home-2 homeiconsize")
             };


            if (AuthManager.HasPermission(Permissions.ApplicationStatus.View()))
                SideMenuItems.Add(new(id: "applicationStatus", text: "Application Status", url: "application-status"));

            bool isApprover = (AuthManager.HasPermission(Permissions.ConfigParameter.Approve())
                            | AuthManager.HasPermission(Permissions.DirectDebit.Approve())
                            | AuthManager.HasPermission(Permissions.Promotions.Approve())
                            | AuthManager.HasPermission(Permissions.EligiblePromotions.Approve())
                            | AuthManager.HasPermission(Permissions.PromotionGroup.Approve())
                            | AuthManager.HasPermission(Permissions.GroupAttributes.Approve())
                            | AuthManager.HasPermission(Permissions.PCT.Approve())
                            | AuthManager.HasPermission(Permissions.Services.Approve())
                            | AuthManager.HasPermission(Permissions.LoyaltyPoints.Approve())
                            | AuthManager.HasPermission(Permissions.CardDefinitions.Approve())
                            | AuthManager.HasPermission(Permissions.CardEligibilityMatrix.Approve())
                            | AuthManager.HasPermission(Permissions.ConfigParameter.Approve()));

            if (isApprover)
                SideMenuItems.Add(new(id: "adminRequests", text: "Requests", url: "requests"));

            #region Admin
            var fileMenu = new List<SideMenuItem>();
            fileMenu.Add(new(id: "artworks", text: "Cards Artwork", url: "/credit-cards", icon: "icon-Home-2 homeiconsize"));

            if (AuthManager.HasPermission(Permissions.MigsLoadFile.View()))
                fileMenu.Add(new(id: "migs", text: "MIGS", url: "migs"));

            if (AuthManager.HasPermission(Permissions.DirectDebit.View()))
                fileMenu.Add(new(id: "directDebit", text: "DirectDebit", url: "direct-debit-option"));


            if (AuthManager.HasPermission(Permissions.WorkflowCases.View()))
                fileMenu.Add(new(id: "cases", text: "Cases", url: "/workflow-cases", icon: "icon-Home-2 homeiconsize"));

            if (AuthManager.HasPermission(Permissions.PromotionsBeneficiaries.View()))
                fileMenu.Add(new(id: "adminBeneficiaries", text: "Beneficiaries", url: "beneficiaries"));

            if (AuthManager.HasPermission(Permissions.LoyaltyPoints.View()))
                fileMenu.Add(new(id: "adminPoints", text: "Loyalty Points", url: "loyalty/points"));

            if (AuthManager.HasPermission(Permissions.ConfigParameter.View()))
                fileMenu.Add(new(id: "adminParameter", text: "Config Parameter", url: "config/params"));

            if (fileMenu.Count != 0)
                SideMenuItems.Add(new(id: "admin", text: "Admin", url: "/", items: fileMenu));

            #endregion

            #region Reports
            var reportsMenu = new List<SideMenuItem>();

            if (AuthManager.HasPermission(Permissions.EndOfDayReport.View()))
                reportsMenu.Add(new(id: "eodReport", text: "End Of Day Report", url: "end-of-day-report",
                    icon: "icon-Home-2 homeiconsize"));

            if (AuthManager.HasPermission(Permissions.StatisticalReport.View()))
                reportsMenu.Add(new(id: "statisticalReport", text: "Statistical Report", url: "statistical-report",
                    icon: "icon-Home-2 homeiconsize"));

            if (AuthManager.HasPermission(Permissions.StatementSingleReport.View()))
                reportsMenu.Add(new(id: "singleReport", text: "Single Report", url: "single-report",
                    icon: "icon-Home-2 homeiconsize"));

            if (reportsMenu.Count != 0)
                SideMenuItems.Add(new(id: "reports", text: "Reports", url: "/", items: reportsMenu));
            #endregion

            

            #region Promotions
            var promotionMenu = new List<SideMenuItem>();
            if (AuthManager.HasPermission(Permissions.Promotions.View()))
                promotionMenu.Add(new(id: "promotionDef", text: "Promotions Definitions", url: "/promotions"));

            if (AuthManager.HasPermission(Permissions.EligiblePromotions.View()))
                promotionMenu.Add(new(id: "adminEligiblePromotions", text: "Eligible Promotions", url: "eligible/promotions"));

            if (AuthManager.HasPermission(Permissions.PromotionGroup.View()))
                promotionMenu.Add(new(id: "adminGroups", text: "Groups", url: "groups"));

            if (AuthManager.HasPermission(Permissions.GroupAttributes.View()))
                promotionMenu.Add(new(id: "adminAttributes", text: "Groups Attributes", url: "group/attributes"));

            if (AuthManager.HasPermission(Permissions.PCT.View()))
                promotionMenu.Add(new(id: "adminPct", text: "PCT", url: "pct"));

            if (AuthManager.HasPermission(Permissions.Services.View()))
                promotionMenu.Add(new(id: "adminServices", text: "Services", url: "services"));

            //add empty menu to add sub menu under it
            if (promotionMenu.Any())
                SideMenuItems.Add(new(id: "adminPromotions", text: "Promotions", url: "/", items: promotionMenu));
            #endregion

            #region Configurations
            var cardConfigMenu = new List<SideMenuItem>();

            if (AuthManager.HasPermission(Permissions.CardDefinitions.View()))
                cardConfigMenu.Add(new(id: "cardsDefinitions", text: "Cards Definitions", url: "cards/def"));

            if (AuthManager.HasPermission(Permissions.CardEligibilityMatrix.View()))
                cardConfigMenu.Add(new(id: "cardsEligibility", text: "Cards Eligibility Matrix", url: "cards/matrix"));

            //add empty menu to add sub menu under it
            if (cardConfigMenu.Any())
                SideMenuItems.Add(new(id: "cardsConfig", text: "Cards Configurations", url: "/", items: cardConfigMenu));

            #endregion




        }

        private List<SideMenuItem> SideMenuItems = new();
    }
}
