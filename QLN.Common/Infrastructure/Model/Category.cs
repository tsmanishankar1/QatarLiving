using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Subscriptions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;


namespace QLN.Common.Infrastructure.Model
{
    public class Category
    {
        [Key]
        public long Id { get; set; }

        public string CategoryName { get; set; } = default!;

        public long? ParentId { get; set; }

        [ForeignKey("ParentId")]

        public Category? ParentCategory { get; set; }

        public List<Category>? CategoryFields { get; set; } = new();

        public string? Type { get; set; } = "text";

        public List<string>? Options { get; set; }

        public Vertical Vertical { get; set; }

        public SubVertical? SubVertical { get; set; }

    }
    public class CategoryDto
    {
        public long Id { get; set; }
        public string CategoryName { get; set; } = default!;

        public string Vertical { get; set; } = default!;
        public long? ParentId { get; set; }
        public string SubVertical { get; set; } = default!;
        public List<FieldDto>? Fields { get; set; } = new();

    }
    public class FieldDto
    {
        public long Id { get; set; }
        public string CategoryName { get; set; } = default!;

        public string? Type { get; set; } = "text";

        public List<string>? Options { get; set; }
        public List<FieldDto>? Fields { get; set; }

    }
}

