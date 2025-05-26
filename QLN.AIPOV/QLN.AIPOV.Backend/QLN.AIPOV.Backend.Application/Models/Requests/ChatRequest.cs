using System.ComponentModel.DataAnnotations;

namespace QLN.AIPOV.Backend.Application.Models.Requests
{
    public class ChatRequest
    {
        [Required(AllowEmptyStrings = false)]
        public string Message { get; set; } = string.Empty;
    }
}
