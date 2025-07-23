using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class DrupalAuthResponse
    {
        [JsonPropertyName("status")]
        public bool Status { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("user")]
        public User User { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("token")]
        public string Token { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("expiry")]
        public int Expiry { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("errorCode")]
        public int ErrorCode { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("messages")]
        public List<string> Messages { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("message")]
        public string Message { get; set; }
    }

    public class User
    {
        [JsonPropertyName("uid")]
        public string Uid { get; set; }

        [JsonPropertyName("qlnext_user_id")]
        public string QLNextUserId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("mail")]
        public string Mail { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("init")]
        public string Init { get; set; }

        [JsonPropertyName("gender")]
        public string Gender { get; set; }

        [JsonPropertyName("phone")]
        public string Phone { get; set; }

        [JsonPropertyName("age")]
        public string Age { get; set; }

        [JsonPropertyName("birthday")]
        public string Birthday { get; set; }

        [JsonPropertyName("picture")]
        public string Picture { get; set; }

        [JsonPropertyName("default_picture")]
        public string DefaultPicture { get; set; }

        [JsonPropertyName("nationality_id")]
        public string NationalityId { get; set; }

        [JsonPropertyName("nationality_name")]
        public string NationalityName { get; set; }

        [JsonPropertyName("created")]
        public string Created { get; set; }

        [JsonPropertyName("user_verified")]
        public string UserVerified { get; set; }

        [JsonPropertyName("roles")]
        public Dictionary<string,string> Roles { get; set; }

        [JsonPropertyName("subscription")]
        public int Subscription { get; set; }

        [JsonPropertyName("subscription_type")]
        public string SubscriptionType { get; set; }

        [JsonPropertyName("buy_now")]
        public int BuyNow { get; set; }

        [JsonPropertyName("sub")]
        public object Sub { get; set; }
    }
}
