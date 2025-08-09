using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class FileUploadRequest
    {
        [JsonPropertyName("container")]
        public string Container { get; set; }

        [JsonPropertyName("file")]
        public string Base64 { get; set; }

        [JsonPropertyName("filename")]
        [DefaultValue("")]
        public string? FileName { get; set; } = "";

    }
}
