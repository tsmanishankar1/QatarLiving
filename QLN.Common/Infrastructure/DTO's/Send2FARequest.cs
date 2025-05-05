using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.DTO_s
{
    public class Send2FARequest
    {
        [Required]
        public string UsernameOrEmailOrPhone { get; set; } = null!;
        [Required]
        public string Method { get; set; } = null!;
    }
}
