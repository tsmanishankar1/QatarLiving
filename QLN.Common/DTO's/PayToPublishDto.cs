using QLN.Common.Infrastructure.Subscriptions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public enum DurationType
    {
        ThreeMonths = 1,
        SixMonths = 2,
        OneYear = 3,
        TwoMinutes=4,
        OneWeek=5,
        OneMonth=6,
    }
    public enum BasicPrice
    {
       
        BasicPrice_200 = 200,
        BasicPrice_50 = 50,
        BasicPrice_29 = 29,

    }
    public class PayToPublishDto
    {
        public Guid Id { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; } // Updated
        public decimal Price { get; set; }
        public int TotalCount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public Vertical VerticalTypeId { get; set; }
        public SubscriptionCategory CategoryId { get; set; }
        public bool IsFreeAd { get; set; }
        public bool IsPromoteAd { get; set; }
        public bool IsFeatureAd { get; set; }

        public Status StatusId { get; set; }
        public DateTime LastUpdated { get; set; }

    }
    public class PayToPublishDataDto
    {
        public Guid Id { get; set; }
        public DateTime LastUpdated { get; set; }
        public List<PayToPublishDto>? Plans { get; set; }
        public List<BasicPriceDto>? BasicPrices { get; set; }
    }
    public class BasicPriceDto
    {
        public Guid Id { get; set; }
        public Vertical VerticalTypeId { get; set; }
        public SubscriptionCategory CategoryId { get; set; }
        public BasicPrice BasicPriceId { get; set; }
        public TimeSpan Duration { get; set; }
        public DateTime LastUpdated { get; set; }

    }
    public class PayToPublishPlansResponse
    {
        public int? BasicPriceId { get; set; }
        public string Duration { get; set; }
        public List<PayToPublishWithBasicPriceResponseDto> PlanDetails { get; set; } = new();
    }
    public class PayToPublishWithBasicPriceResponseDto
    {
        public Guid Id { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DurationName { get; set; } = string.Empty;
        public bool IsFreeAd { get; set; } = false;
        public bool IsPromoteAd { get; set; }
        public bool IsFeatureAd { get; set; }
        public int VerticalId { get; set; }
        public string VerticalName { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int? BasicPriceId { get; set; }
      
    }
    public class BasicPriceRequestDto
    {
        public Vertical VerticalTypeId { get; set; }
        public SubscriptionCategory CategoryId { get; set; }
        public TimeSpan Duration { get; set; }
        public BasicPrice BasicPriceId { get; set; }
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
        public TimeSpan Duration { get; set; }

   
        [Required]
        public decimal Price { get; set; }
        [Required]
        public string Currency { get; set; } = string.Empty;
        [Required]
        public Vertical VerticalTypeId { get; set; }

        public SubscriptionCategory CategoryId { get; set; }
        public bool IsFreeAd { get; set; }
        public bool IsPromoteAd { get; set; }
        public bool IsFeatureAd { get; set; }
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
        public string DurationName { get; set; } = string.Empty;
        public bool IsFreeAd { get; set; } = false;
        public bool IsPromoteAd { get; set; }
        public bool IsFeatureAd { get; set; }
    }




    public class PayToPublishListResponseDto
    {
        public int VerticalId { get; set; }
        public string VerticalName { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public List<PayToPublishResponseDto> PayToPublish { get; set; } = new();
    }
    public class UserP2PPaymentDetailsResponseDto
    {
        public string  UserId { get; set; }
        public Guid PaymentTransactionId { get; set; }
        public DateTime TransactionDate { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public Guid PaytoPublishId { get; set; }
        public string PayToPublishName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool IsPromoteAd { get; set; }
        public bool IsFeatureAd { get; set; }
        public bool IsAdFree { get; set; }
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
    public class PaymentDto
    {
        public Guid Id { get; set; }
        public Guid PayToPublishId { get; set; }
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



