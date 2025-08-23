using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Subscriptions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.Model
{
    

    public class CategoryDropdowndto
    {
        public long? Id { get; set; }
        public string CategoryName { get; set; } = default!;
        public string Vertical { get; set; } = default!;
        public string SubVertical { get; set; } = default!;
        public long? ParentId { get; set; }
        public List<FieldDtos>? Fields { get; set; } = new();
    }
    public class FieldDtos
    {
        public long? Id { get; set; }
        public string CategoryName { get; set; } = default!;
        public string? Type { get; set; }
        public List<string>? Options { get; set; } = new();
        public List<FieldDto>? Fields { get; set; } = new();
    }
}
