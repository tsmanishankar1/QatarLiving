using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using QLN.Common.DTO_s;
using System;

public static class SubscriptionEndpoints
{
    public static IEndpointRouteBuilder MapSubscriptionEndpoints(this IEndpointRouteBuilder endpoints, bool enableAuthorization = true)
    {
        var group = endpoints.MapGroup("/subscriptions");


        group.MapPost("/", async (SubscriptionDto subscription, ISubscriptionService service) =>
        {
            await service.CreateSubscriptionAsync(subscription);
            return Results.Created($"/subscriptions/{subscription.Id}", subscription);
        })
        .WithName("CreateSubscription")
        .WithSummary("Create or update a subscription");

        group.MapGet("/{id:guid}", async (Guid id, ISubscriptionService service) =>
        {
            var subscription = await service.GetSubscriptionByIdAsync(id);
            return subscription is not null ? Results.Ok(subscription) : Results.NotFound();
        })
        .WithName("GetSubscriptionById")
        .WithSummary("Fetch subscription by ID");

        group.MapPost("/{id:guid}/expire", async (Guid id, ISubscriptionService service) =>
        {
            await service.ExpireSubscriptionAsync(id);
            return Results.Ok(new { message = $"Subscription {id} expired." });
        })
        .WithName("ExpireSubscription")
        .WithSummary("Expire a subscription");

        return endpoints;
    }
}
