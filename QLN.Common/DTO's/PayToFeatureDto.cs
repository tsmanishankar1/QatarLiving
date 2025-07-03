using QLN.Common.Infrastructure.Subscriptions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
   

    public class PayToFeatureDto
    {
        public Guid Id { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; } // Updated
        public decimal Price { get; set; }
        public int TotalCount { get; set; }
         //public BasicPrice BasicPriceId { get; set; }
        public string Currency { get; set; } = string.Empty;
        public Vertical VerticalTypeId { get; set; }
        public SubscriptionCategory CategoryId { get; set; }
     public bool IsPromoteAd { get; set; }
        public bool IsFeaturedAd { get; set; }
        public Status StatusId { get; set; }
        public DateTime LastUpdated { get; set; }

    }
    public class PayToFeatureDataDto
    {
        public Guid Id { get; set; }
        public DateTime LastUpdated { get; set; }
        public List<PayToFeatureDto> Plans { get; set; } = new();
        public List<PayToFeatureBasicPriceDto> BasicPrices { get; set; } = new();
    }
    public class PayToFeatureBasicPriceDto
    {
        public Guid Id { get; set; }
        public Vertical VerticalTypeId { get; set; }
        public SubscriptionCategory CategoryId { get; set; }
        public BasicPrice BasicPriceId { get; set; }
        public TimeSpan Duration { get; set; }
        public DateTime LastUpdated { get; set; }

    }
    public class PayToFeaturePlansResponse
    {
        public int? BasicPriceId { get; set; }
        public string Duration { get; set; }
        public List<PayToFeatureWithBasicPriceResponseDto> PlanDetails { get; set; } = new();
    }

    public class PayToFeatureWithBasicPriceResponseDto
    {
        public Guid Id { get; set; }
        public string PlanName { get; set; }
        public decimal Price { get; set; }
        public string Currency { get; set; }
        public string Description { get; set; }
        public int VerticalId { get; set; }
        public string VerticalName { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public bool IsFeaturedAd { get; set; }
        public bool IsPromoteAd { get; set; }
        public string DurationName { get; set; }
     
    }

    public class PayToFeatureBasicPriceRequestDto
    {
        public Vertical VerticalTypeId { get; set; }
        public SubscriptionCategory CategoryId { get; set; }
        public TimeSpan Duration { get; set; }
        public BasicPrice BasicPriceId { get; set; }
    }

    public class P2FBasicPriceResponseDto
    {
        public Guid Id { get; set; }
        public int VerticalTypeId { get; set; }
        public string VerticalTypeName { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public int BasicPriceId { get; set; }
        public string BasicPriceName { get; set; }
        public DateTime LastUpdated { get; set; }

    }

    public class PayToFeatureRequestDto
    {
        [Required]
        public string PlanName { get; set; } = string.Empty;
        [Required]
        public int TotalCount { get; set; }
        [Required]
        public string Description { get; set; } = string.Empty;
        [Required]
        public TimeSpan Duration { get; set; }
        public Vertical VerticalTypeId { get; set; }
        public SubscriptionCategory CategoryId { get; set; }


        public decimal Price { get; set; }
        public bool IsPromoteAd { get; set; }
        public bool IsFeatureAd { get; set; }
        [Required]
        public string Currency { get; set; } = string.Empty;
        [Required]
        public Status StatusId { get; set; }
    }
    public class PayToFeatureResponseDto
    {
        public Guid Id { get; set; }
        public string PlanName { get; set; } = string.Empty;

        public decimal Price { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DurationName { get; set; } = string.Empty;
        public bool IsPromoteAd { get; set; }
        public bool IsFeatureAd { get; set; }

    }




    public class PayToFeatureListResponseDto
    {
        public int VerticalId { get; set; }
        public string VerticalName { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public List<PayToPublishResponseDto> PayToPublish { get; set; } = new();
    }

    public class PayToFeaturePaymentDto
    {
        public Guid Id { get; set; }
        public Guid PayToFeatureId { get; set; }
        public int VerticalId { get; set; }

        public int CategoryId { get; set; }
        public string  UserId { get; set; }
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
    public class UserP2FPaymentDetailsResponseDto
    {
        public string UserId { get; set; }
        public Guid PaymentTransactionId { get; set; }
        public DateTime TransactionDate { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public Guid PaytoFeatureId { get; set; }
        public string PayToFeatureName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool IsPromoteAd { get; set; }
        public bool IsFeatureAd { get; set; }
        public Guid AddId { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int DurationId { get; set; }
        public string DurationName { get; set; } = string.Empty;
        public int VerticalTypeId { get; set; }
        public string VerticalName { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string CardHolderName { get; set; } = string.Empty;
    }
    public class PayToFeaturePaymentRequestDto
    {
        [Required]
        public int VerticalId { get; set; }

        [Required]
        public int CategoryId { get; set; }

        [Required]
        public Guid PayToFeatureId { get; set; }


        public CardDetailPaymentDto CardDetails { get; set; } = new();
    }

    public class PayToFeatureCardDetailPaymentDto
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



