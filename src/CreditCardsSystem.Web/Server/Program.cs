using Autofac;
using CreditCardsSystem.Application;
using CreditCardsSystem.Application.BCDPromotions.Requests;
using CreditCardsSystem.Domain.Interfaces;
using CreditCardsSystem.Domain.Models.Options;
using CreditCardsSystem.Domain.Shared.Interfaces;
using CreditCardsSystem.ExternalService;
using CreditCardsSystem.Web.Server;
using Kfh.Aurora;
using Kfh.Aurora.Common.Application.Setup;
using Kfh.Aurora.ExternalServices.Server.Setup;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;

var builder = WebApplication.CreateBuilder(args);


var discovery = await TokenExtensions.GetDiscovery(builder.Configuration["AuthServer:Authority"]!);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .WriteTo.Seq(discovery["seq"])
    .CreateBootstrapLogger();

Log.Information("Starting up");

try
{
    builder.Host.AddAurora(options =>
    {
        options.ConfigureLogging = c =>
        {
            c.Syslog.Host = builder.Configuration["Syslog:Host"];
            c.Syslog.AppName = "Aurora.CreditCard";
        };
        options.ConfigureContainer = c =>
        {
            c.RegisterGeneric(typeof(RequestMaker<>)).As(typeof(IRequestMaker<>));
            c.RegisterModule<ApplicationModule>();
            c.RegisterModule<ExternalServiceModule>();
        };

        options.EnableFileStorage = true;
    });


    builder.Services.Configure<IntegrationOptions>(builder.Configuration.GetSection(IntegrationOptions.Integration));
    builder.Services.Configure<DocuwareOptions>(builder.Configuration.GetSection(DocuwareOptions.Section));


    builder.AddAuroraIntegration();


    builder.AddAuroraCommonSetup();
    builder.Services.AddMvc()
        .AddKfhDynamicControllers<IAppService, ApplicationModule>();

    builder.Services.AddRazorPages();

    builder.Services.AddDataBaseConfiguration(builder.Configuration);

    builder.WebHost.AddTokenManagement();

    builder.Services.AddOutputCache(options =>
    {
        options.AddBasePolicy(policy => policy.Expire(TimeSpan.FromMinutes(5)));
        options.AddPolicy("Expire10", policy => policy.Expire(TimeSpan.FromMinutes(10)));
    });

    builder.Services.AddHealthChecks()
        .AddSqlServer(connectionString: builder.Configuration["ConnectionStrings:DefaultConnection"]!,
            name: "ConnectionStrings:DefaultConnection", failureStatus: HealthStatus.Degraded)
        .AddOracle(connectionString: builder.Configuration["ConnectionStrings:FdrOracleConnection"]!,
            name: "ConnectionStrings:FdrOracleConnection", failureStatus: HealthStatus.Degraded);

    builder.Services.AddAuthorization(c =>
    {
        c.AddPolicy("allowedUsers",
            p =>
            {
                p.RequireAssertion(ctx =>
                    ctx.User.Claims.Any(cl => cl is { Type: "permissions", Value: "creditCards.access" }));
            });
    });

    //builder.Services.AddScoped<IWorkflowClient, WorkflowClientLocal>();

    //builder.Services.Configure<FormOptions>(options =>
    //{
    //    options.MultipartBodyLengthLimit = 2 * 1024 * 1024; //2 MB
    //});

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.UseWebAssemblyDebugging();
        app.UseDeveloperExceptionPage();
    }
    else
    {
        app.AddKfhSecurityHeaders();
    }

    app.UseKfhForwardedHeaders(discovery["loadBalancerIps"]);
    app.UseHttpsRedirection();

    app.UseSwagger();
    app.UseSwaggerUI(o =>
    {
        o.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
        o.RoutePrefix = "swagger";
    });

    app.UseStatusCodePages();
    app.UseKfhLogging();

    app.UseBlazorFrameworkFiles();
    app.UseStaticFiles();

    app.UseAuroraHealthChecks();

    app.UseRouting();

    app.UseAuthentication();

    app.UseAurorExceptionHandler();

    //anti-forgery for local api, authorization for local and remote api
    app.UseKfhBff();
    app.MapAuroraEndpoints();
    app.MapRazorPages();
    app.MapControllers().RequireAuthorization("allowedUsers").AsBffApiEndpoint();
    app.AddMinimalApi();
    app.MapFallbackToFile("index.html");
    app.Run();
}
catch (Exception e) when (e is not HostAbortedException)
{
    Log.Fatal(e, "Unhandled exception");
}
finally
{
    Log.Information("Shut down complete");
    Log.CloseAndFlush();
}