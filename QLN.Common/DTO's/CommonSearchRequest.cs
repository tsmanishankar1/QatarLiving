using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class CommonSearchRequest

    {
        [StringLength(50)]
        public string Text { get; set; } = "*";
        public Dictionary<string, object> Filters { get; set; } = new();
        public string? OrderBy { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }
}
