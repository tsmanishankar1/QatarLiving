using QLN.Common.Infrastructure.Subscriptions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class PayToPublishDto
    {
        public Guid Id { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Duration { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int TotalCount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public Vertical VerticalTypeId { get; set; }
        public SubscriptionCategory CategoryId { get; set; }
        public Status StatusId { get; set; }

        public DateTime LastUpdated { get; set; }
    }
    public class PayToPublishRequestDto
    {
        [Required]
        public string PlanName { get; set; } = string.Empty;
        [Required]
        public int TotalCount { get; set; }
        [Required]
        public string Description { get; set; } = string.Empty;
        [Required]
        public string Duration { get; set; } = string.Empty;
        [Required]
        public decimal Price { get; set; }
        [Required]
        public string Currency { get; set; } = string.Empty;
        [Required]
        public Vertical VerticalTypeId { get; set; }
        [Required]
        public SubscriptionCategory CategoryId { get; set; }
        [Required]
        public Status StatusId { get; set; }
    }
    public class PayToPublishResponseDto
    {
        public Guid Id { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Duration { get; set; } = string.Empty;
    }

    public class PayToPublishListResponseDto
    {
        public int VerticalId { get; set; }
        public string VerticalName { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public List<PayToPublishResponseDto> PayToPublish { get; set; } = new();
    }

    public class PaymentDto
    {
        public Guid Id { get; set; }
        public Guid PayToPublishId { get; set; }
        public int VerticalId { get; set; }
        public int CategoryId { get; set; }
        public Guid UserId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string CardNumber { get; set; } = string.Empty;
        public string ExpiryMonth { get; set; } = string.Empty;
        public string ExpiryYear { get; set; } = string.Empty;
        public string Cvv { get; set; } = string.Empty; 
        public string CardHolderName { get; set; } = string.Empty;
        public bool IsExpired { get; set; } = false;
     
        public DateTime LastUpdated { get; set; }
        
    }
    public class PaymentRequestDto
    {
        [Required]
        public int VerticalId { get; set; }

        [Required]
        public int CategoryId { get; set; }

        [Required]
        public Guid PayToPublishId { get; set; }


        public CardDetailPaymentDto CardDetails { get; set; } = new();
    }

    public class CardDetailPaymentDto
    {
        [Required]

        public string CardNumber { get; set; } = string.Empty;

        [Required]

        public string ExpiryMonth { get; set; } = string.Empty;

        [Required]

        public string ExpiryYear { get; set; } = string.Empty;

        [Required]

        public string Cvv { get; set; } = string.Empty;

        [Required]

        public string CardHolderName { get; set; } = string.Empty;
    }

}



