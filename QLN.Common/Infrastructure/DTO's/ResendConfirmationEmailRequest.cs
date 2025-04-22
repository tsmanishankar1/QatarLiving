

using System.ComponentModel.DataAnnotations;

namespace QLN.Common.DTO_s
{
    public class ResendConfirmationEmailRequest
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address format.")]
        public string Email { get; set; } = string.Empty;
    }
}
