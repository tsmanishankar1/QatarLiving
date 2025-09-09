using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class ArticleItem
    {
        [JsonPropertyName("forum_id")]
        public string ForumId { get; set; }

        [JsonPropertyName("forum_category")]
        public string ForumCategory { get; set; }

        [JsonPropertyName("category_id")]
        public string CategoryId { get; set; }

        [JsonPropertyName("category")]
        public string Category { get; set; }

        [JsonPropertyName("user_name")]
        public string UserName { get; set; }

        [JsonPropertyName("nid")]
        public string Nid { get; set; }

        [JsonPropertyName("date_created")]
        public string DateCreated { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("image_url")]
        public string ImageUrl { get; set; }

        [JsonPropertyName("slug")]
        public string Slug { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("comment_count")]
        public string CommentCount { get; set; }
    }

    public class ArticleResponse
    {
        [JsonPropertyName("items")]
        public List<ArticleItem> Items { get; set; }

        [JsonPropertyName("total")]
        public string Total { get; set; }
    }

}
