using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.DTO_s
{
    public class CommonSearchRequest

    {
        [Required]
        [StringLength(50, ErrorMessage = "The 'text' field must not exceed 50 characters.")]
        public string Text { get; set; } = "*";
        public int Top { get; set; } = 50;
        public Dictionary<string, object> Filters { get; set; } = new();
        public string? OrderBy { get; set; }
    }
}
