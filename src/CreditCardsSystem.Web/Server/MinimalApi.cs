using CreditCardsSystem.Domain.Interfaces;
using Kfh.Aurora.Storage;

namespace CreditCardsSystem.Web.Server;

public static class MinimalApi
{
    public static IEndpointRouteBuilder AddMinimalApi(this IEndpointRouteBuilder builder)
    {
        builder.MapGet("/api/discovery", async (IConfiguration configuration) =>
        {
            var discovery = await TokenExtensions.GetDiscovery(configuration["AuthServer:Authority"]!);

            return Results.Json(discovery);

        }).RequireAuthorization().AsBffApiEndpoint().SkipAntiforgery().CacheOutput(x => x.Expire(TimeSpan.FromHours(1)));

        builder.MapGet("/api/printHistory/{fileId:guid}", async (IStorageClient service, Guid fileId) =>
        {
            var reportFile = await service.DownloadFile(fileId);
            if (reportFile is null)
                return null;

            if (reportFile.Success)
            {
                var file = reportFile.Content;
                return Results.File(file!);
            }
            return null;


        }).RequireAuthorization().AsBffApiEndpoint().SkipAntiforgery().CacheOutput("Expire10");

        builder.MapGet("/api/creditCardsImages/{type}", async (ICreditCardArtworkAppService service, string type) =>
        {
            var card = await service.GetCardImage(type);
            if (card?.Image is null)
                return null;

            var ms = new MemoryStream(card.Image);
            return Results.File(ms, card.Extension);

        }).RequireAuthorization().AsBffApiEndpoint().SkipAntiforgery().CacheOutput(x => x.Expire(TimeSpan.FromHours(1)));

        return builder;
    }
}