using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using QLN.Common.DTO_s;
using QLN.Common.DTO_s.Payments;
using QLN.Common.DTO_s.Subscription;
using QLN.Common.Infrastructure.CustomException;
using QLN.Common.Infrastructure.IService.IProductService;
using QLN.Common.Infrastructure.Subscriptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.CustomEndpoints.ProductEndpoints
{
    public static class ProductEndpoints
    {
        public static RouteGroupBuilder MapProductEndpoints(this RouteGroupBuilder group)
        {
            group.MapGetAllProducts();
            group.MapGetProductsByVertical();
            group.MapGetProductsByType();
            group.MapGetProductByCode();
            group.MapCreateProduct();
            group.MapUpdateProduct();
            group.MapDeleteProduct();

            group.MapCreateFreeAdsProduct();
            group.MapCreateFreeAdsProductFromCompleteJson();
            group.MapGetFreeAdsProducts();

            return group;
        }

        #region Existing Product Endpoints

        private static RouteGroupBuilder MapGetAllProducts(this RouteGroupBuilder group)
        {
            group.MapGet("/getall", async (
                [FromServices] IProductService productService,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var products = await productService.GetAllProductsAsync(cancellationToken);
                    return Results.Ok(products);
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "Error retrieving products",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
            .AllowAnonymous()
            .WithName("GetAllProducts")
            .WithTags("Products")
            .WithSummary("Get all active products")
            .WithDescription("Retrieves all active products across all verticals")
            .Produces<List<ProductResponseDto>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }

        private static RouteGroupBuilder MapGetProductsByVertical(this RouteGroupBuilder group)
        {
            group.MapGet("/getallproducts", async (
                [FromQuery] Vertical? vertical,
                [FromQuery] SubVertical? subvertical,
                [FromQuery] ProductType? productType,
                [FromServices] IProductService productService,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var products = await productService.GetProductsByVerticalAsync(vertical, subvertical, productType, cancellationToken);
                    return Results.Ok(products);
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
            .AllowAnonymous()
            .WithName("GetProductsByVertical")
            .WithTags("Products")
            .WithSummary("Get all products")
            .WithDescription("Retrieves all active products for a specific vertical")
            .Produces<List<ProductResponseDto>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }

        private static RouteGroupBuilder MapGetProductsByType(this RouteGroupBuilder group)
        {
            group.MapGet("/type/{productType:int}", async (
                ProductType productType,
                [FromServices] IProductService productService,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    if (!Enum.IsDefined(typeof(ProductType), productType))
                        return Results.BadRequest(new ProblemDetails
                        {
                            Title = "Invalid Data",
                            Detail = "Invalid product type",
                            Status = StatusCodes.Status400BadRequest
                        });

                    var products = await productService.GetProductsByTypeAsync((ProductType)productType, cancellationToken);
                    return Results.Ok(products);
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
            .AllowAnonymous()
            .WithName("GetProductsByType")
            .WithTags("Products")
            .WithSummary("Get products by type")
            .WithDescription("Retrieves all active products for a specific product type")
            .Produces<List<ProductResponseDto>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }

        private static RouteGroupBuilder MapGetProductByCode(this RouteGroupBuilder group)
        {
            group.MapGet("/{productCode}", async (
                string productCode,
                [FromServices] IProductService productService,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var product = await productService.GetProductByCodeAsync(productCode, cancellationToken);

                    if (product == null)
                        return Results.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = $"Product with code '{productCode}' not found",
                            Status = StatusCodes.Status404NotFound
                        });

                    return Results.Ok(product);
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
            .AllowAnonymous()
            .WithName("GetProductByCode")
            .WithTags("Products")
            .WithSummary("Get product by code")
            .WithDescription("Retrieves a specific product by its product code")
            .Produces<ProductResponseDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }

        private static RouteGroupBuilder MapCreateProduct(this RouteGroupBuilder group)
        {
            group.MapPost("/create", async (
                CreateProductDto request,
                [FromServices] IProductService productService,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var product = await productService.CreateProductAsync(request, cancellationToken);
                    return Results.Ok(product);
                }
                catch (ConflictException ex)
                {
                    return Results.Problem(
                        title: "Conflict",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status409Conflict);
                }
                catch (InvalidDataException ex)
                {
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Data",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
            //.RequireAuthorization(policy => policy.RequireRole("Admin"))
            .WithName("CreateProduct")
            .WithTags("Products")
            .WithSummary("Create a new product")
            .WithDescription("Creates a new product. Admin access required.")
            .Produces<ProductResponseDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }

        private static RouteGroupBuilder MapUpdateProduct(this RouteGroupBuilder group)
        {
            group.MapPut("/{productCode}", async (
                string productCode,
                UpdateProductDto request,
                [FromServices] IProductService productService,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var product = await productService.UpdateProductAsync(productCode, request, cancellationToken);
                    return Results.Ok(product);
                }
                catch (KeyNotFoundException ex)
                {
                    return Results.NotFound(new ProblemDetails
                    {
                        Title = "Not Found",
                        Detail = ex.Message,
                        Status = StatusCodes.Status404NotFound
                    });
                }
                catch (InvalidDataException ex)
                {
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Data",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
            //.RequireAuthorization(policy => policy.RequireRole("Admin"))
            .WithName("UpdateProduct")
            .WithTags("Products")
            .WithSummary("Update an existing product")
            .WithDescription("Updates an existing product. Admin access required.")
            .Produces<ProductResponseDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }

        private static RouteGroupBuilder MapDeleteProduct(this RouteGroupBuilder group)
        {
            group.MapDelete("/{productCode}", async (
                string productCode,
                [FromServices] IProductService productService,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var result = await productService.DeleteProductAsync(productCode, cancellationToken);

                    if (!result)
                        return Results.NotFound(new ProblemDetails
                        {
                            Title = "Not Found",
                            Detail = $"Product with code '{productCode}' not found",
                            Status = StatusCodes.Status404NotFound
                        });

                    return Results.Ok("Product deleted successfully");
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
            //.RequireAuthorization(policy => policy.RequireRole("Admin"))
            .WithName("DeleteProduct")
            .WithTags("Products")
            .WithSummary("Delete a product")
            .WithDescription("Soft deletes a product (sets IsActive = false). Admin access required.")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }

        #endregion

        #region NEW: FREE Ads Product Endpoints

        private static RouteGroupBuilder MapCreateFreeAdsProduct(this RouteGroupBuilder group)
        {
            group.MapPost("/create-free-ads", async (
                CreateFreeAdsProductDto request,
                [FromServices] IProductService productService,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var product = await productService.CreateFreeAdsProductAsync(request, cancellationToken);

                    return Results.Ok(new
                    {
                        Success = true,
                        Message = "FREE ads product created successfully",
                        Product = product,
                        CategoryQuotasCount = product.Constraints?.CategoryQuotas?.Count ?? 0,
                        TotalFreeAdsAllowed = product.Constraints?.CategoryQuotas?.Sum(c => c.AdsBudget) ?? 0,
                        CategoryBreakdown = product.Constraints?.CategoryQuotas?.GroupBy(c => c.Category)
                            .Select(g => new
                            {
                                Category = g.Key,
                                SubcategoriesCount = g.Count(),
                                TotalAdsInCategory = g.Sum(c => c.AdsBudget)
                            }).ToList()
                    });
                }
                catch (ConflictException ex)
                {
                    return Results.Problem(
                        title: "Conflict",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status409Conflict);
                }
                catch (InvalidOperationException ex)
                {
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Category Hierarchy",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
            //.RequireAuthorization(policy => policy.RequireRole("Admin"))
            .WithName("CreateFreeAdsProduct")
            .WithTags("Products", "FREE Ads")
            .WithSummary("Create a FREE ads product with category-based quotas")
            .WithDescription("Creates a FREE ads product from JSON category hierarchy. Admin access required.")
            .Produces<object>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }

        private static RouteGroupBuilder MapCreateFreeAdsProductFromCompleteJson(this RouteGroupBuilder group)
        {
            group.MapPost("/create-free-ads-from-complete-json", async (
                [FromBody] CreateFreeAdsFromCompleteJsonRequest request,
                [FromServices] IProductService productService,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    // You can either read from file or accept the JSON in the request body
                    string jsonContent;

                    if (!string.IsNullOrEmpty(request.CategoryHierarchyJson))
                    {
                        // Use provided JSON
                        jsonContent = request.CategoryHierarchyJson;
                    }
                    else
                    {
                        // Read from your JSON file (make sure the path is correct)
                        try
                        {
                            jsonContent = await System.IO.File.ReadAllTextAsync("classifiedsitems_hierarchy_no_null_l2.json");
                        }
                        catch (FileNotFoundException)
                        {
                            return Results.BadRequest(new ProblemDetails
                            {
                                Title = "JSON File Not Found",
                                Detail = "The classifiedsitems_hierarchy_no_null_l2.json file was not found. Please provide the JSON in the request body.",
                                Status = StatusCodes.Status400BadRequest
                            });
                        }
                    }

                    var createDto = new CreateFreeAdsProductDto
                    {
                        ProductCode = request.ProductCode ?? "FREE_CLASSIFIEDS_ALL_CATEGORIES",
                        ProductName = request.ProductName ?? "Free Classifieds - All Categories",
                        Vertical = request.Vertical ?? Vertical.Classifieds,
                        SubVertical = request.SubVertical,
                        Price = 0, // Always 0 for FREE
                        Currency = request.Currency ?? "QAR",
                        Duration = request.Duration, // Unlimited if null
                        CategoryHierarchyJson = jsonContent,
                        Remarks = request.Remarks ?? "Complete FREE ads product with all classifieds categories from JSON"
                    };

                    var product = await productService.CreateFreeAdsProductAsync(createDto, cancellationToken);

                    var categoryBreakdown = product.Constraints?.CategoryQuotas?.GroupBy(c => c.Category)
                        .Select(g => new
                        {
                            Category = g.Key,
                            SubcategoriesCount = g.Count(),
                            TotalAdsInCategory = g.Sum(c => c.AdsBudget),
                            Subcategories = g.Select(c => new
                            {
                                L1Category = c.L1Category,
                                L2Category = c.L2Category,
                                AdsBudget = c.AdsBudget,
                                CategoryPath = !string.IsNullOrEmpty(c.L2Category)
                                    ? $"{c.Category} > {c.L1Category} > {c.L2Category}"
                                    : !string.IsNullOrEmpty(c.L1Category)
                                        ? $"{c.Category} > {c.L1Category}"
                                        : c.Category
                            }).ToList()
                        }).ToList();

                    return Results.Ok(new
                    {
                        Success = true,
                        Message = "Complete FREE classifieds product created from JSON hierarchy",
                        Product = product,
                        Statistics = new
                        {
                            TotalCategories = product.Constraints?.CategoryQuotas?.Count ?? 0,
                            TotalFreeAdsAllowed = product.Constraints?.CategoryQuotas?.Sum(c => c.AdsBudget) ?? 0,
                            MainCategoriesCount = categoryBreakdown.Count,
                            AverageAdsPerCategory = categoryBreakdown.Count > 0
                                ? Math.Round(categoryBreakdown.Average(c => (double)c.TotalAdsInCategory), 2)
                                : 0
                        },
                        CategoryBreakdown = categoryBreakdown
                    });
                }
                catch (ConflictException ex)
                {
                    return Results.Problem(
                        title: "Conflict",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status409Conflict);
                }
                catch (InvalidOperationException ex)
                {
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid JSON Format",
                        Detail = ex.Message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "Internal Server Error",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
            //.RequireAuthorization(policy => policy.RequireRole("Admin"))
            .WithName("CreateFreeAdsProductFromCompleteJson")
            .WithTags("Products", "FREE Ads")
            .WithSummary("Create FREE ads product from complete JSON hierarchy")
            .WithDescription("Creates a complete FREE ads product using the full category hierarchy JSON. Admin access required.")
            .Produces<object>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }

        private static RouteGroupBuilder MapGetFreeAdsProducts(this RouteGroupBuilder group)
        {
            group.MapGet("/free-ads", async (
                [FromServices] IProductService productService,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var freeProducts = await productService.GetProductsByTypeAsync(ProductType.FREE, cancellationToken);

                    var enhancedProducts = freeProducts.Select(product => new
                    {
                        ProductCode = product.ProductCode,
                        ProductName = product.ProductName,
                        ProductType = product.ProductType,
                        Vertical = product.Vertical,
                        SubVertical = product.SubVertical,
                        Price = product.Price,
                        Currency = product.Currency,
                        IsActive = product.IsActive,
                        CreatedAt = product.CreatedAt,
                        UpdatedAt = product.UpdatedAt,
                        CategoryStatistics = new
                        {
                            TotalCategories = product.Constraints?.CategoryQuotas?.Count ?? 0,
                            TotalFreeAdsAllowed = product.Constraints?.CategoryQuotas?.Sum(c => c.AdsBudget) ?? 0,
                            MainCategories = product.Constraints?.CategoryQuotas != null
                                ? product.Constraints.CategoryQuotas.GroupBy(c => c.Category)
                                    .Select(g => new CategorySummaryDto
                                    {
                                        Category = g.Key,
                                        SubcategoriesCount = g.Count(),
                                        TotalAds = g.Sum(c => c.AdsBudget)
                                    }).ToList()
                                : new List<CategorySummaryDto>()
                        },
                        // Optionally include detailed category breakdown (comment out if too verbose)
                        CategoryQuotas = product.Constraints?.CategoryQuotas != null
                            ? product.Constraints.CategoryQuotas.Select(c => new CategoryQuotaDto
                            {
                                CategoryPath = !string.IsNullOrEmpty(c.L2Category)
                                    ? $"{c.Category} > {c.L1Category} > {c.L2Category}"
                                    : !string.IsNullOrEmpty(c.L1Category)
                                        ? $"{c.Category} > {c.L1Category}"
                                        : c.Category,
                                AdsBudget = c.AdsBudget
                            }).ToList()
                            : new List<CategoryQuotaDto>()
                    }).ToList();

                    return Results.Ok(new
                    {
                        Success = true,
                        Message = $"Found {freeProducts.Count} FREE ads products",
                        TotalProducts = freeProducts.Count,
                        Products = enhancedProducts,
                        Summary = new
                        {
                            TotalFreeAdsAcrossAllProducts = enhancedProducts.Sum(p => p.CategoryStatistics.TotalFreeAdsAllowed),
                            TotalCategoriesAcrossAllProducts = enhancedProducts.Sum(p => p.CategoryStatistics.TotalCategories),
                            ActiveProducts = enhancedProducts.Count(p => p.IsActive),
                            InactiveProducts = enhancedProducts.Count(p => !p.IsActive)
                        }
                    });
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "Error retrieving FREE ads products",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
            .AllowAnonymous()
            .WithName("GetFreeAdsProducts")
            .WithTags("Products", "FREE Ads")
            .WithSummary("Get all FREE ads products")
            .WithDescription("Retrieves all FREE ads products with category statistics")
            .Produces<object>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }

        #endregion
    }
}