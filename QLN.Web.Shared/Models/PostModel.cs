using System.Text.Json.Serialization;

namespace QLN.Web.Shared.Model
{
    public class PostModel
    {
        public string Id { get; set; } 
        public string Category { get; set; }
        public string CategoryId { get; set; }
        public string Title { get; set; }
        public string? ImageUrl { get; set; }
        public string BodyPreview { get; set; }
        public string Author { get; set; }
        public string slug { get; set; }
        public DateTime Time { get; set; }
        public int LikeCount { get; set; }
        public int CommentCount { get; set; }
        public bool isCommented { get; set; }
        public string Slug { get; set; } 
        public int TotalCount { get; set; }
        public List<CommentModel> Comments { get; set; } = new();
    }

    public class PostDetailsModel
    {
        public string Id { get; set; }
        public string Category { get; set; }
        public string Title { get; set; }
        public string? ImageUrl { get; set; }
        public string BodyPreview { get; set; }
        public string Author { get; set; }
        public DateTime Time { get; set; }
        public int LikeCount { get; set; }
        public int CommentCount { get; set; }
        public bool isCommented { get; set; }
        public string Slug { get; set; }
        public List<CommentModel> Comments { get; set; } = new();
    }

    public class CommentModel
    {
        public string Avatar { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string Description { get; set; } = string.Empty;
        public int LikeCount { get; set; }
        public int UnlikeCount { get; set; }
        public bool IsByCurrentUser { get; set; }
    }
  
    public class PostListDto
    {
        public string forum_id { get; set; }
        public string forum_category { get; set; }
        public string category_id { get; set; }
        public string user_name { get; set; }
        public string nid { get; set; }
        public string date_created { get; set; }
        public string title { get; set; }
        public string slug { get; set; }
        public string description { get; set; }
        public string image_url { get; set; }
        public List<CommentDto> comments { get; set; } = new();

    }
    public class PostDetailsDto
    {
        public string forum_id { get; set; }
        public string category { get; set; }
        public string user_name { get; set; }
        public string nid { get; set; }
        public string date_created { get; set; }
        public string title { get; set; }
        public string slug { get; set; }
        public string description { get; set; }
        public string image_url { get; set; }
        public List<CommentDto> comments { get; set; } = new();

    }
    public class CommentDto
    {
        public string comment_id { get; set; } = string.Empty;
        public string user_name { get; set; } = string.Empty;
        public DateTime created_date { get; set; }
        public string subject { get; set; } = string.Empty;
        public int LikeCount { get; set; }
        public int UnlikeCount { get; set; }
        public string profile_picture { get; set; }

    }
    public class PostListResponse
    {
        [JsonPropertyName("items")]
        public List<PostListDto> items { get; set; }

        [JsonPropertyName("total")]
        public int total { get; set; }
    }

}
