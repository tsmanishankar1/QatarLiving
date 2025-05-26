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
        public int VerticalTypeId { get; set; }
        public int CategoryId { get; set; }
        public int StatusId { get; set; }

        public DateTime LastUpdated { get; set; }
    }
    public class PayToPublishRequestDto
    {

        public string PlanName { get; set; } = string.Empty;
        public int TotalCount { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Duration { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Currency { get; set; } = string.Empty;
        public int VerticalTypeId { get; set; }
        public int CategoryId { get; set; }
        public int StatusId { get; set; }
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
    public class PaymentDto
    {
        public Guid Id { get; set; }
        public Guid PayToPublishId { get; set; }
        public int VerticalId { get; set; }
        public int CategoryId { get; set; }
        public Guid UserId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string CardNumber { get; set; } = string.Empty; // In real implementation, this should be encrypted/tokenized
        public string ExpiryMonth { get; set; } = string.Empty;
        public string ExpiryYear { get; set; } = string.Empty;
        public string Cvv { get; set; } = string.Empty; // In real implementation, this should never be stored
        public string CardHolderName { get; set; } = string.Empty;
     
        public DateTime LastUpdated { get; set; }
        
    }
    public class PaymentRequestDto
    {
   
        [Required]
        public int VerticalId { get; set; }

        [Required]
        public int CategoryId { get; set; }

        [Required]
        [StringLength(19, MinimumLength = 13)]
        public string CardNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(2, MinimumLength = 2)]
        public string ExpiryMonth { get; set; } = string.Empty;

        [Required]
        [StringLength(4, MinimumLength = 4)]
        public string ExpiryYear { get; set; } = string.Empty;

        [Required]
        [StringLength(4, MinimumLength = 3)]
        public string Cvv { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 2)]
        public string CardHolderName { get; set; } = string.Empty;

        [Required]
        public Guid PayToPublishId { get; set; }
    }
}



