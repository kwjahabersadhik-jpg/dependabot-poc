using Autofac;
using CreditCardsSystem.Api;
using CreditCardsSystem.Application;
using CreditCardsSystem.Application.BCDPromotions.Requests;
using CreditCardsSystem.Data;
using CreditCardsSystem.Domain.Models.Options;
using CreditCardsSystem.Domain.Shared.Interfaces;
using CreditCardsSystem.ExternalService;
using Kfh.Aurora;
using Kfh.Aurora.Caching;
using Kfh.Aurora.Common.Application.Setup;
using Kfh.Aurora.ExternalServices.Server.Setup;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
try
{
    var builder = WebApplication.CreateBuilder(args);

    var configuration = builder.Configuration;

    builder.Host.AddAuroraApi(o =>
    {
        o.ConfigureLogging = opts =>
        {
            opts.Syslog.Host = builder.Configuration["Syslog:Host"];
            opts.Syslog.AppName = "CreditCard.Api";
        };

        o.ConfigureContainer = b =>
        {
            b.RegisterGeneric(typeof(RequestMaker<>)).As(typeof(IRequestMaker<>));
            b.RegisterModule<ApplicationModule>();
            b.RegisterModule<ExternalServiceModule>();
        };

        o.DistributedCache = CacheOptions.Redis;
        o.EnableFileStorage = true;
    });

    builder.Services.Configure<IntegrationOptions>(builder.Configuration.GetSection(IntegrationOptions.Integration));
    builder.AddAuroraIntegration();
    builder.AddAuroraCommonSetup();

    builder.Services.AddAuthorization(options =>
    {
        var apiPolicy =
            new AuthorizationPolicyBuilder().RequireClaim("scope", AuthorizationConstants.Scopes.ApiScope).Build();

        options.AddPolicy(AuthorizationConstants.Policies.ApiPolicy, apiPolicy);
    });

    builder.Services.AddDbContext<ApplicationDbContext>(option =>
        {
            option.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
        });
    builder.Services.AddDbContext<FdrDBContext>(option =>
    {
        option.UseOracle(builder.Configuration.GetConnectionString("FdrOracleConnection"), options =>
        {
            options.UseOracleSQLCompatibility(OracleSQLCompatibility.DatabaseVersion19);
        });
    });

    builder.WebHost.AddTokenManagement();
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.Configure<IntegrationOptions>(builder.Configuration.GetSection(IntegrationOptions.Integration));
    builder.Services.AddHealthChecks()
            .AddSqlServer(connectionString: builder.Configuration["ConnectionStrings:DefaultConnection"]!, name: "ConnectionStrings:DefaultConnection", failureStatus: HealthStatus.Degraded)
            .AddOracle(connectionString: builder.Configuration["ConnectionStrings:FdrOracleConnection"]!, name: "ConnectionStrings:FdrOracleConnection", failureStatus: HealthStatus.Degraded);

    //builder.Services.AddScoped<IWorkflowClient, WorkflowClientLocal>();
    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseKfhForwardedHeaders(GetLoadBalancerIps(builder.Configuration["AuthServer:Authority"]!).GetAwaiter().GetResult());

    app.UseHttpsRedirection();


    app.UseKfhLogging();
    app.UseSwagger();
    app.UseSwaggerUI(o =>
    {
        o.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
        o.RoutePrefix = "swagger";
    });

    app.UseAuroraHealthChecks();

    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseAurorExceptionHandler();

    app.MapAuroraEndpoints();

    app.MapControllers().RequireAuthorization();
    app.MapGet("/", () => Results.Ok("Welcome to Credit Cards API"));
    app.Run();


}
catch (Exception e) when (e is not HostAbortedException)
{
    Console.WriteLine(e.Message);
}
finally
{
    Console.WriteLine("Shut down completed");
}
static async Task<string> GetLoadBalancerIps(string uri)
{
    using var client = new HttpClient();
    var api = $"{uri.TrimEnd('/')}/api/discovery";

    var result = await client.GetFromJsonAsync<Dictionary<string, string>>(api) ?? new();

    return result["loadBalancerIps"];
}