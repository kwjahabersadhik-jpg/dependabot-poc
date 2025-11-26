using CreditCardsSystem.Domain.Interfaces;
using CreditCardsSystem.Domain.Models.BCDPromotions.Requests;
using CreditCardsSystem.Web.Client;
using CreditCardsSystem.Web.Client.Components.Dialog;
using CreditCardsSystem.Web.Client.Pages.CustomerProfile;
using Kfh.Aurora.Blazor;
using Kfh.Aurora.Common.Components.UI.Search.Setup;
using Kfh.Aurora.Common.Components.UI.Settings.Setup;
using Kfh.Aurora.Common.Shared.Interfaces.Setup;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Refit;
using Serilog;
using Toolbelt.Blazor.Extensions.DependencyInjection;



var builder = WebAssemblyHostBuilder.CreateDefault(args);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Error()
    .Enrich.WithProperty("InstanceId", Guid.NewGuid().ToString("n"))
    .WriteTo.BrowserHttp(endpointUrl: $"{builder.HostEnvironment.BaseAddress}ingest")
    .WriteTo.BrowserConsole()
    .CreateLogger();

Log.Information("Starting Blazor WASM for Aurora CreditCards");

try
{
    builder.RootComponents.Add<App>("#app");
    builder.RootComponents.Add<HeadOutlet>("head::after");
    builder.Services.AddTelerikBlazor();
    builder.Services.AddAurora();
    var settings = new RefitSettings(new NewtonsoftJsonContentSerializer());
    builder.Services.AddKfhRefitClients<IRefitClient>(settings);
    #region Common Package Setup

    builder.Services.AddKfhRefitClients<ICommonRefitClient>(settings);

    builder.Services.AddAuroraCommonStates();
    #endregion

    builder.Services.AddHotKeys2();
    builder.Services.AddLocalization();
    builder.Services.AddScoped<AppState>();
    builder.Services.AddScoped<ApplicationState>();
    builder.Services.AddScoped<RequestStateContainer>();

    builder.Services.AddScoped<DialogBoxService>();

    builder.Services.AddAdvancedSearchComponents();
    builder.Services.AddUserSearchComponents();
    builder.Services.AddUserSettingsComponents();

    var host = builder.Build();
    await host.SetDefaultCulture();
    await host.RunAsync();
}
catch (Exception e)
{
    Log.Fatal(e, "An exception occurred while creating the WASM host");
    throw;
}
