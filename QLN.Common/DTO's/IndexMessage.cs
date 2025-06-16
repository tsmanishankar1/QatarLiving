using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class IndexMessage
    {
        public string Vertical { get; set; } 
        public string Action { get; set; }   
        public CommonIndexRequest? UpsertRequest { get; set; }
        public string? DeleteKey { get; set; }
    }
}
