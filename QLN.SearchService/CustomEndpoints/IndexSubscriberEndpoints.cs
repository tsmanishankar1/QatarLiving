using Dapr;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.IService.ISearchService;
using static QLN.Common.Infrastructure.Constants.ConstantValues;

namespace QLN.SearchService.CustomEndpoints
{
    public static class IndexSubscriberEndpoints
    {
        /// <summary>
        /// Single Dapr-subscriber for all vertical index updates.
        /// </summary>
        public static void MapIndexSubscriberEndpoints(this WebApplication app)
        {
            app.MapPost(
                    "/index-updates",
                    [Topic("pubsub", PubSubTopics.IndexUpdates)]
            async (IndexMessage msg, ISearchService svc) =>
                    {
                        if (msg.Action == "Upsert" && msg.UpsertRequest != null)
                        {
                            await svc.UploadAsync(msg.UpsertRequest);
                        }
                        else if (msg.Action == "Delete" && msg.DeleteKey != null)
                        {
                            await svc.DeleteAsync(msg.Vertical, msg.DeleteKey);
                        }
                        return Results.Ok();
                    })
               .WithName("IndexSubscriber")
               .WithTags("Indexing");
        }
    }
}
