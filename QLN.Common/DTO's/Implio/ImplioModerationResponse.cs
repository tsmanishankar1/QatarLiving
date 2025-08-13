using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s.Implio
{

    public class ImplioModerationResponse
    {
        [JsonPropertyName("batchId")]
        public required string BatchId { get; set; }

        [JsonPropertyName("accepted")]
        public List<ImplioAccepted> Accepted { get; set; } = new List<ImplioAccepted>();

        [JsonPropertyName("rejected")]
        public List<ImplioRejected> Rejected { get; set; } = new List<ImplioRejected>();
    }

}
