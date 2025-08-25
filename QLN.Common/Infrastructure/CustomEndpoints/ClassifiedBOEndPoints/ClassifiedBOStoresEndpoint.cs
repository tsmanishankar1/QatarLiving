using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using QLN.Common.DTO_s;
using QLN.Common.DTO_s.ClassifiedsBo;
using QLN.Common.Infrastructure.IService.IClassifiedBoService;
using QLN.Common.Infrastructure.IService.V2IClassifiedBoService;
using QLN.Common.Infrastructure.Subscriptions;
using QLN.Common.Infrastructure.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.CustomEndpoints.V2ClassifiedBOEndPoints
{
    public static class ClassifiedBOStoresEndpoint
    {
        public static RouteGroupBuilder MapClassifiedBOStoresEndpoints(this RouteGroupBuilder group)
        {

            group.MapGet("/stores-get-subscriptions", async Task<Results<
          Ok<ClassifiedBOPageResponse<ViewStoresSubscriptionDto>>,
          BadRequest<ProblemDetails>,
          ProblemHttpResult>>
          (
          [FromServices]IClassifiedStoresBOService service,
          HttpContext context,
          string? subscriptionType,
          string? filterDate,
          int? Page, int? PageSize, string? Search,
          CancellationToken cancellationToken
          ) =>
            {
                try
                {
                    int page = Page ?? 1;
                    int pageSize = PageSize ?? 12;
                    var result = await service.getStoreSubscriptions(subscriptionType, filterDate, page, pageSize, Search, cancellationToken);

                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: context.Request.Path
                    );
                }
            })
                .WithName("GetStoresSubscriptions")
                .AllowAnonymous()
                .WithTags("ClassifiedBo")
                .WithSummary("To list all subscriptions in the stores.")
                .WithDescription("Fetches all subscriptions of users on stores")
                .Produces<ClassifiedBOPageResponse<ViewStoresSubscriptionDto>>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapGet("/stores-process-csv",
    async Task<Results<Ok<string>, BadRequest<ProblemDetails>, ForbidHttpResult, ProblemHttpResult>> (
        string Url,
        string CsvPlatform,
        string CompanyId,
        string SubscriptionId,
        string Domain,
        [FromServices] IClassifiedStoresBOService service,
        HttpContext context,
        CancellationToken cancellationToken
    ) =>
    {
        try
        {
            var (userId, error) = GenericClaimsHelper.GetValidUserId(context.User);
            if (!string.IsNullOrEmpty(error))
            {
                return TypedResults.Problem(
                title: "Subscription issue in token.",
                detail: error,
                statusCode: StatusCodes.Status500InternalServerError,
                instance: context.Request.Path
                );
            }

            if (string.IsNullOrEmpty(userId))
            {
                return TypedResults.Forbid();
            }

            var result = await service.GetProcessStoresCSV(Url, CsvPlatform,CompanyId, SubscriptionId, userId?.ToString(), Domain, cancellationToken);

            switch (result?.ToString())
            {
                case "created":
                    return TypedResults.Ok("Products have been successfully created at the specified store(s).");

                case "No products":
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "No Products Found",
                        Detail = "The CSV did not contain any valid products to process.",
                        Status = StatusCodes.Status400BadRequest
                    });

                case "Insufficient quota":
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Insufficient quota",
                        Detail = "The CSV did not contain any valid quota to process.",
                        Status = StatusCodes.Status400BadRequest
                    });

                case "Fail to reserve quota":
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Fail to reserve quota",
                        Detail = "The CSV fail to reserve quota to process.",
                        Status = StatusCodes.Status400BadRequest
                    });

                default:
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Store Processing Failed",
                        Detail = result?.ToString() ?? "Unknown error occurred.",
                        Status = StatusCodes.Status400BadRequest
                    });
            }
        }
        catch (Exception ex)
        {
            return TypedResults.Problem(
                title: "Internal Server Error",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError,
                instance: context.Request.Path
            );
        }
    })
                .WithName("GetProcessStoresCSV")
                .WithTags("ClassifiedBo")
                .WithSummary("Process the csv file.")
                .WithDescription("Processing the uploaded csv.Storing the products into data layer.")
                .Produces<string>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status403Forbidden)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapGet("/stores-processing-csv",
   async Task<Results<Ok<string>, BadRequest<ProblemDetails>, ForbidHttpResult, ProblemHttpResult>> (
       string Url,
        string CsvPlatform,
       string CompanyId,
       string SubscriptionId,
       string UserId,
       string Domain,
       [FromServices] IClassifiedStoresBOService service,
       HttpContext context,
       CancellationToken cancellationToken
   ) =>
   {
       try
       {
           var (userId, userName) = UserTokenHelper.ExtractUserAsync(context);

           if (string.IsNullOrWhiteSpace(userId))
           {
               return TypedResults.Forbid();
           }
           var result = await service.GetProcessStoresCSV(Url, CsvPlatform, CompanyId, SubscriptionId, userId, Domain, cancellationToken);
           return TypedResults.Ok(result);

       }
       catch (Exception ex)
       {
           return TypedResults.Problem(
               title: "Internal Server Error",
               detail: ex.Message,
               statusCode: StatusCodes.Status500InternalServerError,
               instance: context.Request.Path
           );
       }
   })
               .ExcludeFromDescription()
               //.AllowAnonymous()
               .WithName("GetProcessStoreCSV")
               .WithTags("ClassifiedBo")
               .WithSummary("Processing the csv.")
               .WithDescription("Processing the uploaded csv.Storing the products into data layer.")
               .Produces<string>(StatusCodes.Status200OK)
               .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
               .Produces(StatusCodes.Status403Forbidden)
               .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
    }
}
