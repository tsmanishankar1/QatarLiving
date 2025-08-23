using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s.Services
{
    public class CategoryDto
    {
        public long? Id { get; set; }
        public string CategoryName { get; set; } = default!;
        public string Vertical { get; set; } = default!;
        public string SubVertical { get; set; } = default!;
        public long? ParentId { get; set; }
        public List<FieldDto>? Fields { get; set; } = new();
    }
    public class FieldDto
    {
        public long? Id { get; set; }
        public string CategoryName { get; set; } = default!;
        public string? Type { get; set; }
        public List<string>? Options { get; set; } = new();
        public List<FieldDto>? Fields { get; set; } = new();
    }
}
