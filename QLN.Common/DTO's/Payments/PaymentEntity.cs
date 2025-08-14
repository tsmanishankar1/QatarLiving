using QLN.Common.Infrastructure.Subscriptions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s.Payments
{
    public class PaymentEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PaymentId { get; set; }

        public ProductType ProductType { get; set; }
        public Guid? UserSubscriptionId { get; set; }
        public Guid? UserAddonId { get; set; }
        public Vertical Vertical { get; set; }
        public SubVertical? SubVertical { get; set; }

        public string? AdId { get; set; }

        public PaymentStatus Status { get; set; }

        public decimal Fee { get; set; }
        public string PaidByUid { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime Date { get; set; } = DateTime.UtcNow;

        public PaymentMethod? PaymentMethod { get; set; }

        public CardType? CardType { get; set; } // what if not paying by card ?

        public Source Source { get; set; }

        public Gateway Gateway { get; set; }

        public string? TransactionId { get; set; }

        public int? AttachedPaymentId { get; set; } // not sure what this is for

        public GatewayResponse? GatewayResponse { get; set; } // not sure of the purpose of this one

        public string? Comments { get; set; }

        public TriggeredSource TriggeredSource { get; set; }

        [Column(TypeName = "decimal(10,1)")]
        [Required]
        public decimal? Points { get; set; } = 0.0m;
    }
}
