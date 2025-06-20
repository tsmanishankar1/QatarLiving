using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class ClassifiedItems : CommonAdBase
    {        
        public Guid Id { get; set; }        
        public string AcceptsOffers { get; set; }
        public string? MakeType { get; set; }
    }
}
