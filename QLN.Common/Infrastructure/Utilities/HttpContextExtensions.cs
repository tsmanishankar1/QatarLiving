using Microsoft.AspNetCore.Http;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Subscriptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.Utilities
{
    public static class HttpContextExtensions
    {
        public static Guid? GetSubscriptionId(ClaimsPrincipal user, Vertical vertical, SubVertical? subVertical = null)
        {
            var subscriptionClaims = user.FindAll("subscriptions").ToList();
            if (!subscriptionClaims.Any())
                return null;

            string? subscriptionId = null;

            foreach (var subClaim in subscriptionClaims)
            {
                using var doc = JsonDocument.Parse(subClaim.Value);
                var sub = doc.RootElement;

                if (sub.TryGetProperty("Vertical", out var verticalProp) &&
                    verticalProp.ValueKind == JsonValueKind.Number)
                {
                    var v = (Vertical)verticalProp.GetInt32();
                    var sv = sub.TryGetProperty("SubVertical", out var subVerticalProp) &&
                             subVerticalProp.ValueKind == JsonValueKind.Number
                        ? (SubVertical?)subVerticalProp.GetInt32()
                        : null;

                    bool match = v == vertical && (subVertical == null || sv == subVertical);

                    if (match)
                    {
                        if (sub.TryGetProperty("Id", out var idProp))
                        {
                            subscriptionId = idProp.GetString();
                            break; 
                        }
                    }
                }
            }

            
            if (string.IsNullOrEmpty(subscriptionId))
            {
                using var doc = JsonDocument.Parse(subscriptionClaims.First().Value);
                subscriptionId = doc.RootElement.GetProperty("Id").GetString();
            }

            
            return Guid.TryParse(subscriptionId, out var subId) ? subId : null;
        }
    }
}

    


