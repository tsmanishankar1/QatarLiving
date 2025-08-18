using QLN.Common.DTO_s.ClassifiedsBo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.Utilities
{
   

    public static class GenericClaimsHelper
    {
        public static (string? UserId, List<SubscriptionToken> Subscriptions, string? Error) GetValidSubscriptions(
            ClaimsPrincipal user, int Vertical, int SubVertical)
        {
            try
            {
                var userId = user.FindFirst("sub")?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return (null, new List<SubscriptionToken>(), "User ID not found in token.");
                }

                var subscriptionClaims = user.FindAll("subscriptions");
                var subscriptions = new List<SubscriptionToken>();

                foreach (var claim in subscriptionClaims)
                {
                    try
                    {
                        var sub = JsonSerializer.Deserialize<SubscriptionToken>(claim.Value, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        if (sub != null)
                            subscriptions.Add(sub);
                    }
                    catch (Exception ex)
                    {
                       
                        return (userId, new List<SubscriptionToken>(), $"Error deserializing subscription: {ex.Message}");
                    }
                }

                var validSubscriptions = subscriptions
                    .Where(s => s.Vertical == Vertical && s.SubVertical == SubVertical && s.EndDate >= DateTime.UtcNow)
                    .ToList();

                return (userId, validSubscriptions, null); 
            }
            catch (Exception ex)
            {
                return (null, new List<SubscriptionToken>(), $"Unexpected error: {ex.Message}");
            }
        }

    }
}
