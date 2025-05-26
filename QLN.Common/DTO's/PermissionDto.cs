using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.DTO_s
{
    public class PermissionDto
    {
        [JsonPropertyName("owners")]
        public IEnumerable<Guid> Owners { get; set; } = Array.Empty<Guid>();
        [JsonPropertyName("read")]
        public IEnumerable<Guid> Read { get; set; } = Array.Empty<Guid>();
        [JsonPropertyName("write")]
        public IEnumerable<Guid> Write { get; set; } = Array.Empty<Guid>();

    }
}
