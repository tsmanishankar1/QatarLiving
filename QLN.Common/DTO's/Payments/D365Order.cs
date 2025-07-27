using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s.Payments
{
    public class D365Order
    {
        /// <summary>
        /// QL User id
        /// </summary>
        public int QLUserId { get; set; }

        /// <summary>
        /// QL User name
        /// </summary>
        public string QLUsername { get; set; } = string.Empty;

        /// <summary>
        /// Order Id of D365
        /// </summary>
        public string OrderId { get; set; } = string.Empty;

        /// <summary>
        /// Mobile number of the user
        /// </summary>
        public string Mobile { get; set; } = string.Empty;

        /// <summary>
        /// Email of the user
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// D365 item id
        /// </summary>
        public string D365Itemid { get; set; } = string.Empty;

        /// <summary>
        /// D365 Customer ID
        /// </summary>
        public string D365CustId { get; set; } = string.Empty;

        /// <summary>
        /// Classification for properties
        /// </summary>
        public string Classification { get; set; } = string.Empty;

        /// <summary>
        /// Ad ID
        /// </summary>
        public int? AdId { get; set; }

        /// <summary>
        /// Price of the item
        /// </summary>
        public decimal? Price { get; set; }

        /// <summary>
        /// Qty of the item
        /// </summary>
        public int? Qty { get; set; }

        /// <summary>
        /// Start date of the advertisement
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// End date of the advertisement
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Sales Type
        /// </summary>
        public string? SalesType { get; set; }
    }
}
