using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
public class IndexMessage
{
    [JsonPropertyName("action")]
    public string Action { get; set; }
    
    [JsonPropertyName("vertical")]
    public string Vertical { get; set; }
    
    [JsonPropertyName("deleteKey")]
    public string DeleteKey { get; set; }
    
    [JsonPropertyName("upsertRequest")]
    public CommonIndexRequest UpsertRequest { get; set; }
}
}
