using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s.Payments
{
    public class D365Order
    {
        /// <summary>
        /// QL User id
        /// </summary>
        [JsonPropertyName("QLUserId")]
        public int QLUserId { get; set; }

        /// <summary>
        /// QL User name
        /// </summary>
        [JsonPropertyName("QLUsername")]
        public string QLUsername { get; set; }

        /// <summary>
        /// Order Id of D365
        /// </summary>
        [JsonPropertyName("OrderId")]
        public string OrderId { get; set; }

        /// <summary>
        /// Mobile number of the user
        /// </summary>
        [JsonPropertyName("Mobile")]
        public string Mobile { get; set; }

        /// <summary>
        /// Email of the user
        /// </summary>
        [JsonPropertyName("Email")]
        public string Email { get; set; }

        /// <summary>
        /// D365 item id
        /// </summary>
        [JsonPropertyName("D365Itemid")]
        public string D365Itemid { get; set; }

        /// <summary>
        /// D365 Customer ID
        /// </summary>
        [JsonPropertyName("D365CustId")]
        public string D365CustId { get; set; }

        /// <summary>
        /// Classification for properties
        /// </summary>
        [JsonPropertyName("Classification")]
        public string Classification { get; set; }

        /// <summary>
        /// Ad ID
        /// </summary>
        [JsonPropertyName("AdId")]
        public int AdId { get; set; }

        /// <summary>
        /// Price of the item
        /// </summary>
        [JsonPropertyName("Price")]
        public decimal? Price { get; set; }

        /// <summary>
        /// Qty of the item
        /// </summary>
        [JsonPropertyName("Qty")]
        public int? Qty { get; set; }

        /// <summary>
        /// Start date of the advertisement
        /// </summary>
        [JsonPropertyName("start_date")]
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// End date of the advertisement
        /// </summary>
        [JsonPropertyName("end_date")]
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Sales Type
        /// </summary>
        [JsonPropertyName("SalesType")]
        public string? SalesType { get; set; }
    }
}
