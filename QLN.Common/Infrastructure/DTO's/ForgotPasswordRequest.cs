

using System.ComponentModel.DataAnnotations;

namespace QLN.Common.DTO_s
{
    public class ForgotPasswordRequest
    {
        [Required]
        public string Email { get; set; } = string.Empty;
    }
}
