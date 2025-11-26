using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Models;
using Kfh.Aurora.Logging;
using Newtonsoft.Json;
using System.ServiceModel;
using Serilog;

namespace CreditCardsSystem.Web.Server;

// You may need to install the Microsoft.AspNetCore.Http.Abstractions package into your project
public class ExceptionHandlerMiddleware(RequestDelegate _next,
                                  ILoggerFactory _loggerFactory,
                                  IWebHostEnvironment _environment)
{
    private const string contentType = "application/json";

    public async Task Invoke(HttpContext httpContext)
    {
        var errorResponse = new ApiResponseModel<string>().Fail("error");
        var _logger = _loggerFactory.CreateLogger<ExceptionHandlerMiddleware>();
        //IAuditLogger<ExceptionHandlerMiddleware> _auditLogger = (IAuditLogger<ExceptionHandlerMiddleware>)_logger;


        string requestBody = string.Empty;
        string? actionName = string.Empty;

        RouteEndpoint? endpoint = null;

        if (httpContext.GetEndpoint() is RouteEndpoint _endpoint)
        {
            endpoint = _endpoint;

            actionName = _endpoint.RoutePattern.Defaults.Count > 1 ? _endpoint.RoutePattern.Defaults["action"]?.ToString() : null;
        }

        try
        {
            //TODO : Capture Request and keep it for exception log
            await _next(httpContext);
        }

        catch (IntegrationException ex)
        {
            //TODO: Need to validate
            //httpContext.Request.EnableBuffering();
            //if (httpContext.Request.Body.CanRead)
            //    requestBody = await new StreamReader(httpContext.Request.Body).ReadToEndAsync();

            _logger.LogError(ex, "message:{message}, Attributes:{@Attributes}", ex.Message, ex);

            if (ex.Continue)
            {
                await _next(httpContext);
            }
            else
            {
                errorResponse.Message = ex.Message;
                await WriteResponse(httpContext, errorResponse);
            }
        }

        catch (ApiException ex)
        {
            if (ex.InsertSeriLog)
                _logger.LogError(ex, message: "Unhandled API Exception");

            if (!ex.Continue)
            {
                errorResponse.Message = ex.Message;
                errorResponse.ValidationErrors = ex.Errors ?? [];
                await WriteResponse(httpContext, errorResponse);
            }
        }
        catch (Exception ex) when (ex is CommunicationException or FaultException)
        {
            errorResponse.Message = "Integration communication error";
            _logger.LogError(ex, message: errorResponse.Message);
            await WriteResponse(httpContext, errorResponse);
        }
        catch (Exception ex) when (ex is not (TaskCanceledException or IntegrationException))
        {
            errorResponse.Message = ex.Message;
            _logger.LogError(ex, message: $"{actionName} {errorResponse.Message}");
#if DEBUG

#else
            _logger.LogError(ex, message: ex.Message);
#endif
            await WriteResponse(httpContext, errorResponse);
        }
    }

    private async Task WriteResponse(HttpContext httpContext, ApiResponseModel<string> errorResponse)
    {
        httpContext.Response.ContentType = contentType;
        await httpContext.Response.WriteAsync(JsonConvert.SerializeObject(errorResponse));
    }
}

// Extension method used to add the middleware to the HTTP request pipeline.
public static class ExceptionHandlerExtensions
{
    public static IApplicationBuilder UseAurorExceptionHandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ExceptionHandlerMiddleware>();
    }
}

 