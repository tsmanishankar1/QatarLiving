using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s.Payments
{
    public class D365PaymentLogsEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int PaymentId { get; set; }

        public int Status { get; set; }

        public Operation Operation { get; set; }

        public object Response { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
