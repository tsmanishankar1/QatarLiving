using Microsoft.AspNetCore.Http;
using QLN.Common.DTO_s.Company;
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
        public static async Task<(string uid, string username, List<UserSubscription> subscriptions, List<string> roles)>
            ExtractUserAndSubscriptionDetailsAsync(HttpContext httpContext)
        {
            string uid = "";
            string username = "unknown";
            var subscriptions = new List<UserSubscription>();
            var roles = new List<string>();

            try
            {
                uid = httpContext.User.FindFirst("sub")?.Value ?? string.Empty;
                username = httpContext.User.FindFirst("preferred_username")?.Value ?? "unknown";

                var subscriptionClaims = httpContext.User.FindAll("subscriptions").ToList();
                foreach (var claim in subscriptionClaims)
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(claim.Value);
                        var sub = doc.RootElement;

                        subscriptions.Add(new UserSubscription
                        {
                            Id = sub.GetProperty("Id").GetString(),
                            Vertical = sub.GetProperty("Vertical").GetInt32(),
                            SubVertical = sub.TryGetProperty("SubVertical", out var subV) && subV.ValueKind != JsonValueKind.Null
                                ? subV.GetInt32()
                                : (int?)null,
                            StartDate = DateTime.Parse(sub.GetProperty("StartDate").GetString()),
                            EndDate = DateTime.Parse(sub.GetProperty("EndDate").GetString())
                        });
                    }
                    catch
                    {
                        continue;
                    }
                }

                var roleClaims = httpContext.User.FindAll("roles").ToList();
                foreach (var role in roleClaims)
                {
                    roles.Add(role.Value);
                }
            }
            catch { }

            return (uid, username, subscriptions, roles);
        }
        /// <summary>

        /// Parses the JSON "user" claim (Drupal). Returns (uid, username, email, alias, qlNextUserId, roles).

        /// </summary>

        public static (string uid, string username)

             GetDrupalUser(HttpContext httpContext)

        {

            string uid = string.Empty, username = string.Empty;

            string? email = null, alias = null, qlNextUserId = null;

            var roles = new List<string>();

            if (TryGetDrupalUserElement(httpContext, out var user))

            {

                uid = TryGetString(user, "uid") ?? string.Empty;

                username = TryGetString(user, "name") ?? string.Empty;

            }

            return (uid, username);

        }

        private static bool TryGetDrupalUserElement(HttpContext httpContext, out JsonElement user)

        {

            user = default;

            var userClaim = httpContext.User.FindFirst("user")?.Value;

            return TryParseJson(userClaim, out user);

        }


        private static bool TryParseJson(string? json, out JsonElement root)

        {

            root = default;

            if (string.IsNullOrWhiteSpace(json)) return false;

            try

            {

                using var doc = JsonDocument.Parse(json);

                root = doc.RootElement.Clone();

                return root.ValueKind == JsonValueKind.Object;

            }

            catch { return false; }

        }

        private static string? TryGetString(JsonElement obj, string name)

        {

            if (!obj.TryGetProperty(name, out var v)) return null;

            return v.ValueKind switch

            {

                JsonValueKind.String => v.GetString(),

                JsonValueKind.Number => v.TryGetInt64(out var n) ? n.ToString() : v.ToString(),

                JsonValueKind.True => "true",

                JsonValueKind.False => "false",

                _ => null

            };

        }

    }
}
