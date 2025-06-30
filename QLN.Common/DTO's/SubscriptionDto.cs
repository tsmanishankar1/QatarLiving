
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Subscriptions;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace QLN.Common.DTOs
{
    public class SubscriptionDto
    {
        public Guid Id { get; set; }
        public string subscriptionName { get; set; }
        public TimeSpan Duration { get; set; }

        public decimal? price { get; set; }
        public string? description { get; set; }
        public string? currency { get; set; }
        public SubscriptionCategory CategoryId { get; set; }
        public int adsbudget { get; set; }
        public int promotebudget { get; set; }
        public int refreshbudget { get; set; }
        public Vertical VerticalTypeId { get; set; }
        public Status StatusId { get; set; }
        public DateTime lastUpdated { get; set; }
    }

    public class MyMessage
    {
        public Guid Id { get; set; }
        public string Content { get; set; }
    }
    public class PaymentCompletedMessage
    {
        public Guid UserId { get; set; }
        public Guid SubscriptionId { get; set; }
        public Guid PaymentTransactionId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime ProcessedAt { get; set; }
    }
    // Add this DTO to your DTOs namespace/folder

    public class UserPaymentDetailsResponseDto
    {
        // Payment Information
        public Guid PaymentTransactionId { get; set; }
        public DateTime TransactionDate { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public string CardHolderName { get; set; }

        // Subscription Information
        public Guid SubscriptionId { get; set; }
        public string SubscriptionName { get; set; }
        public decimal? Price { get; set; }
        public string Currency { get; set; }
        public string Description { get; set; }
        public int DurationId { get; set; }
        public string DurationName { get; set; }

        // Category and Vertical Information
        public int VerticalTypeId { get; set; }
        public string VerticalName { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }

   
        public decimal? AdsbudBudget { get; set; }
        public decimal? PromoteBudget { get; set; }
        public decimal? RefreshBudget { get; set; }


       
    }
    public class YearlySubscriptionResponseDto
    {
        public Guid UserId { get; set; }
        public bool IsRewardsYearlySubscription { get; set; }
        public decimal? Price { get; set; }
        public string Currency { get; set; } = string.Empty;
        public DateTime? EndDate { get; set; }
      
        public Guid? PaymentTransactionId { get; set; }
        public Guid? SubscriptionId { get; set; }
    }
    public class SubscriptionExpiryMessage
    {
        public Guid UserId { get; set; }
        public Guid SubscriptionId { get; set; }
        public Guid PaymentTransactionId { get; set; }
        public DateTime ExpiryDate { get; set; }
        public DateTime ProcessedAt { get; set; }
    }
    public class SubscriptionRequestDto
    {
        [Required]
        public string SubscriptionName { get; set; }
        [Required]
        public TimeSpan Duration{ get; set; }
        [Required]
        public decimal? Price { get; set; }
        [Required]
        public int adsbudget { get; set; }
        [Required]
        public int promotebudget { get; set; }
        [Required]
        public int refreshbudget { get; set; }
        [Required]

        public string? Description { get; set; }
        [Required]

        public string? Currency { get; set; }
        [Required]
        public SubscriptionCategory CategoryId { get; set; }
        [Required]
        public Status StatusId { get; set; }
        [Required]
        public Vertical VerticalTypeId { get; set; }
    }
    public class SubscriptionGroupResponseDto
    {
        public int VerticalTypeId { get; set; }
        public string VerticalName { get; set; }

        public int CategoryId { get; set; }
        public string CategoryName { get; set; }

        public List<SubscriptionResponseDto> Subscriptions { get; set; } = new();
    }


    public class SubscriptionResponseDto
    {
        public Guid Id { get; set; }
        public string? SubscriptionName { get; set; }
        public int DurationId { get; set; }
        public string DurationName { get; set; } = string.Empty;
        public decimal? Price { get; set; }
        public string? Description { get; set; }
        public string? Currency { get; set; }
    }


    public class PaymentTransactionRequestDto
    {
        [Required]
        public Guid SubscriptionId { get; set; }

        [Required]
        public int VerticalId { get; set; }

        [Required]
        public int CategoryId { get; set; }

        [Required]
        public CardDetailsDto CardDetails { get; set; } = new CardDetailsDto();
    }
    public class CardDetailsDto
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




    public class PaymentTransactionDto
    {
        public Guid Id { get; set; }
        public Guid SubscriptionId { get; set; }
        public int VerticalId { get; set; }
        public int CategoryId { get; set; }
        public Guid UserId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string CardNumber { get; set; } = string.Empty; 
        public string ExpiryMonth { get; set; } = string.Empty;
        public string ExpiryYear { get; set; } = string.Empty;
        public string CardHolderName { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; }
        public DateTime LastUpdated { get; set; }
        public bool IsExpired { get; set; } = false;
    }


}





