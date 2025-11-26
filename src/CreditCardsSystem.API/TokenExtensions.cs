namespace CreditCardsSystem.Api;

public static class TokenExtensions
{
    public static IWebHostBuilder AddTokenManagement(this IWebHostBuilder builder)
    {
        builder.ConfigureServices((context, services) =>
        {
            var discovery = GetDiscovery(context.Configuration["AuthServer:Authority"]!).GetAwaiter().GetResult();
            AuroraApiClient(services, context, discovery["api_aurora"]);
            AuroraWorkflowApiClient(services, context, discovery["enigma"]);
        });

        return builder;
    }

    public static async Task<string> GetLoadBalancerIps(string uri)
    {
        var discovery = await GetDiscovery(uri);
        return discovery["loadBalancerIps"];
    }

    private static async Task<Dictionary<string, string>> GetDiscovery(string uri)
    {
        using var client = new HttpClient();
        var api = $"{uri.TrimEnd('/')}/api/discovery";

        return await client.GetFromJsonAsync<Dictionary<string, string>>(api) ?? new();
    }
    private static void AuroraWorkflowApiClient(IServiceCollection services, WebHostBuilderContext context, string endpoint)
    {
        services.AddClientCredentialsTokenManagement()
            .AddClient("enigma.client", client =>
            {
                client.TokenEndpoint = $"{context.Configuration["AuthServer:Authority"]}/connect/token";
                client.ClientId = context.Configuration["AuthServer:ClientId"];
                client.ClientSecret = context.Configuration["AuthServer:ClientSecret"];
                client.Scope = "enigma.api.access";
            });

        services.AddClientCredentialsHttpClient("enigma", "enigma.client",
            client => { client.BaseAddress = new Uri(endpoint); });

        //services.AddTransient<IWorkflowClient, WorkflowClientLocal>();
    }



    private static void AuroraApiClient(IServiceCollection services, WebHostBuilderContext context, string endpoint)
    {
        services.AddClientCredentialsTokenManagement()
            .AddClient("aurora.client", client =>
            {
                client.TokenEndpoint = $"{context.Configuration["AuthServer:Authority"]}/connect/token";

                client.ClientId = context.Configuration["AuthServer:ClientId"];
                client.ClientSecret = context.Configuration["AuthServer:ClientSecret"];

                client.Scope = "aurora.api.access";
            });

        services.AddClientCredentialsHttpClient("aurora", "aurora.client",
            client => { client.BaseAddress = new Uri(endpoint); });
    }

}