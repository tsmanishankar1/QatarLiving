using System.ComponentModel.DataAnnotations;

namespace QLN.Web.Shared.Models
{
    public class CommunityPostListResponse
    {
        public int Total { get; set; }
        public List<CommunityPostModel> Items { get; set; }
    }

    public class CommunityPostModel
    {
        public Guid Id { get; set; }
        public string UserName { get; set; }
        public string Title { get; set; }
        public string Slug { get; set; }
        public string Category { get; set; }
        public string CategoryId { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public string ImageBase64 { get; set; }
        public bool IsActive { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }
        public DateTime DateCreated { get; set; }
        public int LikeCount { get; set; }
        public int CommentCount { get; set; }
        public List<string> LikedUserIds { get; set; }
        public List<string> CommentedUserIds { get; set; }

    }

    public class CreateCommunityPostDto
    {
        [Required(ErrorMessage = "Category is required.")]
        public string CategoryId { get; set; }

        [Required(ErrorMessage = "Title is required.")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Description is required.")]
        public string Description { get; set; }

        public string ImageBase64 { get; set; }

        public string Category { get; set; }

        public bool IsActive { get; set; }
    }
}
