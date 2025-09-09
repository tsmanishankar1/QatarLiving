using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Indexes;

namespace QLN.Common.DTO_s
{
    public class AnalyticsIndex
    {
        [SimpleField(IsKey = true, IsFilterable = true)]
        public string Id { get; set; }

        [SearchableField(IsFilterable = true, IsSortable = true)]
        public string Section { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public string EntityId { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public DateTimeOffset LastUpdated { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public long Impressions { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public long Views { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public long WhatsApp { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public long Calls { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public long Shares { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public long Saves { get; set; }
    }
}
