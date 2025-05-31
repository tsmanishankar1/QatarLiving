using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Indexes;

namespace QLN.Common.DTO_s
{
    public class AnalyticsIndex
    {
        /// <summary>
        /// Unique key for this section’s summary. 
        /// E.g. $"{Section}_{EntityId}" or just $"{EntityId}" if Section is constant.
        /// </summary>
        [SimpleField(IsKey = true, IsFilterable = true)]
        public string Id { get; set; }

        /// <summary>
        /// Which landing‐page section these metrics belong to
        /// (e.g. "ad", "banner", "featuredServices", "socialMedia", "subscribeNow").
        /// </summary>
        [SearchableField(IsFilterable = true, IsSortable = true)]
        public string Section { get; set; }

        /// <summary>
        /// The specific entity within that section (e.g. AdId, BannerId, SocialMediaTileId).
        /// </summary>
        [SimpleField(IsFilterable = true, IsSortable = true)]
        public string EntityId { get; set; }

        /// <summary>
        /// When this summary was last updated
        /// </summary>
        [SimpleField(IsFilterable = true, IsSortable = true)]
        public DateTimeOffset LastUpdated { get; set; }

        /// <summary>
        /// Total number of times shown (impressions)
        /// </summary>
        [SimpleField(IsFilterable = true, IsSortable = true)]
        public long Impressions { get; set; }

        /// <summary>
        /// Total number of detail‐page opens or similar “views”
        /// </summary>
        [SimpleField(IsFilterable = true, IsSortable = true)]
        public long Views { get; set; }

        /// <summary>
        /// Total WhatsApp taps/events
        /// </summary>
        [SimpleField(IsFilterable = true, IsSortable = true)]
        public long WhatsApp { get; set; }

        /// <summary>
        /// Total phone‐call taps/events
        /// </summary>
        [SimpleField(IsFilterable = true, IsSortable = true)]
        public long Calls { get; set; }

        /// <summary>
        /// Total share taps/events
        /// </summary>
        [SimpleField(IsFilterable = true, IsSortable = true)]
        public long Shares { get; set; }

        /// <summary>
        /// Total save taps/events
        /// </summary>
        [SimpleField(IsFilterable = true, IsSortable = true)]
        public long Saves { get; set; }
    }
}
