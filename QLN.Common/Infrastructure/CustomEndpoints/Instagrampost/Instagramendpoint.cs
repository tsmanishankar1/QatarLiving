using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using QLN.Common.DTO_s.InstagramDto;
using QLN.Common.Infrastructure.EventLogger;
using QLN.Common.Infrastructure.IService; // Assuming you put IInstagramService here
using QLN.Common.Infrastructure.IService.IInstagramPost;
using System.Threading;

namespace QLN.Common.Infrastructure.CustomEndpoints.InstagramService
{
    public static class Instagramendpoint
    {
        public static RouteGroupBuilder MapInstagramEndpoints(this RouteGroupBuilder group)
        {
            group.MapGet("/latest-posts",
       async Task<Results<Ok<List<InstagramPost>>,
                          ProblemHttpResult>> (
           [FromServices] IInstaService instagramService,
           [FromServices] IEventlogger log,
           CancellationToken cancellationToken
       ) =>
       {
           try
           {
               var posts = await instagramService.GetLatestPostsAsync(10, cancellationToken);

               if (posts == null || posts.Count == 0)
               {
                   log.LogTrace("No Instagram posts found.");
                   return TypedResults.Problem(
                       title: "No Posts Found",
                       detail: "No Instagram posts were returned.",
                       statusCode: StatusCodes.Status404NotFound);
               }

               log.LogTrace($"Fetched {posts.Count} Instagram posts successfully.");
               return TypedResults.Ok(posts);
           }
           catch (Exception ex)
           {
               log.LogException(ex);
               return TypedResults.Problem(
                   title: "Instagram API Error",
                   detail: "An error occurred while fetching Instagram posts.",
                   statusCode: StatusCodes.Status500InternalServerError);
           }
       })
       .WithName("GetLatestInstagramPosts")
       .WithTags("Instagram")
       .WithSummary("Fetches the latest Instagram posts.")
       .WithDescription("Fetches the latest Instagram posts (excluding reels) from the configured account.")
       .Produces<List<InstagramPost>>(StatusCodes.Status200OK)
       .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
       .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
            


            return group;
        }
    }
}
