using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace QLN.Common.DTO_s.Payments
{
    public class ProcessedOrder
    {
        [JsonPropertyName("_request")]
        public RequestData Request { get; set; }
    }

    public class RequestData
    {
        [JsonPropertyName("QLSalesOrderArray")]
        public List<OrderItem> QLSalesOrderArray { get; set; }
    }
}
