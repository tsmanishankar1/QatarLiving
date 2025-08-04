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
    public class ClassifiedsSearchRequest

    {
        [Required]
        public string SubVertical { get; set; } = null!;

        [StringLength(50)]
        public string Text { get; set; } = "*";
        public Dictionary<string, object> Filters { get; set; } = new();
        public string? OrderBy { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }
    public enum SearchType
    {
        AdId,
        OrderId,
        Username,
        Email,
        PhoneNumber,
        General
    }

    public class SearchDetectionResult
    {
        public SearchType Type { get; set; }
        public string SearchTerm { get; set; }
        public string Filter { get; set; }
    }
}
