using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.DTO_s
{
    public class SavedSearchResponseDto
    {
        public Guid Id { get; set; }
        public Guid? UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public CommonSearchRequest SearchQuery { get; set; } = new();
    }


}
