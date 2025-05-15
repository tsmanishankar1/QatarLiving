using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.DTO_s
{
    public class ClassifiedSearchRequest
    {
        public string Text { get; set; } = "*";
        public int Top { get; set; } = 50;
        public Dictionary<string, object> Filters { get; set; } = new();
        public string? OrderBy { get; set; }
    }
}
