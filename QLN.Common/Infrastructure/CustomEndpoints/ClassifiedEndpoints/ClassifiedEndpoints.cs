using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.Model;
using QLN.Common.Infrastructure.IService.BannerService;
using System.Security.Claims;

namespace QLN.Common.Infrastructure.CustomEndpoints.ClassifiedEndpoints
{
    public static class ClassifiedEndpoints
    {
        /// <summary>
        /// Maps Classified endpoints: search, detail, upload, and landing.
        /// </summary>
        public static RouteGroupBuilder MapClassifiedEndpoints(this RouteGroupBuilder group)
        {
            // POST /api/{vertical}/search
            group.MapPost("/search", async (
                    [FromRoute] string vertical,
                    [FromBody] ClassifiedSearchRequest req,
                    [FromServices] IClassifiedService svc)
                =>
            {
                if (string.IsNullOrWhiteSpace(vertical))
                    return Results.BadRequest(new { Message = "Vertical is required in route." });

                try
                {
                    var results = await svc.SearchAsync(vertical, req);
                    return Results.Ok(results);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { Message = ex.Message });
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "Search Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
            .WithName("SearchClassified")
            .WithTags("Classified");

            // GET /api/{vertical}/{id}
            group.MapGet("/{id}", async (
                    [FromRoute] string vertical,
                    [FromRoute] string id,
                    [FromServices] IClassifiedService svc)
                =>
            {
                if (string.IsNullOrWhiteSpace(vertical) || string.IsNullOrWhiteSpace(id))
                    return Results.BadRequest(new { Message = "Vertical and Id are required." });

                try
                {
                    var ad = await svc.GetByIdAsync(vertical, id);
                    return ad is null ? Results.NotFound() : Results.Ok(ad);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { Message = ex.Message });
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "Lookup Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
            .WithName("GetClassifiedById")
            .WithTags("Classified");

            // POST /api/{vertical}/upload
            group.MapPost("/upload", async (
                    [FromRoute] string vertical,
                    [FromBody] ClassifiedIndexDto doc,
                    [FromServices] IClassifiedService svc)
                =>
            {
                if (string.IsNullOrWhiteSpace(vertical) || doc == null)
                    return Results.BadRequest(new { Message = "Vertical and document payload are required." });

                try
                {
                    var msg = await svc.UploadAsync(vertical, doc);
                    return Results.Ok(new { Message = msg });
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { Message = ex.Message });
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "Upload Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
            .WithName("UploadClassified")
            .WithTags("Classified");

            //adding category

            group.MapPost("/api/category", async Task<IResult> (
                AdCategory category,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    var created = await service.AddCategory(category, token);
                    return TypedResults.Created($"/api/category/{created.Id}", created);
                }
                catch (InvalidDataException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Data",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: "An unexpected error occurred.",
                        statusCode: StatusCodes.Status500InternalServerError
                        );
                }
            })
                .WithName("AddCategory")
                .WithTags("Category")
                .WithSummary("Create a new ad category")
                .WithDescription("Adds a new ad category")
                .Produces<AdCategory>(StatusCodes.Status201Created)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // fetching categories

            group.MapGet("/api/categories", async Task<IResult> (
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    var categories = await service.GetAllCategories(token);
                    return TypedResults.Ok(categories);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                            title: "Fetch Error",
                            detail: ex.Message,
                            statusCode: StatusCodes.Status500InternalServerError
                            );
                }
            })
                .WithName("GetAllCategories")
                .WithTags("Category")
                .WithSummary("Get all ad categories")
                .WithDescription("Fetches all available ad categories from Dapr store")
                .Produces<List<AdCategory>>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


            // adding subcategory
            group.MapPost("/api/subcategory", async Task<IResult> (
                AdSubCategory subCategory,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    var created = await service.AddSubCategory(subCategory, token);
                    return TypedResults.Created($"/api/subcategory/{created.Id}", created);
                }
                catch (InvalidDataException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Data",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: "An unexpected error occurred.",
                        statusCode: StatusCodes.Status500InternalServerError
                        );
                }
            })
                .WithName("AddSubCategory")
                .WithTags("SubCategory")
                .WithSummary("Create a new subcategory")
                .WithDescription("Adds a subcategory linked to a category.")
                .Produces<AdSubCategory>(StatusCodes.Status201Created)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


            // fetching subcategories

            group.MapGet("/api/subcategory", async Task<IResult> (
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    var subCategories = await service.GetAllSubCategories(token);
                    return TypedResults.Ok(subCategories);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Fetch Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                        );
                }
            })
                .WithName("GetAllSubCategories")
                .WithTags("SubCategory")
                .WithSummary("Get all subcategories")
                .WithDescription("Fetches all available subcategories from Dapr store.")
                .Produces<List<AdSubCategory>>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // fetching subcategories by category id

            group.MapGet("/api/subcategory/by-category/{categoryId:guid}", async Task<IResult> (
                Guid categoryId,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    var subCategories = await service.GetSubCategoriesByCategoryId(categoryId, token);
                    return TypedResults.Ok(subCategories);
                }
                catch(Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Fetch Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                        );
                }
            })
                .WithName("GetSubCategoriesByCategoryId")
                .WithTags("SubCategory")
                .WithSummary("Get subcategories by category ID")
                .WithDescription("Fetches all subcategories related to a given category from Dapr state store.")
                .Produces<List<AdSubCategory>>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // adding brand

            group.MapPost("/api/brand", async Task<IResult> (
                AdBrand brand,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    var result = await service.AddBrand(brand);
                    return TypedResults.Created($"/api/brand/{result.Id}", result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Add Brand Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
                .WithName("AddBrand")
                .WithTags("Brand")
                .WithSummary("Add a new brand")
                .WithDescription("Adds a brand entry linked to a subcategory in the Dapr state store.")
                .Produces<AdBrand>(StatusCodes.Status201Created)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // fetching all brands

            group.MapGet("/api/brand", async Task<IResult> (
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    var brands = await service.GetAllBrands();
                    return TypedResults.Ok(brands);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Fetch Brands Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
                .WithName("GetAllBrands")
                .WithTags("Brand")
                .WithSummary("Get all brands")
                .WithDescription("Fetches all brand records from the Dapr state store.")
                .Produces<List<AdBrand>>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


            // fetching brands by subcategory id

            group.MapGet("/api/brand/by-subcategory/{subCategoryId:guid}", async Task<IResult> (
                Guid subCategoryId,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    var brands = await service.GetBrandsBySubCategoryId(subCategoryId);
                    return TypedResults.Ok(brands);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Fetch Brands by SubCategory Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
                .WithName("GetBrandsBySubCategoryId")
                .WithTags("Brand")
                .WithSummary("Get brands by subcategory ID")
                .WithDescription("Fetches all brands that belong to the given subcategory from the Dapr state store.")
                .Produces<List<AdBrand>>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


            // adding model

            group.MapPost("/api/model", async Task<IResult> (
                AdModel model,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    var result = await service.AddModel(model, token);
                    return TypedResults.Created($"/api/model/{result.Id}", result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Add Model Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
                .WithName("AddModel")
                .WithTags("Model")
                .WithSummary("Add a new model")
                .WithDescription("Adds a model linked to a brand.")
                .Produces<AdModel>(StatusCodes.Status201Created)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // fetching all models

            group.MapGet("/api/model", async Task<IResult> (
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    var models = await service.GetAllModels(token);
                    return TypedResults.Ok(models);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Fetch Models Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
                .WithName("GetAllModels")
                .WithTags("Model")
                .WithSummary("Get all models")
                .WithDescription("Fetches all models from the Dapr state store.")
                .Produces<List<AdModel>>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // fetching models by brand id

            group.MapGet("/api/model/by-brand/{brandId:guid}", async Task<IResult> (
                Guid brandId,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    var models = await service.GetModelsByBrandId(brandId, token);
                    return TypedResults.Ok(models);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Fetch Models by Brand Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
                .WithName("GetModelsByBrandId")
                .WithTags("Model")
                .WithSummary("Get models by brand ID")
                .WithDescription("Fetches all models related to a given brand.")
                .Produces<List<AdModel>>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // Adding condition

            group.MapPost("/api/condition", async Task<IResult> (
                AdCondition condition,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    var result = await service.AddCondition(condition, token);
                    return TypedResults.Created($"/api/condition/{result.Id}", result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Add Condition Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
                .WithName("AddCondition")
                .WithTags("Condition")
                .WithSummary("Add a condition")
                .WithDescription("Adds a new product condition to the store.")
                .Produces<AdCondition>(StatusCodes.Status201Created)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // Get all Condition
            group.MapGet("/api/condition", async Task<IResult> (
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    var result = await service.GetAllConditions(token);
                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Get Conditions Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
                .WithName("GetAllConditions")
                .WithTags("Condition")
                .WithSummary("Get all conditions")
                .WithDescription("Fetches all condition records.")
                .Produces<List<AdCondition>>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // adding color

            group.MapPost("/api/color", async Task<IResult> (
                AdColor color,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    var result = await service.AddColor(color, token);
                    return TypedResults.Created($"/api/color/{result.Id}", result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Add Color Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
                .WithName("AddColor")
                .WithTags("Color")
                .WithSummary("Add a color")
                .WithDescription("Adds a new color value to the store.")
                .Produces<AdColor>(StatusCodes.Status201Created)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // get all color

            group.MapGet("/api/color", async Task<IResult> (
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    var result = await service.GetAllColors(token);
                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Get Colors Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
                .WithName("GetAllColors")
                .WithTags("Color")
                .WithSummary("Get all colors")
                .WithDescription("Fetches all color records.")
                .Produces<List<AdColor>>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // adding capacity
            group.MapPost("/api/capacity", async Task<IResult> (
                AdCapacity capacity,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    var result = await service.AddCapacity(capacity, token);
                    return TypedResults.Created($"/api/capacity/{result.Id}", result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Add Capacity Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
                .WithName("AddCapacity")
                .WithTags("Capacity")
                .WithSummary("Add a capacity value")
                .WithDescription("Adds a new storage capacity like '64GB', '256GB' etc.")
                .Produces<AdCapacity>(StatusCodes.Status201Created)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // get all capacity
            group.MapGet("/api/capacity", async Task<IResult> (
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    var result = await service.GetAllCapacities(token);
                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Get Capacities Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
                .WithName("GetAllCapacities")
                .WithTags("Capacity")
                .WithSummary("Get all capacity values")
                .WithDescription("Fetches all available capacities from Dapr store.")
                .Produces<List<AdCapacity>>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // adding processor
            group.MapPost("/api/processor", async Task<IResult> (
                AdProcessor processor,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    var result = await service.AddProcessor(processor, token);
                    return TypedResults.Created($"/api/processor/{result.Id}", result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Add Processor Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
                .WithName("AddProcessor")
                .WithTags("Processor")
                .WithSummary("Add a processor")
                .WithDescription("Adds a new processor (e.g., Snapdragon 8, A16 Bionic) linked to a model.")
                .Produces<AdProcessor>(StatusCodes.Status201Created)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            //get processorby id 
            group.MapGet("/api/processor/by-model/{modelId:guid}", async Task<IResult> (
                Guid modelId,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    var result = await service.GetProcessorsByModelId(modelId, token);
                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Fetch Processor Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
                .WithName("GetProcessorsByModelId")
                .WithTags("Processor")
                .WithSummary("Get processors by model")
                .WithDescription("Fetches processors like A16, A15 etc. based on selected mobile model.")
                .Produces<List<AdProcessor>>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // get all processor 
            group.MapGet("/api/processor", async Task<IResult> (
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    var result = await service.GetAllProcessors(token);
                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Get All Processors Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
                .WithName("GetAllProcessors")
                .WithTags("Processor")
                .WithSummary("Get all processors")
                .WithDescription("Returns all processors from the store.")
                .Produces<List<AdProcessor>>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // adding coverage
            group.MapPost("/api/coverage", async Task<IResult> (
                AdCoverage coverage,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    var result = await service.AddCoverage(coverage, token);
                    return TypedResults.Created($"/api/coverage/{result.Id}", result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Add Coverage Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
                .WithName("AddCoverage")
                .WithTags("Coverage")
                .WithSummary("Add a coverage option")
                .WithDescription("Adds a coverage label such as 'Under Warranty' ")
                .Produces<AdCoverage>(StatusCodes.Status201Created)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // Get coverage list

            group.MapGet("/api/coverage", async Task<IResult> (
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    var result = await service.GetAllCoverages(token);
                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Get Coverage Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
                .WithName("GetAllCoverages")
                .WithTags("Coverage")
                .WithSummary("Get all coverage options")
                .WithDescription("Fetches all warranty/coverage types from the store.")
                .Produces<List<AdCoverage>>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // adding ram
            group.MapPost("/api/ram", async Task<IResult> (
                AdRam ram,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    var result = await service.AddRam(ram, token);
                    return TypedResults.Created($"/api/ram/{result.Id}", result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Add RAM Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
                .WithName("AddRam")
                .WithTags("RAM")
                .WithSummary("Add RAM")
                .WithDescription("Adds a RAM option.")
                .Produces<AdRam>(StatusCodes.Status201Created)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


            // Get all RAMs
            group.MapGet("/api/ram", async Task<IResult> (
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    var result = await service.GetAllRams(token);
                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Get RAM Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("GetAllRams")
            .WithTags("RAM")
            .WithSummary("Get all RAM values")
            .WithDescription("Fetches all RAM sizes from the store.")
            .Produces<List<AdRam>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // Get RAMs by model
            group.MapGet("/api/ram/by-model/{modelId:guid}", async Task<IResult> (
                Guid modelId,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    var result = await service.GetRamsByModelId(modelId, token);
                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Get RAM by Model Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("GetRamsByModelId")
            .WithTags("RAM")
            .WithSummary("Get RAM by model")
            .WithDescription("Returns RAM values for a specific model.")
            .Produces<List<AdRam>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // Add Resolution
            group.MapPost("/api/resolution", async Task<IResult> (
                AdResolution resolution,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    var result = await service.AddResolution(resolution, token);
                    return TypedResults.Created($"/api/resolution/{result.Id}", result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Add Resolution Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("AddResolution")
            .WithTags("Resolution")
            .WithSummary("Add resolution")
            .WithDescription("Adds a resolution like '1080x2400'.")
            .Produces<AdResolution>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // Get all Resolutions
            group.MapGet("/api/resolution", async Task<IResult> (
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    var result = await service.GetAllResolutions(token);
                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Get Resolution Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("GetAllResolutions")
            .WithTags("Resolution")
            .WithSummary("Get all resolutions")
            .WithDescription("Returns all screen resolutions from store.")
            .Produces<List<AdResolution>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // Get Resolutions by model
            group.MapGet("/api/resolution/by-model/{modelId:guid}", async Task<IResult> (
                Guid modelId,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    var result = await service.GetResolutionsByModelId(modelId, token);
                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Get Resolution by Model Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("GetResolutionsByModelId")
            .WithTags("Resolution")
            .WithSummary("Get resolutions by model")
            .WithDescription("Returns resolutions for a selected model.")
            .Produces<List<AdResolution>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


            // POST /api/size-type
            group.MapPost("/api/size-type", async Task<IResult> (
                AdSizeType sizeType,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    var result = await service.AddSizeType(sizeType, token);
                    return TypedResults.Created($"/api/size-type/{result.Id}", result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Add SizeType Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("AddSizeType")
            .WithTags("SizeType")
            .WithSummary("Add a size type")
            .WithDescription("Adds size labels like 'Small', 'Medium', 'XL'.")
            .Produces<AdSizeType>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


            // GET /api/size-type
            group.MapGet("/api/size-type", async Task<IResult> (
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    var result = await service.GetAllSizeTypes(token);
                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Get SizeType Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("GetAllSizeTypes")
            .WithTags("SizeType")
            .WithSummary("Get all size types")
            .WithDescription("Fetches size options from Dapr store.")
            .Produces<List<AdSizeType>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // POST /api/gender
            group.MapPost("/api/gender", async Task<IResult> (
                AdGender gender,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    var result = await service.AddGender(gender, token);
                    return TypedResults.Created($"/api/gender/{result.Id}", result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Add Gender Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("AddGender")
            .WithTags("Gender")
            .WithSummary("Add a gender type")
            .WithDescription("Adds gender like 'Male', 'Female'.")
            .Produces<AdGender>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // GET /api/gender
            group.MapGet("/api/gender", async Task<IResult> (
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    var result = await service.GetAllGenders(token);
                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Get Gender Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("GetAllGenders")
            .WithTags("Gender")
            .WithSummary("Get all gender types")
            .WithDescription("Fetches all gender options from Dapr store.")
            .Produces<List<AdGender>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // POST /api/zone
            group.MapPost("/api/zone", async Task<IResult> (
                AdZone zone,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    var result = await service.AddZone(zone, token);
                    return TypedResults.Created($"/api/zone/{result.Id}", result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Add Zone Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("AddZone")
            .WithTags("Zone")
            .WithSummary("Add a zone number")
            .WithDescription("Adds a zone like 51, 52, etc.")
            .Produces<AdZone>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // GET /api/zone
            group.MapGet("/api/zone", async Task<IResult> (
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    var result = await service.GetAllZones(token);
                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Get Zone Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("GetAllZones")
            .WithTags("Zone")
            .WithSummary("Get all zones")
            .WithDescription("Fetches all zone numbers from the store.")
            .Produces<List<AdZone>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


            // create Ad 
            group.MapPost("/ad", async Task<IResult> (
                [FromRoute] string vertical,
                [FromForm] AdInformation ad,
                HttpContext context,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (string.IsNullOrWhiteSpace(userId))
                        return Results.Unauthorized();

                    var adKey = await service.CreateAd(ad, vertical, userId, token);

                    return TypedResults.Ok(new
                    {
                        Message = "Ad created successfully.",
                        Key = adKey
                    });
                }
                catch (UnauthorizedAccessException ex)
                {
                    return TypedResults.Problem(
                        title: "Unauthorized",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status401Unauthorized);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Ad Creation Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
                .WithName("CreateAd")
                .WithTags("Ad")
                .WithSummary("Create new classified ad")
                .WithDescription("Creates a new classified ad, validates user, stores ad in Dapr, and uploads files.")
                .Accepts<AdInformation>("multipart/form-data")
                .Produces(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .DisableAntiforgery();


            // GET /api/{vertical}/landing
            group.MapGet("/landing", async (
                    [FromRoute] string vertical,
                    [FromServices] IClassifiedService svc)
                =>
            {
                if (string.IsNullOrWhiteSpace(vertical))
                    return Results.BadRequest(new { Message = "Vertical is required in route." });

                try
                {
                    var model = await svc.GetLandingPageAsync(vertical);
                    return Results.Ok(model);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { Message = ex.Message });
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "Landing Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
            .WithName("GetClassifiedLanding")
            .WithTags("Classified");

            return group;
        }
    }
}

