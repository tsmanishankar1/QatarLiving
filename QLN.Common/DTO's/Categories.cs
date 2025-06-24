using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class Categories
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public Guid ParentId { get; set; } = Guid.Empty;
        public List<CategoryField>? Fields { get; set; } = new();
    }

    public class CategoryField
    {
        public string Name { get; set; } = default!;
        public string Type { get; set; } = "text"; // "text", "number", "dropdown"
        public List<string>? Options { get; set; } // only for dropdown
    }

    public class CategoryDtos
    {
        public string Vertical { get; set; }
        public string Name { get; set; } = default!;
        public Guid? ParentId { get; set; }
        public List<CategoryField>? Fields { get; set; } = new();
    }

    public class CategoryTreeDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public List<CategoryField> Fields { get; set; } = new();
        public List<CategoryTreeDto> Children { get; set; } = new();
    }

}
