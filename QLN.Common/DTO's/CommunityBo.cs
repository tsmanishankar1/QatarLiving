using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class CommunityBo
    {
        public class ForumCategoryDto
        {
            public string Id { get; set; }
            public string Name { get; set; }
        }
        public class ForumCategoryListDto
        {
            public List<ForumCategoryDto> ForumCategories { get; set; }
        }

    }
}
