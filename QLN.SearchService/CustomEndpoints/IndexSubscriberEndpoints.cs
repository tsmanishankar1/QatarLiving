using Dapr;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.IService.ISearchService;
using static QLN.Common.Infrastructure.Constants.ConstantValues;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace QLN.SearchService.CustomEndpoints
{
    internal class SearchIndexSubscriber { }

    public static class IndexSubscriberEndpoints
    {
        public static void MapIndexSubscriberEndpoints(this WebApplication app)
        {
            app.MapPost(
                "/index-updates",
                [Topic(PubSubName, PubSubTopics.IndexUpdates)]
            async (HttpContext context,
                       ISearchService svc,
                       ILogger<SearchIndexSubscriber> logger) =>
                {
                    try
                    {
                        using var reader = new StreamReader(context.Request.Body);
                        var requestBody = await reader.ReadToEndAsync();

                        using var jsonDoc = JsonDocument.Parse(requestBody);
                        var root = jsonDoc.RootElement;

                        IndexMessage msg = null;
                        var jsonOptions = new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                            PropertyNameCaseInsensitive = true
                        };

                        // Handle both CloudEvent and direct IndexMessage formats
                        if (root.TryGetProperty("data", out var dataProperty))
                        {
                            msg = JsonSerializer.Deserialize<IndexMessage>(dataProperty.GetRawText(), jsonOptions);
                        }
                        else if (root.TryGetProperty("action", out _))
                        {
                            msg = JsonSerializer.Deserialize<IndexMessage>(requestBody, jsonOptions);
                        }
                        else
                        {
                            logger.LogWarning("Unknown message format received");
                            return Results.BadRequest("Unknown message format");
                        }

                        if (msg == null || string.IsNullOrEmpty(msg.Action))
                        {
                            logger.LogWarning("Invalid IndexMessage received");
                            return Results.Ok();
                        }

                        if (msg.Action == "Upsert")
                        {
                            if (msg.UpsertRequest == null)
                            {
                                logger.LogWarning("UpsertRequest is null for Upsert action");
                                return Results.BadRequest("UpsertRequest cannot be null for Upsert action");
                            }

                            var id =msg.UpsertRequest.ServicesItem?.Id ??
                                    msg.UpsertRequest.ClassifiedsItem?.Id ??
                                    msg.UpsertRequest.ClassifiedsDealsItem?.Id ??
                                    msg.UpsertRequest.ClassifiedsPrelovedItem?.Id ??
                                    msg.UpsertRequest.ClassifiedsCollectiblesItem?.Id;

                            await svc.UploadAsync(msg.UpsertRequest);
                            logger.LogInformation("Indexed item {ItemId} in {IndexName}", id, msg.UpsertRequest.IndexName);
                        }
                        else if (msg.Action == "Delete" && !string.IsNullOrWhiteSpace(msg.DeleteKey))
                        {
                            await svc.DeleteAsync(msg.Vertical, msg.DeleteKey);
                            logger.LogInformation("Deleted item {ItemKey} from {IndexName}", msg.DeleteKey, msg.Vertical);
                        }
                        else
                        {
                            logger.LogWarning("No actionable data in IndexMessage: Action={Action}", msg.Action);
                        }

                        return Results.Ok();
                    }
                    catch (JsonException ex)
                    {
                        logger.LogError(ex, "JSON deserialization failed");
                        return Results.BadRequest("Invalid JSON format");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to process IndexMessage");
                        return Results.Problem(
                            title: "Indexing Failure",
                            detail: "An internal error occurred while processing the index message.",
                            statusCode: StatusCodes.Status500InternalServerError
                        );
                    }
                })
            .WithName("IndexSubscriber")
            .WithTags("Indexing");
        }
    }
}