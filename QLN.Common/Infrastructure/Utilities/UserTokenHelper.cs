using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.Utilities
{
    public static class UserTokenHelper
    {
        public static async Task<(string uid, string username, string? subscriptionId, DateTime? expiryDate)> ExtractUserAndSubscriptionDetailsAsync(
            HttpContext httpContext, int vertical, int? subVertical = null)
        {
            string uid = "";
            string? subscriptionId = null;
            DateTime? expiryDate = null;
            string username = "unknown";

            try
            {
                uid = httpContext.User.FindFirst("sub")?.Value;
                username = httpContext.User.FindFirst("preferred_username")?.Value ?? "unknown";

                if (string.IsNullOrEmpty(uid))
                {
                    var userClaim = httpContext.User.FindFirst("user")?.Value;
                    if (!string.IsNullOrEmpty(userClaim))
                    {
                        using var doc = JsonDocument.Parse(userClaim);
                        var user = doc.RootElement;

                        if (user.TryGetProperty("uid", out var uidProp) && !string.IsNullOrEmpty(uidProp.GetString()))
                            uid = uidProp.GetString();

                        if (user.TryGetProperty("name", out var unameProp) && !string.IsNullOrEmpty(unameProp.GetString()))
                            username = unameProp.GetString();

                        if (user.TryGetProperty("expiryDate", out var expiryProp) && !string.IsNullOrEmpty(expiryProp.GetString()))
                        {
                            if (DateTime.TryParse(expiryProp.GetString(), out var parsedDate))
                                expiryDate = parsedDate;
                        }
                    }
                }
                else
                {
                    var subscriptionClaims = httpContext.User.FindAll("subscriptions").ToList();
                    foreach (var claim in subscriptionClaims)
                    {
                        try
                        {
                            using var doc = JsonDocument.Parse(claim.Value);
                            var subscription = doc.RootElement;

                            if (subscription.TryGetProperty("Vertical", out var verticalProp) &&
                                verticalProp.GetInt32() == vertical &&
                                (!subVertical.HasValue ||
                                 (subscription.TryGetProperty("SubVertical", out var subVerticalProp) &&
                                  subVerticalProp.ValueKind != JsonValueKind.Null &&
                                  subVerticalProp.GetInt32() == subVertical.Value)))
                            {
                                subscriptionId = subscription.GetProperty("Id").GetString();

                                if (subscription.TryGetProperty("EndDate", out var endDateProp) &&
                                    endDateProp.ValueKind == JsonValueKind.String)
                                {
                                    if (DateTime.TryParse(endDateProp.GetString(), out var parsedExpiry))
                                        expiryDate = parsedExpiry;
                                }

                                break;
                            }
                        }
                        catch
                        {
                            continue;
                        }
                    }
                }

                return (uid, username, subscriptionId, expiryDate);
            }
            catch
            {
                return (uid, username, subscriptionId, expiryDate);
            }
        }
    }
}
