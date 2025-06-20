using System.ComponentModel.DataAnnotations;

namespace QLN.ContentBO.WebUI.Models
{
    public class V2ContentNewsDto
    {
        public Guid Id { get; set; }
        public Guid CreatedBy { get; set; }
        public Guid? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string Image_url { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public string date_created { get; set; }
        public string publishedDate { get; set; }
        public string authorName { get; set; }
        public string title { get; set; }

        [MaxLength(2000)]
        public string description { get; set; }
        public List<string> WriterTag { get; set; }
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; }
        public StatusType Status { get; set; }

        // DTO for Category, which includes SubCategory
        public class NewsCategoryDto
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
            public List<NewsCategoryDto>? SubCategory { get; set; }
        }

        public enum StatusType
        {
            Unpublished,
            Published

        }
    }
}
