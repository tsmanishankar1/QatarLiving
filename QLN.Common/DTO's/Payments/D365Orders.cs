using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s.Payments
{
    public class D365Orders
    {
        [JsonPropertyName("order")]
        public D365Order[] Orders { get; set; }
    }
}
