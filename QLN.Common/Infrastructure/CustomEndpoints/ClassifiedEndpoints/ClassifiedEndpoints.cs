// QLN.Common.Infrastructure.CustomEndpoints.ClassifiedEndpoints.cs
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService.BannerService;
using System.Security.Claims;
using QLN.Common.Infrastructure.Model;

namespace QLN.Common.Infrastructure.CustomEndpoints.ClassifiedEndpoints
{
    public static class ClassifiedEndpoints
    {
        private const string Vertical = Constants.ConstantValues.ClassifiedsVertical;

        public static RouteGroupBuilder MapClassifiedLandingEndpoints(this RouteGroupBuilder group)
        {
            // SEARCH
            group.MapPost("/search", async (
                    [FromBody] CommonSearchRequest req,
                    [FromServices] IClassifiedService svc,
                    [FromServices] ILoggerFactory logFac
                ) =>
            {
                var logger = logFac.CreateLogger("ClassifiedEndpoints");
                if (req is null)
                {
                    logger.LogWarning("Search called with null payload");
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Bad Request",
                        Detail = "Search payload is required.",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = $"/api/{Vertical}/search"
                    });
                }

                try
                {
                    var results = await svc.Search(req);
                    return Results.Ok(results);
                }
                catch (ArgumentException ex)
                {
                    logger.LogWarning(ex, "Invalid search request");
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Request",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest,
                        Instance = $"/api/{Vertical}/search"
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Search error");
                    return Results.Problem(
                        title: "Search Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: $"/api/{Vertical}/search"
                    );
                }
            })
            .WithName("SearchClassified")
            .WithTags("Classified")
            .WithSummary("Search classifieds")
            .Produces<IEnumerable<ClassifiedIndexDto>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

            // GET BY ID
            group.MapGet("/{id}", async (
                    [FromRoute] string id,
                    [FromServices] IClassifiedService svc,
                    [FromServices] ILoggerFactory logFac
                ) =>
            {
                var logger = logFac.CreateLogger("ClassifiedEndpoints");
                if (string.IsNullOrWhiteSpace(id))
                {
                    logger.LogWarning("GetById called with empty id");
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Bad Request",
                        Detail = "Document ID is required.",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = $"/api/{Vertical}/{id}"
                    });
                }

                try
                {
                    var ad = await svc.GetById(id);
                    if (ad is null)
                        return Results.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = $"No document '{id}' in '{Vertical}'.",
                            Status = StatusCodes.Status404NotFound,
                            Instance = $"/api/{Vertical}/{id}"
                        });
                    return Results.Ok(ad);
                }
                catch (ArgumentException ex)
                {
                    logger.LogWarning(ex, "Invalid GetById request");
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Request",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest,
                        Instance = $"/api/{Vertical}/{id}"
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "GetById error");
                    return Results.Problem(
                        title: "Lookup Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: $"/api/{Vertical}/{id}"
                    );
                }
            })
            .WithName("GetClassifiedById")
            .WithTags("Classified")
            .WithSummary("Get a classified by its ID")
            .Produces<ClassifiedIndexDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

            // UPLOAD
            group.MapPost("/upload", async (
                    [FromBody] ClassifiedIndexDto doc,
                    [FromServices] IClassifiedService svc,
                    [FromServices] ILoggerFactory logFac
                ) =>
            {
                var logger = logFac.CreateLogger("ClassifiedEndpoints");
                if (doc is null)
                {
                    logger.LogWarning("Upload called with null document");
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Bad Request",
                        Detail = "Document payload is required.",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = $"/api/{Vertical}/upload"
                    });
                }

                try
                {
                    var msg = await svc.Upload(doc);
                    return Results.Ok(msg);
                }
                catch (ArgumentException ex)
                {
                    logger.LogWarning(ex, "Invalid upload request");
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Request",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest,
                        Instance = $"/api/{Vertical}/upload"
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Upload error");
                    return Results.Problem(
                        title: "Upload Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: $"/api/{Vertical}/upload"
                    );
                }
            })
            .WithName("UploadClassified")
            .WithTags("Classified")
            .WithSummary("Upload or create a classified item")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

            // UPDATE
            group.MapPut("/update", async (
                    [FromBody] ClassifiedIndexDto doc,
                    [FromServices] IClassifiedService svc,
                    [FromServices] ILoggerFactory logFac
                ) =>
            {
                var logger = logFac.CreateLogger("ClassifiedEndpoints");
                if (doc is null || string.IsNullOrWhiteSpace(doc.Id))
                {
                    logger.LogWarning("Update called with invalid payload");
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Bad Request",
                        Detail = "Document payload with valid Id is required.",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = $"/api/{Vertical}/update"
                    });
                }

                try
                {
                    var msg = await svc.Upload(doc);
                    return Results.Ok(msg);
                }
                catch (ArgumentException ex)
                {
                    logger.LogWarning(ex, "Invalid update request");
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Request",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest,
                        Instance = $"/api/{Vertical}/update"
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Update error");
                    return Results.Problem(
                        title: "Update Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: $"/api/{Vertical}/update"
                    );
                }
            })
            .WithName("UpdateClassified")
            .WithTags("Classified")
            .WithSummary("Update an existing classified item")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

            //adding category

            group.MapPost("/category", async Task<IResult> (
                [FromBody] string categoryName,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    var created = await service.AddCategory(categoryName, token);
                    return TypedResults.Created($"/api/classified/category/{created.Id}", created);
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
                .WithDescription("Adds a new ad category into the unified adstore")
                .Produces<Adcateg>(StatusCodes.Status201Created)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // fetching categories

            group.MapGet("/categories", async Task<IResult> (
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
                .Produces<List<Adcateg>>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


            // adding subcategory
            group.MapPost("/subcategory", async Task<IResult> (
                [FromBody] AddSubCategoryRequest dto,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    var created = await service.AddSubCategory(dto.Name, dto.CategoryId, token);
                    return TypedResults.Created($"/api/classified/subcategory/{created.Id}", created);
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

            group.MapGet("/subcategory", async Task<IResult> (
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

            group.MapGet("/subcategory/by-category/{categoryId:guid}", async Task<IResult> (
                Guid categoryId,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    var subCategories = await service.GetSubCategoriesByCategoryId(categoryId);
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

            group.MapPost("/brand", async Task<IResult> (
                [FromBody] AddBrandRequest dto,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    var result = await service.AddBrand(dto.Name, dto.SubCategoryId);
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

            group.MapGet("/brand", async Task<IResult> (
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

            group.MapGet("/brand/by-subcategory/{subCategoryId:guid}", async Task<IResult> (
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

            group.MapPost("/model", async Task<IResult> (
                [FromBody] AddModelRequest dto,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    var result = await service.AddModel(dto.Name, dto.BrandId);
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

            group.MapGet("/model", async Task<IResult> (
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

            group.MapGet("/model/by-brand/{brandId:guid}", async Task<IResult> (
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

            group.MapPost("/condition", async Task<IResult> (
                [FromBody] AddConditionRequest dto,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    var result = await service.AddCondition(dto.Name, token);
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
            group.MapGet("/condition", async Task<IResult> (
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

            group.MapPost("/color", async Task<IResult> (
                [FromBody] AdColorRequest dto,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    var result = await service.AddColor(dto.Name, token);
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

            group.MapGet("/color", async Task<IResult> (
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
            group.MapPost("/capacity", async Task<IResult> (
                [FromBody] AdCapacityRequest dto,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    var result = await service.AddCapacity(dto.Name, token);
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
            group.MapGet("/capacity", async Task<IResult> (
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
            group.MapPost("/processor", async Task<IResult> (
                [FromBody] AdProcessorRequest dto,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    var result = await service.AddProcessor(dto.Name, dto.ModelId, token);
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
            group.MapGet("/processor/by-model/{modelId:guid}", async Task<IResult> (
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
            group.MapGet("/processor", async Task<IResult> (
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
            group.MapPost("/coverage", async Task<IResult> (
                [FromBody] AdCoverageRequest dto,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    var result = await service.AddCoverage(dto.Name);
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

            group.MapGet("/coverage", async Task<IResult> (
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
            group.MapPost("/ram", async Task<IResult> (
                [FromBody] AdRamRequest dto,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    var result = await service.AddRam(dto.Name, dto.ModelId);
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
            group.MapGet("/ram", async Task<IResult> (
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
            group.MapGet("/ram/by-model/{modelId:guid}", async Task<IResult> (
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
            group.MapPost("/resolution", async Task<IResult> (
                [FromBody] AdResolutionRequest dto,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    var result = await service.AddResolution(dto.Name, dto.ModelId, token);
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
            group.MapGet("/resolution", async Task<IResult> (
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
            group.MapGet("/resolution/by-model/{modelId:guid}", async Task<IResult> (
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


            // POST /size-type
            group.MapPost("/size-type", async Task<IResult> (
                [FromBody] AdSizeRequest dto,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    var result = await service.AddSizeType(dto.Name, token);
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


            // GET /size-type
            group.MapGet("/size-type", async Task<IResult> (
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

            // POST /gender
            group.MapPost("/gender", async Task<IResult> (
                [FromBody] AdGenderRequest dto,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    var result = await service.AddGender(dto.Name, token);
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

            // GET /gender
            group.MapGet("/gender", async Task<IResult> (
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

            // POST /zone
            group.MapPost("/zone", async Task<IResult> (
                [FromBody] AdZoneRequest dto,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    var result = await service.AddZone(dto.Name, token);
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

            // GET /zone
            group.MapGet("/zone", async Task<IResult> (
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
            //group.MapPost("/ad", async Task<IResult> (
            //    [FromForm] AdInformation ad,
            //    HttpContext context,
            //    IClassifiedService service,
            //    CancellationToken token) =>
            //{
            //    try
            //    {
            //        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            //        if (string.IsNullOrWhiteSpace(userId))
            //            return Results.Unauthorized();


            //        if (string.IsNullOrWhiteSpace(ad.SubVertical))
            //        {
            //            return Results.BadRequest(new ProblemDetails
            //            {
            //                Title = "Invalid Request",
            //                Detail = "SubVertical is required in the form data.",
            //                Status = StatusCodes.Status400BadRequest
            //            });
            //        }

            //        var adKey = await service.CreateAd(ad, userId, token);

            //        return TypedResults.Ok(new
            //        {
            //            Message = "Ad created successfully.",
            //            Key = adKey
            //        });
            //    }
            //    catch (UnauthorizedAccessException ex)
            //    {
            //        return TypedResults.Problem(
            //            title: "Unauthorized",
            //            detail: ex.Message,
            //            statusCode: StatusCodes.Status401Unauthorized);
            //    }
            //    catch (Exception ex)
            //    {
            //        return TypedResults.Problem(
            //            title: "Ad Creation Error",
            //            detail: ex.Message,
            //            statusCode: StatusCodes.Status500InternalServerError);
            //    }
            //})
            //    .WithName("CreateAd")
            //    .WithTags("Ad")
            //    .WithSummary("Create new classified ad")
            //    .WithDescription("Creates a new classified ad, validates user, stores ad in Dapr, and uploads files.")
            //    .Accepts<AdInformation>("multipart/form-data")
            //    .Produces(StatusCodes.Status200OK)
            //    .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            //    .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
            //    .DisableAntiforgery()
            //    .RequireAuthorization();

            // get user ad's
            //group.MapGet("/ad/user", async Task<IResult> (
            //    HttpContext context,
            //    [FromQuery] bool? isPublished,
            //    IClassifiedService service,
            //    CancellationToken token) =>
            //{
            //    var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            //    if (string.IsNullOrWhiteSpace(userId))
            //        return Results.Unauthorized();

            //    var ads = await service.GetUserAds(userId, isPublished, token);
            //    return TypedResults.Ok(ads);
            //})
            //    .WithName("GetUserAds")
            //    .WithTags("Ad")
            //    .WithSummary("Get user ads")
            //    .WithDescription("Returns ads created by the logged-in user filtered by IsPublished status")
            //    .Produces<List<AdResponse>>(StatusCodes.Status200OK)
            //    .Produces(StatusCodes.Status401Unauthorized)
            //    .RequireAuthorization();


            // GET /api/{vertical}/landing
            group.MapGet("/landing", async (
                    [FromServices] IClassifiedService svc,
                    [FromServices] ILoggerFactory logFac
                ) =>
            {
                var logger = logFac.CreateLogger("ClassifiedEndpoints");
                try
                {
                    var model = await svc.GetLandingPage();
                    return Results.Ok(model);
                }
                catch (ArgumentException ex)
                {
                    logger.LogWarning(ex, "Invalid landing request");
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Request",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest,
                        Instance = $"/api/{Vertical}/landing"
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Landing error");
                    return Results.Problem(
                        title: "Landing Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: $"/api/{Vertical}/landing"
                    );
                }
            })
            .WithName("GetClassifiedLanding")
            .WithTags("Classified")
            .WithSummary("Get landing-page data for classifieds")
            .Produces<ClassifiedLandingPageResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

            return group;
        }
    }
}
