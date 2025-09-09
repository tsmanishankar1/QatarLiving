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
        public static (string? Uid, string? UserName, string? SubscriptionId) GetUserInfo(this HttpContext httpContext)
        {
            var uid = httpContext.User.FindFirst("sub")?.Value;
            var userName = httpContext.User.FindFirst("preferred_username")?.Value;

            var subscriptionClaim = httpContext.User.FindFirst("subscriptions")?.Value;
            string? subscriptionId = null;

            if (!string.IsNullOrEmpty(subscriptionClaim))
            {
                using var jsonDoc = JsonDocument.Parse(subscriptionClaim);
                var root = jsonDoc.RootElement;

                if (root.ValueKind == JsonValueKind.Array)
                {
                    subscriptionId = root[0].GetProperty("Id").GetString();
                }
                else if (root.ValueKind == JsonValueKind.Object)
                {
                    subscriptionId = root.GetProperty("Id").GetString();
                }
            }

            return (uid, userName, subscriptionId);
        }
    }
}

    


