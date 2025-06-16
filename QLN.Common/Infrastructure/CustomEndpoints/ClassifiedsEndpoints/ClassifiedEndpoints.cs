using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using QLN.Common.DTO_s;
using System.Security.Claims;
using QLN.Common.Infrastructure.Model;
using Microsoft.AspNetCore.Http.HttpResults;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService;
using QLN.Common.Infrastructure.Utilities;
using QLN.Common.Infrastructure.CustomException;
using System.ComponentModel.DataAnnotations;
using QLN.Common.Infrastructure.IService.ISearchService;
using QLN.Common.Infrastructure.Constants;
using Microsoft.AspNetCore.Authorization;

namespace QLN.Common.Infrastructure.CustomEndpoints.ClassifiedEndpoints
{
    public static class ClassifiedEndpoints
    {
        public static RouteGroupBuilder MapClassifiedEndpoints(this RouteGroupBuilder group)
        {
            // SEARCH
            group.MapPost("/search", async (
                    [FromBody] CommonSearchRequest req,
                    [FromServices] ISearchService svc,
                    [FromServices] ILoggerFactory logFac
                ) =>
            {
                var logger = logFac.CreateLogger("ClassifiedEndpoints");

                var validationContext = new ValidationContext(req);
                var validationResults = new List<ValidationResult>();
                if (!Validator.TryValidateObject(req, validationContext, validationResults, validateAllProperties: true))
                {
                    var errorMessages = string.Join("; ", validationResults.Select(v => v.ErrorMessage));
                    logger.LogWarning("Validation failed: {Errors}", errorMessages);

                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Failed",
                        Detail = errorMessages,
                        Status = StatusCodes.Status400BadRequest,
                        Instance = $"/api/classified/search"
                    });
                }

                try
                {
                    var results = await svc.SearchAsync(ConstantValues.ClassifiedsVertical, req);
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
                        Instance = $"/api/classified/search"
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Unhandled exception during search");
                    return Results.Problem(
                        title: "Search Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: $"/api/classified/search"
                    );
                }
            })
            .WithName("SearchClassified")
            .WithTags("Classified")
            .WithSummary("Search classifieds")
            .Produces<IEnumerable<ClassifiedsIndex>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

            // GET BY ID
            group.MapGet("/{id}", async (
                    [FromRoute] string id,
                    [FromServices] ISearchService svc,
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
                        Instance = $"/api/classified/{id}"
                    });
                }

                try
                {
                    var ad = await svc.GetByIdAsync<ClassifiedsIndex>(ConstantValues.Verticals.Classifieds, id);
                    if (ad is null)
                        return Results.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = $"No document '{id}' in 'classifieds'.",
                            Status = StatusCodes.Status404NotFound,
                            Instance = $"/api/classified/{id}"
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
                        Instance = $"/api/classified/{id}"
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "GetById error");
                    return Results.Problem(
                        title: "Lookup Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: $"/api/classified/{id}"
                    );
                }
            })
            .WithName("GetClassifiedById")
            .WithTags("Classified")
            .WithSummary("Get a classified by its ID")
            .Produces<ClassifiedsIndex>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);


            // UPLOAD
            group.MapPost("/upload", async (
                    [FromBody] ClassifiedsIndex doc,
                    [FromServices] ISearchService svc,
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
                        Instance = $"/api/classified/upload"
                    });
                }
                var indexDocument = new CommonIndexRequest
                {
                    VerticalName = ConstantValues.Verticals.Classifieds,
                    ClassifiedsItem = doc
                };
                try
                {
                    var msg = await svc.UploadAsync(indexDocument);
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
                        Instance = $"/api/classified/upload"
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Upload error");
                    return Results.Problem(
                        title: "Upload Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: $"/api/classified/upload"
                    );
                }
            })
            .WithName("UploadClassified")
            .WithTags("Classified")
            .WithSummary("Upload or create a classified item")
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
                    if (string.IsNullOrWhiteSpace(categoryName))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "Category name cannot be null or empty.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }
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
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = $"category not found.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    else if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: "An unexpected error occurred.",
                        statusCode: StatusCodes.Status500InternalServerError
                        );
                }
            })
                .WithName("AddCategory")
                .WithTags("ClassifiedsDropdown")
                .WithSummary("Create a new ad category")
                .WithDescription("Adds a new ad category into the unified adstore")
                .Produces<Category>(StatusCodes.Status201Created)
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

                    if (categories == null || !categories.Any())
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "No Categories Found",
                            Detail = "The category store is empty or not available.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }

                    return TypedResults.Ok(categories);
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.Problem(new ProblemDetails
                    {
                        Title = "Internal Operation Error",
                        Detail = ex.Message,
                        Status = StatusCodes.Status500InternalServerError
                    });
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = $"category not found.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    else if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    return TypedResults.Problem(new ProblemDetails
                    {
                        Title = "Unexpected Error",
                        Detail = "An unexpected error occurred while fetching categories.",
                        Status = StatusCodes.Status500InternalServerError
                    });
                }
            })
                .WithName("GetAllCategories")
                .WithTags("ClassifiedsDropdown")
                .WithSummary("Get all ad categories")
                .WithDescription("Fetches all available ad categories from Dapr store")
                .Produces<List<CategoriesDto>>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


            // adding subcategory
            group.MapPost("/subcategory", async Task<IResult> (
                [FromBody] AddSubCategoryRequest dto,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(dto.Name))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "Subcategory name cannot be empty.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }
                    if (dto.CategoryId == Guid.Empty)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "Parent Category ID is required.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }
                    var created = await service.AddSubCategory(dto.Name, dto.CategoryId, token);
                    return TypedResults.Created($"/api/classified/subcategory/{created.Id}", created);
                }
                catch (ArgumentException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Argument",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
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
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = $"sub category not found.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    return TypedResults.Problem(new ProblemDetails
                    {
                        Title = "Unexpected Error",
                        Detail = "An unexpected error occurred.",
                        Status = StatusCodes.Status500InternalServerError
                    });
                }
            })
                .WithName("AddSubCategory")
                .WithTags("ClassifiedsDropdown")
                .WithSummary("Create a new subcategory")
                .WithDescription("Adds a subcategory linked to a category.")
                .Produces<Category>(StatusCodes.Status201Created)
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
                    if (subCategories == null || !subCategories.Any())
                    {
                        return TypedResults.Problem(new ProblemDetails
                        {
                            Title = "No Data",
                            Detail = "No subcategories found in the store.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    return TypedResults.Ok(subCategories);
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.Problem(new ProblemDetails
                    {
                        Title = "Operation Failed",
                        Detail = ex.Message,
                        Status = StatusCodes.Status500InternalServerError
                    });
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = $"sub category not found.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    else if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    return TypedResults.Problem(new ProblemDetails
                    {
                        Title = "Unexpected Error",
                        Detail = "An unexpected error occurred while retrieving subcategories.",
                        Status = StatusCodes.Status500InternalServerError
                    });
                }
            })
                .WithName("GetAllSubCategories")
                .WithTags("ClassifiedsDropdown")
                .WithSummary("Get all subcategories")
                .WithDescription("Fetches all available subcategories from Dapr store.")
                .Produces<List<CategoriesDto>>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // fetching subcategories by category id

            group.MapGet("/subcategory/by-category/{categoryId:guid}", async Task<IResult> (
                Guid categoryId,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    var result = await service.GetCategoryWithSubCategories(categoryId);

                    if (result is null)
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Category Not Found",
                            Detail = $"No category found with ID: {categoryId}",
                            Status = StatusCodes.Status404NotFound
                        });
                    }

                    return TypedResults.Ok(result);
                }
                catch (ArgumentException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Input",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (KeyNotFoundException ex)
                {
                    return TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Category Not Found",
                        Detail = ex.Message,
                        Status = StatusCodes.Status404NotFound
                    });
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = $"category with {categoryId} was not found.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    else if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: "An unexpected error occurred.",
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
                .WithName("GetCategoryWithSubCategories")
                .WithTags("ClassifiedsDropdown")
                .WithSummary("Get category with subcategories")
                .WithDescription("Fetches a category and its related subcategories from Dapr store.")
                .Produces<CategoryDto>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // adding brand

            group.MapPost("/brand", async Task<IResult> (
                [FromBody] AddBrandRequest dto,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(dto.Name))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "Brand name must not be empty.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    if (dto.SubCategoryId == Guid.Empty)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "SubCategory ID is required.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.AddBrand(dto.Name, dto.SubCategoryId);
                    return TypedResults.Created($"/api/brand/{result.Id}", result);
                }
                catch (ArgumentException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Input",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.Problem(new ProblemDetails
                    {
                        Title = "Operation Failed",
                        Detail = ex.Message,
                        Status = StatusCodes.Status409Conflict
                    });
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = $"brand not found.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    else if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: "An unexpected error occurred.",
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
                .WithName("AddBrand")
                .WithTags("ClassifiedsDropdown")
                .WithSummary("Add a new brand")
                .WithDescription("Adds a brand entry linked to a subcategory in the Dapr state store.")
                .Produces<Category>(StatusCodes.Status201Created)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // fetching all brands

            group.MapGet("/brand", async Task<IResult> (
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    var brands = await service.GetAllBrands();

                    if (brands == null || !brands.Any())
                    {
                        return TypedResults.Ok(new List<CategoriesDto>());
                    }

                    return TypedResults.Ok(brands);
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.Problem(new ProblemDetails
                    {
                        Title = "Brand Fetch Failed",
                        Detail = ex.Message,
                        Status = StatusCodes.Status409Conflict
                    });
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = $"Brand not found.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    else if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    return TypedResults.Problem(new ProblemDetails
                    {
                        Title = "Internal Server Error",
                        Detail = "An unexpected error occurred while fetching brands.",
                        Status = StatusCodes.Status500InternalServerError
                    });
                }
            })
                .WithName("GetAllBrands")
                .WithTags("ClassifiedsDropdown")
                .WithSummary("Get all brands")
                .WithDescription("Fetches all brand records from the Dapr state store.")
                .Produces<List<CategoriesDto>>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


            // fetching brands by subcategory id

            group.MapGet("/brand/by-subcategory/{subCategoryId:guid}", async Task<IResult> (
                Guid subCategoryId,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    var brands = await service.GetSubCategoryWithBrands(subCategoryId);
                    return TypedResults.Ok(brands);
                }
                catch (ArgumentException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid SubCategory ID",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (KeyNotFoundException ex)
                {
                    return TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "SubCategory Not Found",
                        Detail = ex.Message,
                        Status = StatusCodes.Status404NotFound
                    });
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = $"Not found.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    else if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    return TypedResults.Problem(
                        title: "Fetch Brands Error",
                        detail: "An unexpected error occurred while retrieving brands by subcategory.",
                        statusCode: StatusCodes.Status500InternalServerError
                        );
                }
            })
                .WithName("GetBrandsBySubCategoryId")
                .WithTags("ClassifiedsDropdown")
                .WithSummary("Get brands by subcategory ID")
                .WithDescription("Fetches all brands that belong to the given subcategory from the Dapr state store.")
                .Produces<SubCategoryWithBrandsDto>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
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
                catch (ArgumentException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Input",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.Problem(new ProblemDetails
                    {
                        Title = "Operation Failed",
                        Detail = ex.Message,
                        Status = StatusCodes.Status409Conflict
                    });
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = $"model not found.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    else if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    return TypedResults.Problem(new ProblemDetails
                    {
                        Title = "Internal Server Error",
                        Detail = "An unexpected error occurred while adding the model.",
                        Status = StatusCodes.Status500InternalServerError
                    });
                }
            })
                .WithName("AddModel")
                .WithTags("ClassifiedsDropdown")
                .WithSummary("Add a new model")
                .WithDescription("Adds a model linked to a brand.")
                .Produces<Category>(StatusCodes.Status201Created)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // fetching all models

            group.MapGet("/model", async Task<IResult> (
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    var models = await service.GetAllModels(token);
                    if (models == null || !models.Any())
                    {
                        return TypedResults.Problem(new ProblemDetails
                        {
                            Title = "No Models Found",
                            Detail = "The model list is empty or could not be retrieved.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    return TypedResults.Ok(models);
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.Problem(new ProblemDetails
                    {
                        Title = "Data Access Error",
                        Detail = ex.Message,
                        Status = StatusCodes.Status409Conflict
                    });
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = $"model not found.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    else if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    return TypedResults.Problem(new ProblemDetails
                    {
                        Title = "Internal Server Error",
                        Detail = "An unexpected error occurred while retrieving models.",
                        Status = StatusCodes.Status500InternalServerError
                    });
                }
            })
                .WithName("GetAllModels")
                .WithTags("ClassifiedsDropdown")
                .WithSummary("Get all models")
                .WithDescription("Fetches all models from the Dapr state store.")
                .Produces<List<CategoryDto>>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // fetching models by brand id

            group.MapGet("/model/by-brand/{brandId:guid}", async Task<IResult> (
                Guid brandId,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    var result = await service.GetBrandWithModels(brandId, token);
                    if (result == null || string.IsNullOrWhiteSpace(result.Name))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Brand Not Found",
                            Detail = $"No brand found with ID '{brandId}' or it contains no models.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    return TypedResults.Ok(result);
                }
                catch (ArgumentException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Brand ID",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (KeyNotFoundException ex)
                {
                    return TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Brand Not Found",
                        Detail = ex.Message,
                        Status = StatusCodes.Status404NotFound
                    });
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = $"Not found.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    else if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    return TypedResults.Problem(new ProblemDetails
                    {
                        Title = "Internal Server Error",
                        Detail = "An unexpected error occurred while fetching models for the brand.",
                        Status = StatusCodes.Status500InternalServerError
                    });
                }
            })
                .WithName("GetModelsByBrandId")
                .WithTags("ClassifiedsDropdown")
                .WithSummary("Get models by brand ID")
                .WithDescription("Fetches all models related to a given brand.")
                .Produces<BrandWithModelsDto>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // Adding condition

            group.MapPost("/condition", async Task<IResult> (
                [FromBody] AddConditionRequest dto,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(dto?.Name))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "Condition name is required.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.AddCondition(dto.Name, token);

                    if (result == null)
                    {
                        return TypedResults.Problem(new ProblemDetails
                        {
                            Title = "Creation Failed",
                            Detail = "Failed to create the condition entry. Result is null.",
                            Status = StatusCodes.Status500InternalServerError
                        });
                    }

                    return TypedResults.Created($"/api/condition/{result.Id}", result);
                }
                catch (ArgumentException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Argument",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.Problem(new ProblemDetails
                    {
                        Title = "Operation Error",
                        Detail = ex.Message,
                        Status = StatusCodes.Status500InternalServerError
                    });
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = $"Condition not found.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    else if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    return TypedResults.Problem(new ProblemDetails
                    {
                        Title = "Unexpected Error",
                        Detail = "An unexpected error occurred while adding the condition.",
                        Status = StatusCodes.Status500InternalServerError
                    });
                }
            })
                .WithName("AddCondition")
                .WithTags("ClassifiedsDropdown")
                .WithSummary("Add a condition")
                .WithDescription("Adds a new product condition to the store.")
                .Produces<Category>(StatusCodes.Status201Created)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // Get all Condition
            group.MapGet("/condition", async Task<IResult> (
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    var result = await service.GetAllConditions(token);
                    if (result == null || !result.Any())
                    {
                        return TypedResults.Problem(new ProblemDetails
                        {
                            Title = "No Conditions Found",
                            Detail = "No condition records were found in the Dapr store.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    return TypedResults.Ok(result);
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.Problem(new ProblemDetails
                    {
                        Title = "Operational Error",
                        Detail = ex.Message,
                        Status = StatusCodes.Status500InternalServerError
                    });
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = $"Condition not found.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    else if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    return TypedResults.Problem(new ProblemDetails
                    {
                        Title = "Unexpected Error",
                        Detail = "An unexpected error occurred while fetching conditions.",
                        Status = StatusCodes.Status500InternalServerError
                    });
                }
            })
                .WithName("GetAllConditions")
                .WithTags("ClassifiedsDropdown")
                .WithSummary("Get all conditions")
                .WithDescription("Fetches all condition records.")
                .Produces<List<CategoriesDto>>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // adding color

            group.MapPost("/color", async Task<IResult> (
                [FromBody] AdColorRequest dto,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    if (dto is null || string.IsNullOrWhiteSpace(dto.Name))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "Color name must not be null or empty.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }
                    var result = await service.AddColor(dto.Name, token);

                    if (result == null)
                    {
                        return TypedResults.Problem(new ProblemDetails
                        {
                            Title = "Creation Failed",
                            Detail = "Failed to create the color entry.",
                            Status = StatusCodes.Status500InternalServerError
                        });
                    }

                    return TypedResults.Created($"/api/color/{result.Id}", result);
                }
                catch (ArgumentException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Argument",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.Problem(new ProblemDetails
                    {
                        Title = "Operation Error",
                        Detail = ex.Message,
                        Status = StatusCodes.Status500InternalServerError
                    });
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = $"Color not found.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    else if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    return TypedResults.Problem(new ProblemDetails
                    {
                        Title = "Unexpected Error",
                        Detail = "An unexpected error occurred while adding the color.",
                        Status = StatusCodes.Status500InternalServerError
                    });
                }
            })
                .WithName("AddColor")
                .WithTags("ClassifiedsDropdown")
                .WithSummary("Add a color")
                .WithDescription("Adds a new color value to the store.")
                .Produces<Category>(StatusCodes.Status201Created)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // get all color

            group.MapGet("/color", async Task<IResult> (
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    var result = await service.GetAllColors(token);

                    if (result == null || !result.Any())
                    {
                        return TypedResults.Problem(new ProblemDetails
                        {
                            Title = "No Colors Found",
                            Detail = "No color records were found in the store.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }

                    return TypedResults.Ok(result);
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.Problem(new ProblemDetails
                    {
                        Title = "Data Access Error",
                        Detail = ex.Message,
                        Status = StatusCodes.Status500InternalServerError
                    });
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = $"color not found.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    else if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    return TypedResults.Problem(new ProblemDetails
                    {
                        Title = "Unexpected Error",
                        Detail = "An unexpected error occurred while retrieving colors.",
                        Status = StatusCodes.Status500InternalServerError
                    });
                }
            })
                .WithName("GetAllColors")
                .WithTags("ClassifiedsDropdown")
                .WithSummary("Get all colors")
                .WithDescription("Fetches all color records.")
                .Produces<List<CategoriesDto>>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // adding capacity
            group.MapPost("/capacity", async Task<IResult> (
                [FromBody] AdCapacityRequest dto,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(dto?.Name))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "Capacity name cannot be empty.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.AddCapacity(dto.Name, token);

                    if (result == null)
                    {
                        return TypedResults.Problem(new ProblemDetails
                        {
                            Title = "Creation Failed",
                            Detail = "Failed to create the capacity value.",
                            Status = StatusCodes.Status500InternalServerError
                        });
                    }

                    return TypedResults.Created($"/api/capacity/{result.Id}", result);
                }
                catch (ArgumentException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Argument",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.Problem(new ProblemDetails
                    {
                        Title = "Invalid Operation",
                        Detail = ex.Message,
                        Status = StatusCodes.Status500InternalServerError
                    });
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = $"Capacity not found.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    else if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    return TypedResults.Problem(new ProblemDetails
                    {
                        Title = "Unexpected Error",
                        Detail = "An unexpected error occurred while adding the capacity.",
                        Status = StatusCodes.Status500InternalServerError
                    });
                }
            })
                .WithName("AddCapacity")
                .WithTags("ClassifiedsDropdown")
                .WithSummary("Add a capacity value")
                .WithDescription("Adds a new storage capacity like '64GB', '256GB' etc.")
                .Produces<Category>(StatusCodes.Status201Created)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // get all capacity
            group.MapGet("/capacity", async Task<IResult> (
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    var result = await service.GetAllCapacities(token);

                    if (result == null || result.Count == 0)
                    {
                        return TypedResults.Problem(new ProblemDetails
                        {
                            Title = "No Capacities Found",
                            Detail = "No capacity records are available in the store.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }

                    return TypedResults.Ok(result);
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.Problem(new ProblemDetails
                    {
                        Title = "Data Retrieval Error",
                        Detail = ex.Message,
                        Status = StatusCodes.Status500InternalServerError
                    });
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = $"capacity not found.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    else if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    return TypedResults.Problem(new ProblemDetails
                    {
                        Title = "Unexpected Error",
                        Detail = "An unexpected error occurred while retrieving capacities.",
                        Status = StatusCodes.Status500InternalServerError
                    });
                }
            })
                .WithName("GetAllCapacities")
                .WithTags("ClassifiedsDropdown")
                .WithSummary("Get all capacity values")
                .WithDescription("Fetches all available capacities from Dapr store.")
                .Produces<List<CategoriesDto>>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // adding processor
            group.MapPost("/processor", async Task<IResult> (
                [FromBody] AdProcessorRequest dto,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(dto.Name))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "Processor name is required.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    if (dto.ModelId == Guid.Empty)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "Model ID must be a valid non-empty GUID.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.AddProcessor(dto.Name, dto.ModelId, token);
                    return TypedResults.Created($"/api/processor/{result.Id}", result);
                }
                catch (ArgumentException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Argument",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.Problem(new ProblemDetails
                    {
                        Title = "Processor Creation Failed",
                        Detail = ex.Message,
                        Status = StatusCodes.Status500InternalServerError
                    });
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = $"processor not found.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    else if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    return TypedResults.Problem(new ProblemDetails
                    {
                        Title = "Internal Server Error",
                        Detail = "An unexpected error occurred while adding the processor.",
                        Status = StatusCodes.Status500InternalServerError
                    });
                }
            })
                .WithName("AddProcessor")
                .WithTags("ClassifiedsDropdown")
                .WithSummary("Add a processor")
                .WithDescription("Adds a new processor (e.g., Snapdragon 8, A16 Bionic) linked to a model.")
                .Produces<Category>(StatusCodes.Status201Created)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            //get processorby id 
            group.MapGet("/processor/by-model/{modelId:guid}", async Task<IResult> (
                Guid modelId,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    if (modelId == Guid.Empty)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "Model ID cannot be empty.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.GetModelWithProcessors(modelId, token);

                    if (result is null)
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Model Not Found",
                            Detail = $"No model found with ID {modelId}.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }

                    return TypedResults.Ok(result);
                }
                catch (ArgumentException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Argument",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (KeyNotFoundException ex)
                {
                    return TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Not Found",
                        Detail = ex.Message,
                        Status = StatusCodes.Status404NotFound
                    });
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.Problem(new ProblemDetails
                    {
                        Title = "Processor Retrieval Failed",
                        Detail = ex.Message,
                        Status = StatusCodes.Status500InternalServerError
                    });
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = $"Not found.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    else if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    return TypedResults.Problem(new ProblemDetails
                    {
                        Title = "Internal Server Error",
                        Detail = "An unexpected error occurred while fetching processors for the model.",
                        Status = StatusCodes.Status500InternalServerError
                    });
                }
            })
                .WithName("GetProcessorsByModelId")
                .WithTags("ClassifiedsDropdown")
                .WithSummary("Get processors by model")
                .WithDescription("Fetches processors like A16, A15 etc. based on selected mobile model.")
                .Produces<ModelWithProcessorsDto>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // get all processor 
            group.MapGet("/processor", async Task<IResult> (
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    var result = await service.GetAllProcessors(token);
                    if (result == null || !result.Any())
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "No Processors Found",
                            Detail = "No processor records were found in the store.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    return TypedResults.Ok(result);
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.Problem(new ProblemDetails
                    {
                        Title = "Processor Retrieval Failed",
                        Detail = ex.Message,
                        Status = StatusCodes.Status500InternalServerError
                    });
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = $"Processor not found.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    else if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    return TypedResults.Problem(new ProblemDetails
                    {
                        Title = "Internal Server Error",
                        Detail = "An unexpected error occurred while retrieving processors.",
                        Status = StatusCodes.Status500InternalServerError
                    });
                }
            })
                .WithName("GetAllProcessors")
                .WithTags("ClassifiedsDropdown")
                .WithSummary("Get all processors")
                .WithDescription("Returns all processors from the store.")
                .Produces<List<CategoriesDto>>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // adding coverage
            group.MapPost("/coverage", async Task<IResult> (
                [FromBody] AdCoverageRequest dto,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Invalid Input",
                            Detail = "Coverage name cannot be null or empty.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }
                    var result = await service.AddCoverage(dto.Name);
                    if (result == null)
                    {
                        return TypedResults.Problem(new ProblemDetails
                        {
                            Title = "Add Coverage Failed",
                            Detail = "Coverage could not be created.",
                            Status = StatusCodes.Status500InternalServerError
                        });
                    }
                    return TypedResults.Created($"/api/coverage/{result.Id}", result);
                }
                catch (ArgumentException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.Problem(new ProblemDetails
                    {
                        Title = "Coverage Creation Error",
                        Detail = ex.Message,
                        Status = StatusCodes.Status409Conflict
                    });
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = $"coverage not found.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    else if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    return TypedResults.Problem(new ProblemDetails
                    {
                        Title = "Unexpected Error",
                        Detail = "An internal error occurred while adding coverage.",
                        Status = StatusCodes.Status500InternalServerError
                    });
                }
            })
                .WithName("AddCoverage")
                .WithTags("ClassifiedsDropdown")
                .WithSummary("Add a coverage option")
                .WithDescription("Adds a coverage label such as 'Under Warranty' ")
                .Produces<Category>(StatusCodes.Status201Created)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // Get coverage list

            group.MapGet("/coverage", async Task<IResult> (
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    var result = await service.GetAllCoverages(token);
                    if (result == null || !result.Any())
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "No Coverage Found",
                            Detail = "No coverage records exist in the store.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    return TypedResults.Ok(result);
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.Problem(new ProblemDetails
                    {
                        Title = "Data Access Error",
                        Detail = ex.Message,
                        Status = StatusCodes.Status500InternalServerError
                    });
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = $"coverage not found.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    else if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    return TypedResults.Problem(new ProblemDetails
                    {
                        Title = "Unexpected Error",
                        Detail = "An error occurred while fetching coverage data.",
                        Status = StatusCodes.Status500InternalServerError
                    });
                }
            })
                .WithName("GetAllCoverages")
                .WithTags("ClassifiedsDropdown")
                .WithSummary("Get all coverage options")
                .WithDescription("Fetches all warranty/coverage types from the store.")
                .Produces<List<CategoriesDto>>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // adding ram
            group.MapPost("/ram", async Task<IResult> (
                [FromBody] AdRamRequest dto,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    if (dto == null || string.IsNullOrWhiteSpace(dto.Name) || dto.ModelId == Guid.Empty)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Invalid RAM Input",
                            Detail = "RAM name and valid Model ID are required.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.AddRam(dto.Name, dto.ModelId);

                    if (result == null)
                    {
                        return TypedResults.Problem(new ProblemDetails
                        {
                            Title = "RAM Creation Failed",
                            Detail = "Failed to add the new RAM entry.",
                            Status = StatusCodes.Status500InternalServerError
                        });
                    }

                    return TypedResults.Created($"/api/ram/{result.Id}", result);
                }
                catch (ArgumentException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Argument",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.Problem(new ProblemDetails
                    {
                        Title = "Operation Error",
                        Detail = ex.Message,
                        Status = StatusCodes.Status500InternalServerError
                    });
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = $"Ram not found.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    else if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    return TypedResults.Problem(new ProblemDetails
                    {
                        Title = "Internal Server Error",
                        Detail = "An unexpected error occurred while adding RAM.",
                        Status = StatusCodes.Status500InternalServerError
                    });
                }
            })
                .WithName("AddRam")
                .WithTags("ClassifiedsDropdown")
                .WithSummary("Add RAM")
                .WithDescription("Adds a RAM option.")
                .Produces<Category>(StatusCodes.Status201Created)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


            // Get all RAMs
            group.MapGet("/ram", async Task<IResult> (
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    var result = await service.GetAllRams(token);

                    if (result == null || !result.Any())
                    {
                        return TypedResults.Problem(new ProblemDetails
                        {
                            Title = "No RAM Records Found",
                            Detail = "The RAM store is currently empty.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }

                    return TypedResults.Ok(result);
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.Problem(new ProblemDetails
                    {
                        Title = "RAM Fetch Error",
                        Detail = ex.Message,
                        Status = StatusCodes.Status500InternalServerError
                    });
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = $"Ram not found.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    else if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    return TypedResults.Problem(new ProblemDetails
                    {
                        Title = "Unexpected Error",
                        Detail = $"An unexpected error occurred while retrieving RAM records: {ex.Message}",
                        Status = StatusCodes.Status500InternalServerError
                    });
                }
            })
            .WithName("GetAllRams")
            .WithTags("ClassifiedsDropdown")
            .WithSummary("Get all RAM values")
            .WithDescription("Fetches all RAM sizes from the store.")
            .Produces<List<CategoriesDto>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // Get RAMs by model
            group.MapGet("/ram/by-model/{modelId:guid}", async Task<IResult> (
                Guid modelId,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    var result = await service.GetModelWithRam(modelId, token);

                    if (result == null)
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Model Not Found",
                            Detail = $"No RAM options found because model with ID '{modelId}' does not exist.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }

                    return TypedResults.Ok(result);
                }
                catch (ArgumentException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Model ID",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (KeyNotFoundException ex)
                {
                    return TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Model Not Found",
                        Detail = ex.Message,
                        Status = StatusCodes.Status404NotFound
                    });
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = $"Not found.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    else if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: $"An error occurred while retrieving RAM options: {ex.Message}",
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("GetRamsByModelId")
            .WithTags("ClassifiedsDropdown")
            .WithSummary("Get RAM by model")
            .WithDescription("Returns RAM values for a specific model.")
            .Produces<List<CategoriesDto>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // Add Resolution
            group.MapPost("/resolution", async Task<IResult> (
                [FromBody] AdResolutionRequest dto,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(dto.Name))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Invalid Resolution Name",
                            Detail = "Resolution name cannot be null or empty.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    if (dto.ModelId == Guid.Empty)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Invalid Model ID",
                            Detail = "Model ID must be a valid non-empty GUID.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.AddResolution(dto.Name, dto.ModelId, token);
                    return TypedResults.Created($"/api/resolution/{result.Id}", result);
                }
                catch (ArgumentException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = $"resolution not found.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    else if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    return TypedResults.Problem(
                        title: "Add Resolution Error",
                        detail: $"An unexpected error occurred: {ex.Message}",
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("AddResolution")
            .WithTags("ClassifiedsDropdown")
            .WithSummary("Add resolution")
            .WithDescription("Adds a resolution like '1080x2400'.")
            .Produces<Category>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // Get all Resolutions
            group.MapGet("/resolution", async Task<IResult> (
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    var result = await service.GetAllResolutions(token);
                    if (result == null || !result.Any())
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "No Resolutions Found",
                            Detail = "No resolution records are available in the store.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    return TypedResults.Ok(result);
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = $"resolution not found.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    else if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    return TypedResults.Problem(
                        title: "Get Resolution Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("GetAllResolutions")
            .WithTags("ClassifiedsDropdown")
            .WithSummary("Get all resolutions")
            .WithDescription("Returns all screen resolutions from store.")
            .Produces<List<CategoriesDto>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // Get Resolutions by model
            group.MapGet("/resolution/by-model/{modelId:guid}", async Task<IResult> (
                Guid modelId,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    var result = await service.GetModelWithResolutions(modelId, token);
                    if (result == null)
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Model Not Found",
                            Detail = $"No model found with ID: {modelId}",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    return TypedResults.Ok(result);
                }
                catch (ArgumentException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Request",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (KeyNotFoundException ex)
                {
                    return TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Model Not Found",
                        Detail = ex.Message,
                        Status = StatusCodes.Status404NotFound
                    });
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = $"Not found.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    else if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("GetResolutionsByModelId")
            .WithTags("ClassifiedsDropdown")
            .WithSummary("Get resolutions by model")
            .WithDescription("Returns resolutions for a selected model.")
            .Produces<List<ModelWithResolutionsDto>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


            // POST /size-type
            group.MapPost("/size-type", async Task<IResult> (
                [FromBody] AdSizeRequest dto,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(dto?.Name))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "Size type name cannot be null or empty.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }
                    var result = await service.AddSizeType(dto.Name, token);
                    return TypedResults.Created($"/api/size-type/{result.Id}", result);
                }
                catch (ArgumentException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Argument",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.Problem(new ProblemDetails
                    {
                        Title = "Operation Failed",
                        Detail = ex.Message,
                        Status = StatusCodes.Status500InternalServerError
                    });
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = $"SizeType not found.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    else if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    return TypedResults.Problem(new ProblemDetails
                    {
                        Title = "Unexpected Error",
                        Detail = ex.Message,
                        Status = StatusCodes.Status500InternalServerError
                    });
                }
            })
            .WithName("AddSizeType")
            .WithTags("ClassifiedsDropdown")
            .WithSummary("Add a size type")
            .WithDescription("Adds size labels like 'Small', 'Medium', 'XL'.")
            .Produces<Category>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


            // GET /size-type
            group.MapGet("/size-type", async Task<IResult> (
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    var result = await service.GetAllSizeTypes(token);

                    if (result == null || !result.Any())
                    {
                        return TypedResults.Problem(new ProblemDetails
                        {
                            Title = "No Size Types Found",
                            Detail = "No size types are available in the system.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }

                    return TypedResults.Ok(result);
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.Problem(new ProblemDetails
                    {
                        Title = "Operation Failed",
                        Detail = ex.Message,
                        Status = StatusCodes.Status500InternalServerError
                    });
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = $"SizeType not found.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    else if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    return TypedResults.Problem(new ProblemDetails
                    {
                        Title = "Unexpected Error",
                        Detail = ex.Message,
                        Status = StatusCodes.Status500InternalServerError
                    });
                }
            })
            .WithName("GetAllSizeTypes")
            .WithTags("ClassifiedsDropdown")
            .WithSummary("Get all size types")
            .WithDescription("Fetches size options from Dapr store.")
            .Produces<List<CategoriesDto>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // POST /zone
            group.MapPost("/zone", async Task<IResult> (
                [FromBody] AdZoneRequest dto,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "Zone name must be provided.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.AddZone(dto.Name, token);
                    if (result == null)
                    {
                        return TypedResults.Problem(new ProblemDetails
                        {
                            Title = "Zone Creation Failed",
                            Detail = "Zone could not be added due to an internal error.",
                            Status = StatusCodes.Status500InternalServerError
                        });
                    }
                    return TypedResults.Created($"/api/zone/{result.Id}", result);
                }
                catch (ArgumentException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Argument",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.Problem(new ProblemDetails
                    {
                        Title = "Operation Error",
                        Detail = ex.Message,
                        Status = StatusCodes.Status500InternalServerError
                    });
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = $"Zone not found.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    else if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    return TypedResults.Problem(new ProblemDetails
                    {
                        Title = "Unexpected Error",
                        Detail = "An unexpected error occurred while adding the zone.",
                        Status = StatusCodes.Status500InternalServerError
                    });
                }
            })
            .WithName("AddZone")
            .WithTags("ClassifiedsDropdown")
            .WithSummary("Add a zone number")
            .WithDescription("Adds a zone like 51, 52, etc.")
            .Produces<Category>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // GET /zone
            group.MapGet("/zone", async Task<IResult> (
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    var result = await service.GetAllZones(token);

                    if (result == null || !result.Any())
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "No Zones Found",
                            Detail = "No zones are currently available in the system.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }

                    return TypedResults.Ok(result);
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.Problem(new ProblemDetails
                    {
                        Title = "Operational Error",
                        Detail = ex.Message,
                        Status = StatusCodes.Status500InternalServerError
                    });
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = $"Zone not found.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    else if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    return TypedResults.Problem(new ProblemDetails
                    {
                        Title = "Unexpected Error",
                        Detail = "An unexpected error occurred while fetching zones.",
                        Status = StatusCodes.Status500InternalServerError
                    });
                }
            })
            .WithName("GetAllZones")
            .WithTags("ClassifiedsDropdown")
            .WithSummary("Get all zones")
            .WithDescription("Fetches all zone numbers from the store.")
            .Produces<List<CategoriesDto>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // Get /categoryById hierachy 
            group.MapGet("/category-hierarchy/{categoryId}", async Task<IResult> (
                Guid categoryId,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    if (categoryId == Guid.Empty)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "Category ID must be a valid non-empty GUID.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.GetCategoryHierarchy(categoryId, token);

                    if (result == null)
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "No Hierarchy Found",
                            Detail = $"No category hierarchy data found for ID: {categoryId}",
                            Status = StatusCodes.Status404NotFound
                        });
                    }

                    return TypedResults.Ok(result);
                }
                catch (KeyNotFoundException ex)
                {
                    return TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Not Found",
                        Detail = ex.Message,
                        Status = StatusCodes.Status404NotFound
                    });
                }
                catch (ArgumentException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Argument",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.Problem(new ProblemDetails
                    {
                        Title = "Internal Operation Error",
                        Detail = ex.Message,
                        Status = StatusCodes.Status500InternalServerError
                    });
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = "The specified category hierarchy could not be found.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    else if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status400BadRequest
                        });
                    }
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: "An unexpected error occurred while loading category hierarchy.",
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
                .WithName("GetCategoryHierarchy")
                .WithTags("ClassifiedsDropdown")
                .WithSummary("Get a category with all its sub-related data")
                .WithDescription("Fetches a category along with all .")
                .Produces<NestedCategoryDto>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // delete / categoryById hierachy
            group.MapDelete("/category-hierarchy/{categoryId:guid}", async Task<IResult> (
                Guid categoryId,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    if (categoryId == Guid.Empty)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "Category ID must be a valid non-empty GUID.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var deleted = await service.DeleteCategoryHierarchy(categoryId, token);

                    if (!deleted)
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Deletion Failed",
                            Detail = $"No category or related hierarchy found to delete for ID: {categoryId}",
                            Status = StatusCodes.Status404NotFound
                        });
                    }

                    return TypedResults.Ok(new
                    {
                        Message = "Category and all nested data deleted successfully.",
                        Deleted = deleted
                    });
                }
                catch (KeyNotFoundException ex)
                {
                    return TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Not Found",
                        Detail = ex.Message,
                        Status = StatusCodes.Status404NotFound
                    });
                }
                catch (ArgumentException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Argument",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = "Category or its children were not found during deletion.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    else if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status400BadRequest
                        });
                    }
                    return TypedResults.Problem(new ProblemDetails
                    {
                        Title = "Internal Server Error",
                        Detail = "An unexpected error occurred while deleting category hierarchy.",
                        Status = StatusCodes.Status500InternalServerError
                    });
                }
            })
                .WithName("DeleteCategoryHierarchy")
                .WithTags("ClassifiedsDropdown")
                .WithSummary("Delete category hierarchy")
                .WithDescription("Deletes a category and all its nested subcategories, brands, models, RAMs, processors, and resolutions.")
                .Produces(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


            // added save search
            group.MapPost("/search/saveSearch", async Task<Results<
                Ok<string>,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
            (
                SaveSearchRequestDto dto,
                IClassifiedService service,
                HttpContext context
            ) =>
            {
                Guid userId = context.User.GetId();
                if (userId == null || userId == Guid.Empty)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Valid User ID must be provided.",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = context.Request.Path
                    });
                }

                if (string.IsNullOrWhiteSpace(dto.Name))
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Search name is required.",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = context.Request.Path
                    });
                }

                if (dto.SearchQuery == null || string.IsNullOrWhiteSpace(dto.SearchQuery.Text))
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Search query text is required.",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = context.Request.Path
                    });
                }

                try
                {
                    var success = await service.SaveSearch(dto, userId);
                    if (success)
                    {
                        return TypedResults.Ok("Search saved successfully.");
                    }

                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Save Failed",
                        Detail = "Search save could not be confirmed.",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = context.Request.Path
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
           .WithName("SaveSearch")
           .WithTags("Search")
           .WithSummary("Save user search")
           .WithDescription("Save the search criteria using user ID from frontend.")
           .RequireAuthorization()
           .Produces<string>(StatusCodes.Status200OK)
           .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
           .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // added save search id
            group.MapPost("/search/by-category-id", async Task<Results<
                Ok<string>,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
            (
                SaveSearchRequestByIdDto dto,
                IClassifiedService service,
                HttpContext context
            ) =>
            {
                if (dto.UserId == null || dto.UserId == Guid.Empty)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Valid User ID must be provided.",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = context.Request.Path
                    });
                }

                if (string.IsNullOrWhiteSpace(dto.Name))
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Search name is required.",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = context.Request.Path
                    });
                }

                if (dto.SearchQuery == null || string.IsNullOrWhiteSpace(dto.SearchQuery.Text))
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Search query text is required.",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = context.Request.Path
                    });
                }

                try
                {
                    var success = await service.SaveSearchById(dto);
                    if (success)
                    {
                        return TypedResults.Ok("Search saved successfully.");
                    }

                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Save Failed",
                        Detail = "Search save could not be confirmed.",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = context.Request.Path
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
           .WithName("SaveSearchById")
           .WithTags("Search")
           .WithSummary("Save user searcssh")
           .WithDescription("Save the search criteria using user ID from frontendss.")
           .ExcludeFromDescription()
           .Produces<string>(StatusCodes.Status200OK)
           .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
           .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // get save search
            group.MapGet("/search/getsavedSearches", async Task<Results<
                Ok<List<SavedSearchResponseDto>>,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
            (
                IClassifiedService service,
                HttpContext context
            ) =>
            {
                Guid? userId = context.User.GetId();
                if (userId == null || userId == Guid.Empty)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Valid User ID must be provided in the query.",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = context.Request.Path
                    });
                }
                else
                {
                    try
                    {
                        var result = await service.GetSearches(userId.ToString());
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
                }
            })
            .WithName("GetSavedSearch")
            .WithTags("Search")
            .WithSummary("Get saved searches")
            .WithDescription("Get all saved searches for the current user.")
            .RequireAuthorization()
            .Produces<List<SavedSearchResponseDto>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapGet("/search/save-by-id", async Task<Results<
                Ok<List<SavedSearchResponseDto>>,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
            (
                [Required][FromQuery] Guid userId,
                IClassifiedService service,
                HttpContext context
            ) =>
            {
                if (userId == Guid.Empty)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Valid User ID must be provided in the query.",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = context.Request.Path
                    });
                }
                else
                {
                    try
                    {
                        var result = await service.GetSearches(userId.ToString());
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
                }
            })
            .WithName("GetSavedSearcheById")
            .WithTags("Searchs")
            .WithSummary("Get saved searchess")
            .WithDescription("Get all saved searches for the current users.")
            .ExcludeFromDescription()
            .Produces<List<SavedSearchResponseDto>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapGet("itemsAd-dashboard", async Task<IResult> (
                HttpContext context,
                IClassifiedService service,
                CancellationToken token) =>
            {
                var userId = context.User.GetId(); 

                if (userId == null || userId == Guid.Empty)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Valid User ID must be provided in the JWT token.",
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                try
                {
                    var result = await service.GetUserItemsAdsWithDashboard(userId, token);

                    if ((result?.ItemsAds.PublishedAds?.Any() != true) &&
                        (result?.ItemsAds.UnpublishedAds?.Any() != true))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "No Ads Found",
                            Detail = $"No ads were found for user ID '{userId}'.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }

                    return TypedResults.Ok(result);
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Operation",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = "Requested user or ads data not found.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    else if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status400BadRequest
                        });
                    }
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
                .WithName("GetUserItemsAdsWithDashboard")
                .WithTags("Items")
                .WithSummary("Get all user ads and dashboard")
                .WithDescription("Returns both published/unpublished ads and dashboard metrics for a given user ID (from token).")
                .RequireAuthorization() 
                .Produces<ItemAdsAndDashboardResponse>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .RequireAuthorization(new AuthorizeAttribute { Roles = "Subscriber" })
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapGet("itemsAd-dashboard-byId", async Task<IResult> (
                [FromQuery] Guid userId,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    if (userId == Guid.Empty)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "User ID must not be empty.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.GetUserItemsAdsWithDashboard(userId, token);

                    if ((result?.ItemsAds.PublishedAds?.Any() != true) &&
                        (result?.ItemsAds.UnpublishedAds?.Any() != true))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "No Ads Found",
                            Detail = $"No ads were found for user ID '{userId}'.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }

                    return TypedResults.Ok(result);
                }
                catch (InvalidOperationException ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = "Requested user or ads data not found.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    else if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status400BadRequest
                        });
                    }
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Operation",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
                .WithName("GetUserItemsAdsWithDashboardById") 
                .WithTags("Items")
                .WithSummary("Get all user ads and dashboard (By Id)")
                .WithDescription("Returns both published/unpublished ads and dashboard metrics for a given user ID (from query).")
                .Produces<ItemAdsAndDashboardResponse>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .ExcludeFromDescription();



            group.MapGet("prelovedAd-dashboard", async Task<IResult> (
                HttpContext httpContext,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    var userId = httpContext.User.GetId(); 
                    if (userId == Guid.Empty)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "Authenticated user ID is missing or invalid.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.GetUserPrelovedAdsAndDashboard(userId, token);

                    if ((result?.PrelovedAds.PublishedAds?.Any() != true) &&
                        (result?.PrelovedAds.UnpublishedAds?.Any() != true))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "No Ads Found",
                            Detail = $"No Preloved ads were found for authenticated user.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }

                    return TypedResults.Ok(result);
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Operation",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = "Requested Preloved ads or user data not found.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    else if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status400BadRequest
                        });
                    }
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("GetUserPrelovedAdsWithDashboard") 
            .WithTags("Preloved")
            .WithSummary("Get all authenticated user's Preloved ads and dashboard")
            .WithDescription("Returns Preloved ads and dashboard for the currently authenticated user.")
            .Produces<PrelovedAdsAndDashboardResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Subscriber" })
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


            group.MapGet("prelovedAd-dashboard-byId", async Task<IResult> (
                Guid userId,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    if (userId == Guid.Empty)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "User ID must not be empty.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.GetUserPrelovedAdsAndDashboard(userId, token);

                    if ((result?.PrelovedAds.PublishedAds?.Any() != true) &&
                        (result?.PrelovedAds.UnpublishedAds?.Any() != true))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "No Ads Found",
                            Detail = $"No Preloved ads were found for user ID '{userId}'.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }

                    return TypedResults.Ok(result);
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Operation",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404") || (ex.InnerException?.Message.Contains("404") ?? false))
                    {
                        return TypedResults.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = "Requested Preloved ads or user data not found.",
                            Status = StatusCodes.Status404NotFound
                        });
                    }
                    else if (ex.Message.Contains("400") || (ex.InnerException?.Message.Contains("400") ?? false))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = ex.Message,
                            Status = StatusCodes.Status400BadRequest
                        });
                    }
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName("GetUserPrelovedAdsWithDashboardById") 
            .WithTags("Preloved")
            .WithSummary("Get all user Preloved ads and dashboard (by userId)")
            .WithDescription("Returns Preloved ads and dashboard for a user specified via route parameter.")
            .Produces<PrelovedAdsAndDashboardResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
            .ExcludeFromDescription();

            // itemsAd post
            group.MapPost("items/post", async Task<IResult> (
                HttpContext httpContext,
                ClassifiedItems dto,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    var userId = httpContext.User.GetId();
                    if (userId == Guid.Empty)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "Authenticated user ID is missing or invalid.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    dto.UserId = userId;
                    var response = await service.CreateClassifiedItemsAd(dto, token);
                    return TypedResults.Ok(response);
                }
                catch (ArgumentException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.Conflict(new ProblemDetails
                    {
                        Title = "Ad Creation Failed",
                        Detail = ex.Message,
                        Status = StatusCodes.Status409Conflict
                    });
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
                .WithName("PostItemsAd")
                .WithTags("Classified")
                .WithSummary("Post classified items ad using authenticated user")
                .WithDescription("Takes user ID from JWT token and creates the ad.")
                .Produces<AdCreatedResponseDto>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .RequireAuthorization();

            group.MapPost("items/post-by-id", async Task<IResult> (
                ClassifiedItems dto,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    if (dto.UserId == Guid.Empty)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "User ID must not be empty.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var response = await service.CreateClassifiedItemsAd(dto, token);
                    return TypedResults.Ok(response);
                }
                catch (ArgumentException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.Conflict(new ProblemDetails
                    {
                        Title = "Ad Creation Failed",
                        Detail = ex.Message,
                        Status = StatusCodes.Status409Conflict
                    });
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
                .WithName("PostItemsAdById")
                .WithTags("Classified")
                .WithSummary("Post classified items ad using provided UserId")
                .WithDescription("For admin/service scenarios where the UserId is passed explicitly.")
                .Produces<AdCreatedResponseDto>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .ExcludeFromDescription();
            
            
            group.MapPost("preloved/post", async Task<IResult> (
                HttpContext httpContext,
                ClassifiedPreloved dto,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    var userId = httpContext.User.GetId();
                    if (userId == Guid.Empty)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "Authenticated user ID is missing or invalid.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    dto.UserId = userId;
                    var result = await service.CreateClassifiedPrelovedAd(dto, token);                    
                    return TypedResults.Ok(result);
                }
                catch (ArgumentException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.Conflict(new ProblemDetails
                    {
                        Title = "Ad Creation Failed",
                        Detail = ex.Message,
                        Status = StatusCodes.Status409Conflict
                    });
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
                .WithName("PostPrelovedAd")
                .WithTags("Classified")
                .WithSummary("Post classified preloved ad using authenticated user")
                .WithDescription("Takes user ID from JWT token and creates the ad.")
                .Produces<AdCreatedResponseDto>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .RequireAuthorization();

            group.MapPost("preloved/post-by-id", async Task<IResult> (
                ClassifiedPreloved dto,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    if (dto.UserId == Guid.Empty)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "User ID must not be empty.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.CreateClassifiedPrelovedAd(dto, token);                    
                    return TypedResults.Ok(result);
                }
                catch (ArgumentException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.Conflict(new ProblemDetails
                    {
                        Title = "Ad Creation Failed",
                        Detail = ex.Message,
                        Status = StatusCodes.Status409Conflict
                    });
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
                .WithName("PostPrelovedAdById")
                .WithTags("Classified")
                .WithSummary("Post classified preloved ad using provided UserId")
                .WithDescription("For admin/service scenarios where the UserId is passed explicitly.")
                .Produces<AdCreatedResponseDto>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .ExcludeFromDescription();

            group.MapPost("deals/post", async Task<IResult> (
                HttpContext httpContext,
                ClassifiedDeals dto,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    var userId = httpContext.User.GetId();
                    if (userId == Guid.Empty)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "Authenticated user ID is missing or invalid.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    dto.UserId = userId;
                    var result = await service.CreateClassifiedDealsAd(dto, token);
                    
                    return TypedResults.Ok(result);
                }
                catch (ArgumentException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.Conflict(new ProblemDetails
                    {
                        Title = "Ad Creation Failed",
                        Detail = ex.Message,
                        Status = StatusCodes.Status409Conflict
                    });
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
                .WithName("PostDealsAd")
                .WithTags("Classified")
                .WithSummary("Post classified Deals ad using authenticated user")
                .WithDescription("Takes user ID from JWT token and creates the ad.")
                .Produces<AdCreatedResponseDto>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .RequireAuthorization();

            group.MapPost("deals/post-by-id", async Task<IResult> (
                ClassifiedDeals dto,
                IClassifiedService service,
                CancellationToken token) =>
            {
                try
                {
                    if (dto.UserId == Guid.Empty)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Title = "Validation Error",
                            Detail = "User ID must not be empty.",
                            Status = StatusCodes.Status400BadRequest
                        });
                    }

                    var result = await service.CreateClassifiedDealsAd(dto, token);
                    
                    return TypedResults.Ok(result);
                }
                catch (ArgumentException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (InvalidOperationException ex)
                {
                    return TypedResults.Conflict(new ProblemDetails
                    {
                        Title = "Ad Creation Failed",
                        Detail = ex.Message,
                        Status = StatusCodes.Status409Conflict
                    });
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
                .WithName("PostDealsAdById")
                .WithTags("Classified")
                .WithSummary("Post classified deals ad using provided UserId")
                .WithDescription("For admin/service scenarios where the UserId is passed explicitly.")
                .Produces<AdCreatedResponseDto>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
                .ExcludeFromDescription();


            group.MapGet("/collectibles", async Task<Results<
               Ok<CollectiblesResponse>,
               BadRequest<ProblemDetails>,
               UnauthorizedHttpResult,
               ProblemHttpResult>>
            (
                IClassifiedService service,
                HttpContext context,
                CancellationToken cancellationToken
            ) =>
            {
                Guid? userId = context.User.GetId();

                if (userId == null || userId == Guid.Empty)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Valid User ID must be provided in the token.",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = context.Request.Path
                    });
                }

                try
                {
                    var result = await service.GetCollectibles(userId.ToString(), cancellationToken);
                    return TypedResults.Ok(result);
                }
                catch (FileNotFoundException fileEx)
                {
                    return TypedResults.Problem(
                        title: "File Not Found",
                        detail: fileEx.Message,
                        statusCode: StatusCodes.Status404NotFound,
                        instance: context.Request.Path);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: context.Request.Path);
                }
            })
            .WithName("GetCollectibles")
            .WithTags("Collectibles")
            .WithSummary("Get collectibles for the logged-in user")
            .WithDescription("Returns collectibles data for the current user based on token.")
            .Produces<CollectiblesResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
            .Produces(StatusCodes.Status401Unauthorized)
            .RequireAuthorization();

            group.MapGet("/collectibles-by-id", async Task<Results<
                  Ok<CollectiblesResponse>,
                  BadRequest<ProblemDetails>,
                  ProblemHttpResult>>
              (
                  [Required][FromQuery] Guid userId,
                  IClassifiedService service,
                  HttpContext context,
                  CancellationToken cancellationToken
              ) =>
            {
                if (userId == Guid.Empty)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Valid User ID must be provided in the query.",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = context.Request.Path
                    });
                }

                try
                {
                    var result = await service.GetCollectibles(userId.ToString(), cancellationToken);
                    return TypedResults.Ok(result);
                }
                catch (FileNotFoundException fileEx)
                {
                    return TypedResults.Problem(
                        title: "File Not Found",
                        detail: fileEx.Message,
                        statusCode: StatusCodes.Status404NotFound,
                        instance: context.Request.Path);
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: context.Request.Path);
                }
            })
            .WithName("GetCollectiblesById")
            .WithTags("Collectibles")
            .WithSummary("Get collectibles for a specified user ID")
            .WithDescription("Returns collectibles data for a given user ID passed as query parameter.")
            .Produces<CollectiblesResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .ExcludeFromDescription()
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapDelete("/items-ad/{adId:guid}", async Task<Results<
                Ok<DeleteAdResponseDto>,
                BadRequest<ProblemDetails>,
                NotFound<ProblemDetails>,
                ProblemHttpResult>>
                (
                Guid adId,
                IClassifiedService service,
                HttpContext context,
                CancellationToken cancellationToken
                ) =>
            {
                if (adId == Guid.Empty)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Ad ID must be a valid non-empty GUID.",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = context.Request.Path
                    });
                }

                try
                {
                    var response = await service.DeleteClassifiedItemsAd(adId, cancellationToken);
                    return TypedResults.Ok(response);
                }
                catch (KeyNotFoundException ex)
                {
                    return TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Ad Not Found",
                        Detail = ex.Message,
                        Status = StatusCodes.Status404NotFound,
                        Instance = context.Request.Path
                    });
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: context.Request.Path);
                }
            })
                .WithName("DeleteClassifiedItemsAd")
                .WithTags("Classified")
                .WithSummary("Delete a classified items ad by ID")
                .WithDescription("Deletes a classified items ad using the provided Ad ID. Ad must exist in Dapr state store.")
                .Produces<DeleteAdResponseDto>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapDelete("/preloved-ad/{adId:guid}", async Task<Results<
                Ok<DeleteAdResponseDto>,
                BadRequest<ProblemDetails>,
                NotFound<ProblemDetails>,
                ProblemHttpResult>>
                (
                Guid adId,
                IClassifiedService service,
                HttpContext context,
                CancellationToken cancellationToken
                ) =>
            {
                if (adId == Guid.Empty)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Ad ID must be a valid non-empty GUID.",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = context.Request.Path
                    });
                }

                try
                {
                    var response = await service.DeleteClassifiedPrelovedAd(adId, cancellationToken);
                    return TypedResults.Ok(response);
                }
                catch (KeyNotFoundException ex)
                {
                    return TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Ad Not Found",
                        Detail = ex.Message,
                        Status = StatusCodes.Status404NotFound,
                        Instance = context.Request.Path
                    });
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: context.Request.Path);
                }
            })
                .WithName("DeleteClassifiedPrelovedAd")
                .WithTags("Classified")
                .WithSummary("Delete a classified preloved ad by ID")
                .WithDescription("Deletes a classified preloved ad using the provided Ad ID. Ad must exist in Dapr state store.")
                .Produces<DeleteAdResponseDto>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            group.MapDelete("/deals-ad/{adId:guid}", async Task<Results<
                Ok<DeleteAdResponseDto>,
                BadRequest<ProblemDetails>,
                NotFound<ProblemDetails>,
                ProblemHttpResult>>
                (
                Guid adId,
                IClassifiedService service,
                HttpContext context,
                CancellationToken cancellationToken
                ) =>
            {
                if (adId == Guid.Empty)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Validation Error",
                        Detail = "Ad ID must be a valid non-empty GUID.",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = context.Request.Path
                    });
                }

                try
                {
                    var response = await service.DeleteClassifiedDealsAd(adId, cancellationToken);
                    return TypedResults.Ok(response);
                }
                catch (KeyNotFoundException ex)
                {
                    return TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Ad Not Found",
                        Detail = ex.Message,
                        Status = StatusCodes.Status404NotFound,
                        Instance = context.Request.Path
                    });
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: context.Request.Path);
                }
            })
                .WithName("DeleteClassifiedDealsAd")
                .WithTags("Classified")
                .WithSummary("Delete a classified deals ad by ID")
                .WithDescription("Deletes a classified deals ad using the provided Ad ID. Ad must exist in Dapr state store.")
                .Produces<DeleteAdResponseDto>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);



            return group;
        }

        public static RouteGroupBuilder MapClassifiedsFeaturedItemEndpoint(this RouteGroupBuilder group)
        {

            group.MapGet("/featured-items", async Task<IResult> (
                    [FromServices] ISearchService searchSvc,
                    CancellationToken cancellationToken
                ) =>
            {
                var searchReq = new CommonSearchRequest
                {
                    Top = 100,
                    Filters = new Dictionary<string, object>
                   {
                        { "IsFeatured",   true },
                        { "SubVertical", "Item" }
                    }
                };

                try
                {
                    CommonSearchResponse response = await searchSvc.SearchAsync(
                        ConstantValues.Verticals.Classifieds,
                        searchReq
                    );

                    var list = response.ClassifiedsItems ?? new List<ClassifiedsIndex>();

                    return TypedResults.Ok(list);
                }
                catch (ArgumentException ex)
                {
                    return TypedResults.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Argument",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (Exception ex)
                {
                    return TypedResults.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            })
            .WithName($"GetFeatured_{ConstantValues.Verticals.Classifieds}_Items")
            .WithTags("Classified")
            .WithSummary("Get all featured classified items")
            .WithDescription("Fetches every ClassifiedsIndex document where IsFeatured = true.")
            .Produces<IEnumerable<ClassifiedsIndex>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


           

            return group;
        }

    }
}
