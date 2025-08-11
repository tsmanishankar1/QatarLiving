using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.IService.ISearchService;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.CustomEndpoints.ClassifiedFOEndpoints
{
    //public static class ClassifiedFOStoresEndpoint
    //{
    //    //public static RouteGroupBuilder MapClassifiedFOStoresEndpoints(this RouteGroupBuilder group)
    //    //{
    //    //    group.MapPost("/stores-search", async (
    //    //        [FromBody] ClassifiedsSearchRequest req,
    //    //        [FromServices] ISearchService svc,
    //    //        [FromServices] ILoggerFactory logFac
    //    //    ) =>
    //    //    {
    //    //        var logger = logFac.CreateLogger("ClassifiedStoresEndpoints");

    //    //        var validationContext = new ValidationContext(req);
    //    //        var validationResults = new List<ValidationResult>();
    //    //        if (!Validator.TryValidateObject(req, validationContext, validationResults, validateAllProperties: true))
    //    //        {
    //    //            var errorMessages = string.Join("; ", validationResults.Select(v => v.ErrorMessage));
    //    //            logger.LogWarning("Validation failed: {Errors}", errorMessages);

    //    //            return Results.BadRequest(new ProblemDetails
    //    //            {
    //    //                Title = "Validation Failed",
    //    //                Detail = errorMessages,
    //    //                Status = StatusCodes.Status400BadRequest,
    //    //                Instance = $"/api/v2/classifiedfo/stores-search"
    //    //            });
    //    //        }

    //    //        string indexName = ConstantValues.IndexNames.ClassifiedStoresIndex;
               
    //    //        var request = new CommonSearchRequest
    //    //        {
    //    //            Text = req.Text,
    //    //            Filters = req.Filters,
    //    //            OrderBy = req.OrderBy,
    //    //            PageNumber = req.PageNumber,
    //    //            PageSize = req.PageSize
    //    //        };
    //    //        if (indexName == null)
    //    //        {
    //    //            return Results.BadRequest(new ProblemDetails
    //    //            {
    //    //                Title = "Invalid SubVertical",
    //    //                Detail = $"Unsupported subVertical value: '{req.SubVertical}'",
    //    //                Status = StatusCodes.Status400BadRequest,
    //    //                Instance = $"/api/v2/classifiedfo/stores-search"
    //    //            });
    //    //        }

    //    //        try
    //    //        {
    //    //            var results = await svc.SearchAsync(indexName, request);
    //    //            return Results.Ok(results);
    //    //        }
    //    //        catch (ArgumentException ex)
    //    //        {
    //    //            logger.LogWarning(ex, "Invalid search request");
    //    //            return Results.BadRequest(new ProblemDetails
    //    //            {
    //    //                Title = "Invalid Request",
    //    //                Detail = ex.Message,
    //    //                Status = StatusCodes.Status400BadRequest,
    //    //                Instance = $"/api/v2/classifiedfo/stores-search"
    //    //            });
    //    //        }
    //    //        catch (Exception ex)
    //    //        {
    //    //            logger.LogError(ex, "Unhandled exception during search");
    //    //            return Results.Problem(
    //    //                title: "Search Error",
    //    //                detail: ex.Message,
    //    //                statusCode: StatusCodes.Status500InternalServerError,
    //    //                instance: $"/api/classifieds/search"
    //    //            );
    //    //        }
    //    //    })
    //    //    .WithName("SearchClassifiedsStores")
    //    //    .WithTags("Classified")
    //    //    .WithSummary("Classified stores search and products")
    //    //    .Produces(StatusCodes.Status200OK)
    //    //    .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
    //    //    .ProducesProblem(StatusCodes.Status500InternalServerError);
    //    //    return group;
    //    //}
    //}
}
