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
    public class SaveSearch
    {
        [Key]
        public Guid Id { get; set; }

        [MinLength(1)]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        public Vertical Vertical { get; set; }

        public SubVertical? SubVertical { get; set; }

        [Required]
        [MaxLength(255)]
        [Column(TypeName = "jsonb")]
        public CommonSearchRequest SearchQuery { get; set; } = new CommonSearchRequest();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string UserId { get; set; }
    }
}
