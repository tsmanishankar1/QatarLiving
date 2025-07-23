using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.IService.ISubscriptionService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.CustomEndpoints.SubscriptionEndpoints
{
    public static class UserQuotaEndpoints
    {
        public static RouteGroupBuilder MapUserQuotaEndpoints(this RouteGroupBuilder group)
        {
            // 1. GET - All quotas for user
            group.MapGet("/users/{userId}/quotas", async Task<IResult> (
                string userId,
                [FromServices] IUserQuotaService quotaService,
                [FromServices] ILogger<IUserQuotaService> logger) =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(userId))
                        return TypedResults.BadRequest("User ID is required");

                    var quotas = await quotaService.GetUserQuotasAsync(userId);

                    if (quotas?.Quotas == null || !quotas.Quotas.Any())
                        return TypedResults.NotFound($"No quotas found for user {userId}");

                    return TypedResults.Ok(quotas);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error getting quotas for user {UserId}", userId);
                    return TypedResults.Problem("Error retrieving user quotas");
                }
            })
            .WithName("GetUserQuotas")
            .WithTags("UserQuota")
            .WithSummary("Get all quotas for user");

            // 2. GET - Active quotas for user
            group.MapGet("/users/{userId}/quotas/active", async Task<IResult> (
                string userId,
                [FromServices] IUserQuotaService quotaService,
                [FromServices] ILogger<IUserQuotaService> logger) =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(userId))
                        return TypedResults.BadRequest("User ID is required");

                    var activeQuotas = await quotaService.GetActiveUserQuotasAsync(userId);
                    return TypedResults.Ok(activeQuotas);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error getting active quotas for user {UserId}", userId);
                    return TypedResults.Problem("Error retrieving active user quotas");
                }
            })
            .WithName("GetActiveUserQuotas")
            .WithTags("UserQuota")
            .WithSummary("Get active quotas for user");

            // 3. PUT - Update quota
            group.MapPut("/users/{userId}/quotas/{transactionId:guid}", async Task<IResult> (
                string userId,
                Guid transactionId,
                GenericUserQuotaDto updatedQuota,
                [FromServices] IUserQuotaService quotaService,
                [FromServices] ILogger<IUserQuotaService> logger) =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(userId))
                        return TypedResults.BadRequest("User ID is required");

                    if (transactionId == Guid.Empty)
                        return TypedResults.BadRequest("Valid transaction ID is required");

                    if (updatedQuota == null)
                        return TypedResults.BadRequest("Updated quota data is required");

                    var success = await quotaService.UpdateUserQuotaAsync(userId, transactionId, updatedQuota);

                    if (!success)
                        return TypedResults.NotFound($"Quota not found for transaction {transactionId}");

                    return TypedResults.Ok(new { Message = "Quota updated successfully" });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error updating quota for user {UserId}, transaction {TransactionId}", userId, transactionId);
                    return TypedResults.Problem("Error updating user quota");
                }
            })
            .WithName("UpdateUserQuota")
            .WithTags("UserQuota")
            .WithSummary("Update user quota");

            // 4. DELETE - Delete quota
            group.MapDelete("/users/{userId}/quotas/{transactionId:guid}", async Task<IResult> (
                string userId,
                Guid transactionId,
                [FromServices] IUserQuotaService quotaService,
                [FromServices] ILogger<IUserQuotaService> logger) =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(userId))
                        return TypedResults.BadRequest("User ID is required");

                    if (transactionId == Guid.Empty)
                        return TypedResults.BadRequest("Valid transaction ID is required");

                    var success = await quotaService.DeleteUserQuotaAsync(userId, transactionId);

                    if (!success)
                        return TypedResults.NotFound($"Quota not found for transaction {transactionId}");

                    return TypedResults.Ok(new { Message = "Quota deleted successfully" });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error deleting quota for user {UserId}, transaction {TransactionId}", userId, transactionId);
                    return TypedResults.Problem("Error deleting user quota");
                }
            })
            .WithName("DeleteUserQuota")
            .WithTags("UserQuota")
            .WithSummary("Delete user quota");

            return group;
        }
    }
}
