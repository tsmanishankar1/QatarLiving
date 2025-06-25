using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class ClassifiedDeals : CommonAdBase
    {
        public Guid Id { get; set; }
        public string? FlyerFile { get; set; }
        public string FlyerName { get; set; }
        public string XMLLink { get; set; }        
    }
}
