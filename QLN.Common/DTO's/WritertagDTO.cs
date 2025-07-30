using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class WritertagDTO
    {
        
        public string Tagname { get; set; } = string.Empty;
        public Guid tagId { get; set; }

    }
    public class getallwritertagsDTO
    {
        public List<string> Tags { get; set; }
    }

}
