namespace QLN.Web.Shared.Model
{
    public class PostModel
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

}
