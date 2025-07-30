using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace QLN.Web.Shared.Models
{
    public class DrupalJWTToken
    {
        [JsonPropertyName("iss")]
        public string? Issuer { get; set; }

        [JsonPropertyName("sub")]
        public string? Subject { get; set; }

        [JsonPropertyName("aud")]
        public string? Audience { get; set; }

        [JsonPropertyName("iat")]
        public long? IssuedAt { get; set; }

        [JsonPropertyName("exp")]
        public long? ExpiresAt { get; set; }

        [JsonPropertyName("user")]
        public DrupalUser? DrupalUser { get; set; }
    }

    public class DrupalUser
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("language")]
        public string? Language { get; set; }

        [JsonPropertyName("created")]
        public string? Created { get; set; }

        [JsonPropertyName("access")]
        public string? Access { get; set; }

        [JsonPropertyName("login")]
        public long? Login { get; set; }

        [JsonPropertyName("init")]
        public string? Init { get; set; }

        [JsonPropertyName("timezone")]
        public string? Timezone { get; set; }

        [JsonPropertyName("uid")]
        public string? Uid { get; set; }

        [JsonPropertyName("qlnext_user_id")]
        public string? QlnextUserId { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("alias")]
        public string? Alias { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("phone")]
        public string? Phone { get; set; }

        [JsonPropertyName("path")]
        public string? Path { get; set; }

        [JsonPropertyName("image")]
        public string? Image { get; set; }

        [JsonPropertyName("is_admin")]
        public bool? IsAdmin { get; set; }

        [JsonPropertyName("permissions")]
        public List<string>? Permissions { get; set; }

        [JsonPropertyName("roles")]
        public List<string>? Roles { get; set; }

        //[JsonPropertyName("showroom_info")]
        //public List<object> ShowroomInfo { get; set; }

        [JsonPropertyName("subscription")]
        public DrupalSubscription? Subscription { get; set; }

        public class DrupalSubscription
        {
            [JsonPropertyName("reference_id")]
            public string? ReferenceId { get; set; }

            [JsonPropertyName("start_date")]
            public string? StartDate { get; set; }

            [JsonPropertyName("expire_date")]
            public string? ExpireDate { get; set; }

            [JsonPropertyName("uid")]
            public string? Uid { get; set; }

            [JsonPropertyName("product_type")]
            public string? ProductType { get; set; }

            [JsonPropertyName("access_dashboard")]
            public string? AccessDashboard { get; set; }

            [JsonPropertyName("product_class")]
            public string? ProductClass { get; set; }

            [JsonPropertyName("categories")]
            public List<string>? Categories { get; set; }

            [JsonPropertyName("subscription_categories")]
            public List<string>? SubscriptionCategories { get; set; }

            [JsonPropertyName("snid")]
            public string? Snid { get; set; }

            [JsonPropertyName("status")]
            public string? Status { get; set; }
        }
    }
}
