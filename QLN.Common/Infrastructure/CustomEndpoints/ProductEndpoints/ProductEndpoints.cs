using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
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
using static QLN.Common.DTO_s.Enums.Enum;

namespace QLN.Common.Infrastructure.CustomEndpoints.ProductEndpoints
{
    public static class ProductEndpoints
    {
        public static RouteGroupBuilder MapProductEndpoints(this RouteGroupBuilder group)
        {
            // Get all products
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
            .WithName("GetAllProducts")
            .WithTags("Products")
            .WithSummary("Get all active products")
            .WithDescription("Retrieves all active products across all verticals")
            .Produces<List<ProductResponseDto>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // Get products by vertical
            group.MapGet("/vertical/{vertical:int}", async (
                Vertical vertical,
                [FromServices] IProductService productService,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    if (!Enum.IsDefined(typeof(Vertical), vertical))
                        return Results.BadRequest(new ProblemDetails
                        {
                            Title = "Invalid Data",
                            Detail = "Invalid vertical type",
                            Status = StatusCodes.Status400BadRequest
                        });

                    var products = await productService.GetProductsByVerticalAsync((Vertical)vertical, cancellationToken);
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
            .WithName("GetProductsByVertical")
            .WithTags("Products")
            .WithSummary("Get products by vertical")
            .WithDescription("Retrieves all active products for a specific vertical")
            .Produces<List<ProductResponseDto>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // Get products by type
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
            .WithName("GetProductsByType")
            .WithTags("Products")
            .WithSummary("Get products by type")
            .WithDescription("Retrieves all active products for a specific product type")
            .Produces<List<ProductResponseDto>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // Get product by code
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
            .WithName("GetProductByCode")
            .WithTags("Products")
            .WithSummary("Get product by code")
            .WithDescription("Retrieves a specific product by its product code")
            .Produces<ProductResponseDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            // Create product (Admin only)
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

            // Update product (Admin only)
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

            // Delete product (Admin only)
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
    }
}
