using System.ComponentModel.DataAnnotations;

namespace QLN.Web.Shared.Models.FeedbackRequest
{
    public class FeedbackFormModel
    {
        [Required(ErrorMessage = "User name is required")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Invalid email formt"), EmailAddress]
        public string Email { get; set; }

        [Required(ErrorMessage = "Invaild phone number")]
        [RegularExpression(@"^\+974\s?[3-7]\d{7}$", ErrorMessage = "Enter a valid Qatari number")]
        public string Mobile { get; set; } = "+974 ";

        [Required(ErrorMessage = "Category type is required")]
        public string Category { get; set; }

        [Required(ErrorMessage = "Feedback message is required")]
        public string Description { get; set; }
    }
}
