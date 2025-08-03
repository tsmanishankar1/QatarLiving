using System.Text.Json.Serialization;

namespace QLN.ContentBO.WebUI.Models
{
    public class BusinessVerificationItem
    {
        public string UserId { get; set; }
        [JsonPropertyName("companyName")]
        public string BusinessName { get; set; }
        public string UserName { get; set; }
        [JsonPropertyName("crDocument")]
        public string CRFile { get; set; }
        public string CRLicense { get; set; }
        [JsonPropertyName("crNumber")]
        public int CrNumber { get; set; }
        public DateTime EndDate { get; set; }
    }
}