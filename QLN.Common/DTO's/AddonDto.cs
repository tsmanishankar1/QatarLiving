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
            public string QuantitiesName { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        public class Currency
        {
            [Key]
            public Guid CurrencyId { get; set; }
            public string CurrencyName { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        public class UnitCurrency
        {
            [Key]
            public Guid Id { get; set; }
            public Guid QuantityId { get; set; }
            public Guid CurrencyId { get; set; }
            public DateTime CreatedAt { get; set; }
        }
    }
    public class AddonDataDto
    {
        public Guid Id { get; set; }
        public List<Quantities> Quantities { get; set; } = new List<Quantities>();
        public List<Currency> Currencies { get; set; } = new List<Currency>();
        public List<UnitCurrency> QuantitiesCurrencies { get; set; } = new List<UnitCurrency>();
        public Guid NextId { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    // Request DTOs
    public class CreateQuantityRequest
    {
        public string QuantitiesName { get; set; }
    }
    public class CurrencyResponse
    {
        public Guid CurrencyId { get; set; }
        public string CurrencyName { get; set; }
    }
    public class QuantityResponse
    {
        public Guid QuantitiesId { get; set; }
        public string QuantitiesName { get; set; }
    }


    public class CreateCurrencyRequest
    {
        public string CurrencyName { get; set; }
    }

    public class CreateUnitCurrencyRequest
    {
        public Guid QuantityId { get; set; }
        public Guid CurrencyId { get; set; }
    }
    public class UnitCurrencyResponse
    {
        public Guid Id { get; set; }
        public Guid QuantityId { get; set; }
        public string QuantityName { get; set; }
        public Guid CurrencyId { get; set; }
        public string CurrencyName { get; set; }
    }


}