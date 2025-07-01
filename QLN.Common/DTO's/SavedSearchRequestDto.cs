using QLN.Common.DTO_s;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.DTO_s
{
    public class SaveSearchRequestDto
    {

        [MinLength(1)]
        [MaxLength]
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public CommonSearchRequest SearchQuery { get; set; } = new();
        public string subVertical { get; set; }
    }
    public class SaveSearchRequestByIdDto
    {

        [MinLength(1)]
        [MaxLength]
        public string Name { get; set; } = string.Empty;
        public string UserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public CommonSearchRequest SearchQuery { get; set; } = new();
    }


}
