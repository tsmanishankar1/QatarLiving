using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s.Classifieds
{
    public class ClassifiedsPromoteDto
    {
        public long AdId { get; set; }
        public SubVertical SubVertical { get; set; }
        
        public bool? IsPromoted { get; set; }

        public bool? IsFeatured {  get; set; }
        
        public bool? Unpublished {  get; set; }
       
       

        
    }
}
    