
using CsvHelper.Configuration.Attributes;

namespace QLN.DataMigration.Models
{
    public class CsvCategoryMapper
    {
        [Name("ad_id")]
        public string AdId { get; set; } = string.Empty;
        [Name("Main")]
        public string Category { get; set; } = string.Empty;
        [Name("L1")]
        public string L1Category { get; set; } = string.Empty;
        [Name("L2")]
        public string? L2Category { get; set; }
    }
}