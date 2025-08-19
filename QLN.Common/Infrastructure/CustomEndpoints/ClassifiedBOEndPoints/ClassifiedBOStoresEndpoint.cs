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

   //         group.MapPost("/stores-create-subscriptions", async Task<Results<
   //             Ok<string>,
   //             ForbidHttpResult,
   //             BadRequest<ProblemDetails>,
   //             ProblemHttpResult>>
   //             (
   //             [FromBody]StoresSubscriptionDto dto,
   //             [FromServices]IClassifiedStoresBOService service,
   //             HttpContext httpContext,
   //             CancellationToken cancellationToken
   //             ) =>
   //         {
   //             try
   //             {
   //                 var userClaim = httpContext.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;
   //                 if (string.IsNullOrEmpty(userClaim))
   //                 {
   //                     return TypedResults.Forbid();
   //                 }

   //                 var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
   //                 var userId = userData.GetProperty("uid").GetString();
   //                 var userName = userData.GetProperty("name").GetString();

   //                 if (string.IsNullOrWhiteSpace(userId))
   //                 {
   //                     return TypedResults.Forbid();
   //                 }

   //                 var result = await service.CreateStoreSubscriptions(dto, cancellationToken);
   //                 return TypedResults.Ok(result);
   //             }
   //             catch (InvalidDataException ex)
   //             {
   //                 return TypedResults.BadRequest(new ProblemDetails
   //                 {
   //                     Title = "Invalid Data",
   //                     Detail = ex.Message,
   //                     Status = StatusCodes.Status400BadRequest
   //                 });
   //             }
   //             catch (Exception ex)
   //             {
   //                 return TypedResults.Problem("Internal Server Error", ex.Message);
   //             }
   //         })
   //             .WithName("CreateStoresSubscription")
   //             .WithTags("ClassifiedBo")
   //             .WithSummary("Create Stores Subscriptions")
   //             .WithDescription("Creates a stores subscriptions using authenticated user info and returns success message.")
   //             .Produces<string>(StatusCodes.Status200OK)
   //             .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
   //             .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
   //             .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

   //         group.MapPost("/stores-creates-subscriptions", async Task<Results<
   //             Ok<string>,
   //             BadRequest<ProblemDetails>,
   //             ProblemHttpResult>>
   //             (
   //             [FromBody]StoresSubscriptionDto dto,
   //             IClassifiedStoresBOService service,
   //             CancellationToken cancellationToken
   //             ) =>
   //         {
   //             try
   //             {
   //                 Console.WriteLine("hits internal bo");
   //                 var result = await service.CreateStoreSubscriptions(dto, cancellationToken);
   //                 return TypedResults.Ok(result);
   //             }
   //             catch (InvalidDataException ex)
   //             {
   //                 return TypedResults.BadRequest(new ProblemDetails
   //                 {
   //                     Title = "Invalid Data",
   //                     Detail = ex.Message,
   //                     Status = StatusCodes.Status400BadRequest
   //                 });
   //             }
   //             catch (Exception ex)
   //             {
   //                 return TypedResults.Problem("Internal Server Error", ex.Message);
   //             }
   //         })
   //             .ExcludeFromDescription()
   //             .WithName("CreateStoreSubscription")
   //             .WithTags("ClassifiedBo")
   //             .WithSummary("Create Stores Subscriptions")
   //             .WithDescription("Creates a stores subscriptions using authenticated user info and returns success message.")
   //             .Produces<string>(StatusCodes.Status200OK)
   //             .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
   //             .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

   //         group.MapPut("/stores-edit-subscriptions", async Task<Results<
   //       Ok<string>,
   //       ForbidHttpResult,
   //       BadRequest<ProblemDetails>,
   //       ProblemHttpResult>>
   //       (
   //       [FromServices] IClassifiedStoresBOService service,
   //       HttpContext httpContext,
   //       int OrderID,
   //       string Status,
   //       CancellationToken cancellationToken
   //       ) =>
   //         {
   //             try
   //             {
   //                 var userClaim = httpContext.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;
   //                 if (string.IsNullOrEmpty(userClaim))
   //                 {
   //                     return TypedResults.Forbid();
   //                 }

   //                 var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
   //                 var userId = userData.GetProperty("uid").GetString();
   //                 var userName = userData.GetProperty("name").GetString();

   //                 if (string.IsNullOrWhiteSpace(userId))
   //                 {
   //                     return TypedResults.Forbid();
   //                 }


   //                 var result = await service.EditStoreSubscriptions(OrderID, Status, cancellationToken);

   //                 return TypedResults.Ok(result);
   //             }
   //             catch (Exception ex)
   //             {
   //                 return TypedResults.Problem(
   //                     title: "Internal Server Error",
   //                     detail: ex.Message,
   //                     statusCode: StatusCodes.Status500InternalServerError,
   //                     instance: httpContext.Request.Path
   //                 );
   //             }
   //         })
   //             .WithName("EditStoresSubscriptions")
   //             .WithTags("ClassifiedBo")
   //             .WithSummary("Edit subscriptions on stores.")
   //             .WithDescription("Edit the status information of stores subscriptions.")
   //             .Produces<List<StoresSubscriptionDto>>(StatusCodes.Status200OK)
   //             .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
   //             .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

   //         group.MapPut("/stores-edits-subscriptions", async Task<Results<
   //Ok<string>,
   //BadRequest<ProblemDetails>,
   //ProblemHttpResult>>
   //(
   //[FromServices] IClassifiedStoresBOService service,
   //HttpContext httpContext,
   //int OrderID,
   //string Status,
   //CancellationToken cancellationToken
   //) =>
   //         {
   //             try
   //             {
   //                 var result = await service.EditStoreSubscriptions(OrderID, Status, cancellationToken);

   //                 return TypedResults.Ok(result);
   //             }
   //             catch (Exception ex)
   //             {
   //                 return TypedResults.Problem(
   //                     title: "Internal Server Error",
   //                     detail: ex.Message,
   //                     statusCode: StatusCodes.Status500InternalServerError,
   //                     instance: httpContext.Request.Path
   //                 );
   //             }
   //         })
   //             .ExcludeFromDescription()
   //             .WithName("EditStoreSubscriptions")
   //             .WithTags("ClassifiedBo")
   //             .WithSummary("Edit subscriptions on stores.")
   //             .WithDescription("Edit the status information of stores subscriptions.")
   //             .Produces<List<StoresSubscriptionDto>>(StatusCodes.Status200OK)
   //             .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
   //             .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapGet("/stores-process-xml",
    async Task<Results<Ok<string>, BadRequest<ProblemDetails>, ForbidHttpResult, ProblemHttpResult>> (
        string Url,
        string? CompanyId,
        string? SubscriptionId,
        [FromServices] IClassifiedStoresBOService service,
        HttpContext context,
        CancellationToken cancellationToken
    ) =>
    {
        try
        {
            var userClaim = context.User.Claims.FirstOrDefault(c => c.Type == "user")?.Value;
            if (string.IsNullOrEmpty(userClaim))
                return TypedResults.Forbid();

            var userData = JsonSerializer.Deserialize<JsonElement>(userClaim);
            var userId = userData.GetProperty("uid").GetString();
            var UserName = userData.GetProperty("name").GetString();

            if (string.IsNullOrWhiteSpace(userId))
                return TypedResults.Forbid();

            var result = await service.GetProcessStoresXML(Url, CompanyId, SubscriptionId, UserName?.ToString(), cancellationToken);

            if (result?.ToString() == "created")
            {
                return TypedResults.Ok("Products have been successfully created at the specified store(s).");
            }

            return TypedResults.BadRequest(new ProblemDetails
            {
                Title = "Store Processing Failed",
                Detail = result?.ToString() ?? "Unknown error occurred.",
                Status = StatusCodes.Status400BadRequest
            });
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
                .WithName("GetProcessStoresXML")
                .WithTags("ClassifiedBo")
                .WithSummary("Remember, the XML file name should be the GUID number of the uploaded documents (PDF, Excel..etc).")
                .WithDescription("Processing the uploaded xml.Storing the products into data layer.")
                .Produces<string>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status403Forbidden) 
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapGet("/stores-processing-xml",
    async Task<Results<Ok<string>, BadRequest<ProblemDetails>, ForbidHttpResult, ProblemHttpResult>> (
        string Url,
        string? CompanyId,
        string? SubscriptionId,
        string UserName,
        [FromServices] IClassifiedStoresBOService service,
        HttpContext context,
        CancellationToken cancellationToken
    ) =>
    {
        try
        {
            var result = await service.GetProcessStoresXML(Url, CompanyId, SubscriptionId, UserName, cancellationToken);
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
                .WithName("GetProcessStoreXML")
                .WithTags("ClassifiedBo")
                .WithSummary("Remember, the XML file name should be the GUID number of the uploaded documents (PDF, Excel..etc).")
                .WithDescription("Processing the uploaded xml.Storing the products into data layer.")
                .Produces<string>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status403Forbidden)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapGet("/stores-process-csv",
    async Task<Results<Ok<string>, BadRequest<ProblemDetails>, ForbidHttpResult, ProblemHttpResult>> (
        string Url,
        string CsvPlatform,
        string CompanyId,
        string SubscriptionId,
        [FromServices] IClassifiedStoresBOService service,
        HttpContext context,
        CancellationToken cancellationToken
    ) =>
    {
        try
        {
            var (userId, validSubscriptions, error) = GenericClaimsHelper.GetValidSubscriptions(context.User, (int)Vertical.Classifieds, (int)SubVertical.Stores);
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

            var result = await service.GetProcessStoresCSV(Url, CsvPlatform,CompanyId, SubscriptionId, userId?.ToString(), cancellationToken);

            if (result?.ToString() == "created")
            {
                return TypedResults.Ok("Products have been successfully created at the specified store(s).");
            }

            return TypedResults.BadRequest(new ProblemDetails
            {
                Title = "Store Processing Failed",
                Detail = result?.ToString() ?? "Unknown error occurred.",
                Status = StatusCodes.Status400BadRequest
            });
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
       [FromServices] IClassifiedStoresBOService service,
       HttpContext context,
       CancellationToken cancellationToken
   ) =>
   {
       try
       {
           var result = await service.GetProcessStoresCSV(Url, CsvPlatform, CompanyId, SubscriptionId, UserId, cancellationToken);
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

            group.MapGet("/stores-test-xml-validation", async Task<Results<
           Ok<string>,
           BadRequest<ProblemDetails>,
           ProblemHttpResult>>
           (
           [FromServices] IClassifiedStoresBOService service,
            HttpContext context,
           CancellationToken cancellationToken
           ) =>
            {
                try
                {
                    var result = await service.GetTestXMLValidation(cancellationToken);
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
                .WithName("GetTestXMLValidation")
                .AllowAnonymous()
                .WithTags("ClassifiedBo")
                .WithSummary("Test XML Validation.")
                .WithDescription("Testing validation from XSD")
                .Produces<List<StoresSubscriptionDto>>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
    }
}
