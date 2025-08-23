using QLN.Common.DTO_s;
using QLN.Common.DTO_s.Services;
using QLN.Common.Infrastructure.Subscriptions;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.Model
{
    public class CategoryDropdown
    {
        [Key]
        public long Id { get; set; }

        public string CategoryName { get; set; } = default!;

        public long? ParentId { get; set; }

        [Column(TypeName = "jsonb")]
        public List<FieldDto>? Fields { get; set; }

        public Vertical Vertical { get; set; }
        public SubVertical? SubVertical { get; set; }
    }
}
