using Microsoft.AspNetCore.Http;
using QLN.Common.Infrastructure.DTO_s;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.Model
{

    public class Category
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public Guid ParentId { get; set; } = default!;
        public bool IsActive { get; set; }
        public bool IsFeaturedCategory { get; set; } = false;
        public string TypePrefix { get; set; } = default!;
    }

    
}
