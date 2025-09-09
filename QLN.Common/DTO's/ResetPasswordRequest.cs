
using System.ComponentModel.DataAnnotations;

namespace QLN.Common.DTO_s
{
    public class ResetPasswordRequest
    {
        [Required]
        public string Email { get; set; } = string.Empty;
        [Required]
        public string ResetCode { get; set; } = string.Empty;
        [Required]
        public string NewPassword { get; set; } = string.Empty;
    }
}
