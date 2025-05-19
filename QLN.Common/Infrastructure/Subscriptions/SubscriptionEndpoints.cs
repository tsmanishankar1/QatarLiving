using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using QLN.Common.DTO_s;
using System;

public static class SubscriptionEndpoints
    {
        public static RouteGroupBuilder MapSubscriptionEndpoints(this RouteGroupBuilder group)
        {
            group.MapPost("/create", async (
                [FromBody] SubscriptionDto dto,
                [FromServices] ISubscriptionService service,
                HttpContext ctx,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    Console.WriteLine("📥 Received request: " + System.Text.Json.JsonSerializer.Serialize(dto));
                    
                    // Set ID if not provided
                    if (dto.Id == Guid.Empty)
                    {
                        dto.Id = Guid.NewGuid();
                    }
                    
                    // Set start date if not provided
                    if (dto.StartDate == null)
                    {
                        dto.StartDate = DateTime.UtcNow;
                    }
                    
                    var result = await service.CreateSubscriptionAsync(dto, cancellationToken);
                    if (!result)
                    {
                        return Results.Problem("Failed to create subscription");
                    }
                    
                    return Results.Created($"/api/subscriptions/getById?id={dto.Id}", dto);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("❌ Error: " + ex.Message);
                    return Results.Problem("💥 Internal server error: " + ex.Message);
                }
            });

            group.MapGet("/getById", async (
                Guid id,
                [FromServices] ISubscriptionService service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var result = await service.GetSubscriptionAsync(id, cancellationToken);
                    return result is null ? Results.NotFound() : Results.Ok(result);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("❌ Error: " + ex.Message);
                    return Results.Problem("💥 Internal server error: " + ex.Message);
                }
            });

            group.MapGet("/getAll", async (
                [FromServices] ISubscriptionService service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    // Since actors are individual instances, we need a separate implementation 
                    // to get all subscriptions. This is a placeholder that would need to be 
                    // implemented in the service layer.
                    return Results.BadRequest("Getting all subscriptions is not directly supported with actors. Please use a specific ID.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("❌ Error: " + ex.Message);
                    return Results.Problem("💥 Internal server error: " + ex.Message);
                }
            });

            group.MapPut("/update", async (
                [FromBody] SubscriptionDto dto,
                [FromServices] ISubscriptionService service,
                HttpContext ctx,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    if (dto.Id == Guid.Empty)
                    {
                        return Results.BadRequest("Subscription ID is required");
                    }
                    
                    var result = await service.UpdateSubscriptionAsync(dto, cancellationToken);
                    if (!result)
                    {
                        return Results.NotFound($"Subscription with ID {dto.Id} not found");
                    }
                    
                    return Results.Ok(dto);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("❌ Error: " + ex.Message);
                    return Results.Problem("💥 Internal server error: " + ex.Message);
                }
            });

            group.MapDelete("/delete", async (
                Guid id,
                [FromServices] ISubscriptionService service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var result = await service.DeleteSubscriptionAsync(id, cancellationToken);
                    if (!result)
                    {
                        return Results.NotFound($"Subscription with ID {id} not found");
                    }
                    
                    return Results.NoContent();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("❌ Error: " + ex.Message);
                    return Results.Problem("💥 Internal server error: " + ex.Message);
                }
            });

            group.MapPost("/expire", async (
                Guid id,
                [FromServices] ISubscriptionService service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var result = await service.ExpireSubscriptionAsync(id, cancellationToken);
                    if (!result)
                    {
                        return Results.NotFound($"Subscription with ID {id} not found");
                    }
                    
                    return Results.Ok($"Subscription {id} expired successfully");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("❌ Error: " + ex.Message);
                    return Results.Problem("💥 Internal server error: " + ex.Message);
                }
            });

            return group;
        }
    }
