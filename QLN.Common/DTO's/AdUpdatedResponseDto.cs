using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class AdUpdatedResponseDto
    {
        public Guid AdId { get; set; }
        public string Title { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string Message { get; set; }
    }
}
