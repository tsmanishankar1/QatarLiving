
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace QLN.Common.DTOs
{
    public class SubscriptionDto
    {
        public Guid Id { get; set; }
        public string subscriptionName { get; set; }
        public string duration { get; set; }
        public decimal? price { get; set; }
        public string? description { get; set; }
        public decimal? currency { get; set; }


        public int categoryId { get; set; }
        public int adsbudget { get; set; }
        public int promotebudget { get; set; }
        public int refreshbudget { get; set; }
        public int verticalTypeId { get; set; }

        public int statusId { get; set; }
        public DateTime lastUpdated { get; set; }
    }
    public class SubscriptionRequestDto
    {
        [Required]
        public string SubscriptionName { get; set; }

        public string Duration { get; set; }

        public decimal? Price { get; set; }
        public int adsbudget { get; set; }
        public int promotebudget { get; set; }
        public int refreshbudget { get; set; }

        public string? Description { get; set; }

        public decimal? Currency { get; set; }
        public int CategoryId { get; set; }

        public int StatusId { get; set; }

        public int VerticalTypeId { get; set; }
    }


    public class SubscriptionResponseDto
    {
        public Guid Id { get; set; }

        public string SubscriptionName { get; set; }

        public string Duration { get; set; }

        public decimal? Price { get; set; }

        public string? Description { get; set; }

        public decimal? Currency { get; set; }

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
        [StringLength(19, MinimumLength = 13)]
        public string CardNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(2, MinimumLength = 1)]
        public string ExpiryMonth { get; set; } = string.Empty;

        [Required]
        [StringLength(4, MinimumLength = 2)]
        public string ExpiryYear { get; set; } = string.Empty;

        [Required]
        [StringLength(4, MinimumLength = 3)]
        public string Cvv { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 2)]
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
    }


}
   



/*[JsonConverter(typeof(JsonStringEnumConverter))]
    public enum Category
    {
        Deals = 0,
        Stores = 1,
        Preloved = 2
    }*/



