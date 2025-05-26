//using QLN.Common.Infrastructure.Subscriptions;
//using System;
//using System.Collections.Generic;
//using System.ComponentModel.DataAnnotations;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace QLN.Common.DTO_s
//{
//    public class UpdateSubscriptionDto
//    {
//        [Required]
//        public string Name { get; set; }

//        [Required]
//        public Guid UserId { get; set; }

//        public DateTime? StartDate { get; set; }

//        public DateTime? EndDate { get; set; }

//        [Required]
//        public decimal AdsBudget { get; set; }

//        [Required]
//        public decimal PromoteBudget { get; set; }

//        [Required]
//        public decimal RefreshBudget { get; set; }

//        [Required]
//        public decimal Fee { get; set; }

//        [Required]
//        public SubscriptionStatus Status { get; set; }

//        [Required]
//        public Vertical Vertical { get; set; }

//        [Required]
//        public SubscriptionName SubscriptionTypeId { get; set; }

//        [Required]
//        public string D365ItemId { get; set; }
//    }
//}
