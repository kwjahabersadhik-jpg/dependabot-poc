using CreditCardsSystem.Domain.Common;
using CreditCardsSystem.Domain.Models;
using Newtonsoft.Json;

namespace CreditCardsSystem.Api;

// You may need to install the Microsoft.AspNetCore.Http.Abstractions package into your project
public class ExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IWebHostEnvironment _environment;
    private const string contentType = "application/json";

    public ExceptionHandlerMiddleware(RequestDelegate next, ILoggerFactory loggerFactory, IWebHostEnvironment environment)
    {
        _next = next;
        _loggerFactory = loggerFactory;
        _environment = environment;
    }

    public async Task Invoke(HttpContext httpContext)
    {
        var errorResponse = new ApiResponseModel<string>().Fail("error");
        var _logger = _loggerFactory.CreateLogger<ExceptionHandlerMiddleware>();
        try
        {
            await _next(httpContext);
        }
        catch (ApiException ex)
        {
            if (ex.InsertSeriLog)
                _logger.LogError(ex, message: ex.Message);

            errorResponse.Message = ex.Message;
            errorResponse.ValidationErrors = ex.Errors;

            await WriteResponse(httpContext, errorResponse);
        }
        catch (Exception ex) when (ex is not TaskCanceledException)
        {
            _logger.LogError(ex, message: ex.Message);

            errorResponse.Message = ex.ToString();
            errorResponse.IsInternalException = true;
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
