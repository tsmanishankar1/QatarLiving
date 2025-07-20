using Dapr;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.IService.ISearchService;
using static QLN.Common.Infrastructure.Constants.ConstantValues;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

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
            async (CloudEvent<IndexMessage> cloudEvent,
                       ISearchService svc,
                       ILogger<SearchIndexSubscriber> logger) =>
                {
                    var msg = cloudEvent.Data;

                    if (msg == null)
                    {
                        logger.LogWarning("Received a null IndexMessage. Skipping processing.");
                        return Results.Ok();
                    }

                    try
                    {
                        if (msg.Action == "Upsert" && msg.UpsertRequest?.MasterItem != null)
                        {
                            var id = msg.UpsertRequest.MasterItem.Id;
                            var vertical = msg.UpsertRequest.IndexName;
                            logger.LogInformation("Processing upsert for item Id={Id}, Vertical={Vertical}", id, vertical);

                            await svc.UploadAsync(msg.UpsertRequest);

                            logger.LogInformation("Successfully indexed item Id={Id}", id);
                        }
                        else if (msg.Action == "Delete" && !string.IsNullOrWhiteSpace(msg.DeleteKey))
                        {
                            logger.LogInformation("Processing delete for key={Key}, Vertical={Vertical}", msg.DeleteKey, msg.Vertical);

                            await svc.DeleteAsync(msg.Vertical, msg.DeleteKey);

                            logger.LogInformation("Successfully deleted item with key={Key}", msg.DeleteKey);
                        }
                        else
                        {
                            logger.LogWarning("Received IndexMessage with no actionable data. Action={Action}, DeleteKey={DeleteKey}", msg.Action, msg.DeleteKey);
                        }

                        return Results.Ok();
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to process IndexMessage. Action={Action}, Vertical={Vertical}, DeleteKey={DeleteKey}", msg.Action, msg.Vertical, msg.DeleteKey);
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
