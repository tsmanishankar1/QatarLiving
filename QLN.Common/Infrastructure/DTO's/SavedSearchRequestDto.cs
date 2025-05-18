using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.DTO_s
{
    public class SaveSearchRequestDto
    {
        public string Name { get; set; } = string.Empty; 
        public Guid? UserId { get; set; } 
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ClassifiedSearchRequest SearchQuery { get; set; } = new();
    }


}
