using QLN.Common.Infrastructure.Subscriptions;
using System.ComponentModel.DataAnnotations;
using static QLN.Common.DTO_s.AddonDto;

namespace QLN.Common.DTO_s
{
    public class AddonDto
    {
        public class Quantities
        {
            [Key]
            public Guid QuantitiesId { get; set; }
            public int Quantity { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        public class Currency
        {
            [Key]
            public Guid CurrencyId { get; set; }
            public decimal CurrencyValue { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        public class UnitCurrency
        {
            [Key]
            public Guid Id { get; set; }
            public Guid QuantityId { get; set; }
            public Guid CurrencyId { get; set; }
            public string currency { get; set; }
            public DurationType Duration { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        public class AddonDataDto
        {
            public Guid Id { get; set; }
            public List<Quantities> Quantities { get; set; } = new List<Quantities>();
            public List<Currency> Currencies { get; set; } = new List<Currency>();
            public List<AddonDto.UnitCurrency> QuantitiesCurrencies { get; set; } = new();
            public Guid NextId { get; set; }
            public DateTime LastUpdated { get; set; }
        }
        public class CreateQuantityRequest
        {
            public int Quantity { get; set; }
        }
        public class CurrencyResponse
        {
            public Guid CurrencyId { get; set; }
            public decimal CurrencyValue { get; set; }
        }
        public class QuantityResponse
        {
            public Guid QuantitiesId { get; set; }
            public int Quantity { get; set; }
        }


        public class CreateCurrencyRequest
        {
            public decimal CurrencyValue { get; set; }
        }

        public class CreateUnitCurrencyRequest
        {
            public Guid QuantityId { get; set; }
            public Guid CurrencyId { get; set; }
            public string currency { get; set; }
            public DurationType durationId { get; set; }

        }
        public class UnitCurrencyResponse
        {
            public Guid Id { get; set; }
            public Guid QuantityId { get; set; }
            public int Quantity { get; set; }
            public Guid CurrencyId { get; set; }
            public decimal CurrencyValue { get; set; }
            public string Currency { get; set; }
            public int durationId { get; set; }
            public string durationName { get; set; }
        }
        public class AddonPaymentDto
        {
            public Guid Id { get; set; }
            public Guid AddonId { get; set; }
            public int VerticalId { get; set; }
            public string UserId { get; set; }
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
        public class AddonPaymentWithCurrencyDto
        {
           
            public Guid Id { get; set; }
            public Guid AddonId { get; set; }
            public Guid AddId { get; set; }
            public int AddUsage { get; set; }
            public int VerticalId { get; set; }
            public string CardNumber { get; set; } = default!;
            public string ExpiryMonth { get; set; } = default!;
            public string ExpiryYear { get; set; } = default!;
            public string Cvv { get; set; } = default!;
            public string CardHolderName { get; set; } = default!;

            public string UserId { get; set; } = default!;
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public DateTime LastUpdated { get; set; }
            public bool IsExpired { get; set; }
            public Guid UnitCurrencyId { get; set; }
            public Guid QuantityId { get; set; }
            public Guid CurrencyId { get; set; }
            public string Currency { get; set; }
            public int Quantity { get; set; } = default!;
            public decimal CurrencyValue { get; set; } = default!;
            public DurationType Duration { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        public class PaymentAddonRequestDto
        {
            [Required]
            public int VerticalId { get; set; }

            [Required]
            public Guid AddonId { get; set; }


            public CardDetailAddonPaymentDto CardDetails { get; set; } = new();
        }

        public class CardDetailAddonPaymentDto
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
}

