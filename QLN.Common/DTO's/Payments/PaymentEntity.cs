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

        public int ProductType { get; set; }
        public string UserSubscriptionId { get; set; }
        public string SubscriptionFeaturedAddonsId { get; set; }
        public string SubscriptionRefreshedAddonsId { get; set; }
        public Vertical Vertical { get; set; }

        public string AdId { get; set; }

        public int Status { get; set; }

        public int Fee { get; set; }
        public string PaidByUid { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime Date { get; set; }

        public PaymentMethod PaymentMethod { get; set; }

        public CardType CardType { get; set; }

        public Source Source { get; set; }

        public string Gateway { get; set; }

        public string TransactionId { get; set; }

        public int AttachedPaymentId { get; set; }

        public int GatewayResponse { get; set; }

        public string Comments { get; set; }

        public int TriggeredSource { get; set; }

        [Column(TypeName = "decimal(10,1)")]
        [Required]
        public decimal Points { get; set; } = 0.0m;
    }
}
