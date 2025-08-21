
using CsvHelper.Configuration.Attributes;
using QLN.Common.DTO_s;

namespace QLN.DataMigration.Models
{
    public class ItemsCategoryMapper
    {
        [Name("ad_id")]
        public string AdId { get; set; } = string.Empty;

        [Ignore]
        public long? CategoryId { get; set;}

        [Name("Main")]
        public string Category { get; set; } = string.Empty;

        [Ignore]
        public long? L1CategoryId { get; set; }

        [Name("L1")]
        public string L1Category { get; set; } = string.Empty;

        [Ignore]
        public long? L2CategoryId { get; set; }

        [Name("L2")]
        public string? L2Category { get; set; }

        //[Name("snid")]
        //public string? SubscriptionId { get; set; } = string.Empty;

        [Ignore]
        public AdTypeEnum? AdType { get; set; } = AdTypeEnum.Free;

    }
}