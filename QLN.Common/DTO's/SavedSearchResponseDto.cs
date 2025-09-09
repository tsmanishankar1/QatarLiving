using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Subscriptions;
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
        public string UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public CommonSearchRequest SearchQuery { get; set; } = new();
        public SubVertical? SubVertical { get; set; }
        public Vertical Vertical { get; set; }

    }
}
