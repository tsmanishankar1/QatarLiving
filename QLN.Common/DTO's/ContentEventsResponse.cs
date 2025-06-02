using Microsoft.AspNetCore.Http.Features;
using QLN.Common.Infrastructure.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.DTO_s
{
    public class ContentEventsResponse
    {
        [JsonPropertyName("total")]
        public string Total { get; set; }

        [JsonPropertyName("items")]
        public List<ContentEvent> Items { get; set; } = new();
    }
}
