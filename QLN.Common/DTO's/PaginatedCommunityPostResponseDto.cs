using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class PaginatedCommunityPostResponseDto
    {
        public int Total { get; set; }
        public List<V2CommunityPostDto> Items { get; set; } = new();
    }
}
