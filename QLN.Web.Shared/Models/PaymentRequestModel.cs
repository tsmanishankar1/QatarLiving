using System.ComponentModel.DataAnnotations;

namespace QLN.Web.Shared.Models
{
    public class PaymentRequestModel
    {
        [Required(ErrorMessage = "Card Number is required")]
        [CreditCard(ErrorMessage = "Invalid Card Number")]
        public string CardNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Expiry is required")]
        [RegularExpression(@"^(0[1-9]|1[0-2])\/\d{2}$", ErrorMessage = "Invalid Expiry format. Use MM/YY")]
        public string Expiry { get; set; } = string.Empty;

        public string ExpiryMonth => Expiry?.Length >= 2 ? Expiry.Substring(0, 2) : "";
        public string ExpiryYear => Expiry?.Length >= 5 ? Expiry.Substring(3, 2) : "";

        [Required(ErrorMessage = "CVV is required")]
        [RegularExpression(@"^\d{3,4}$", ErrorMessage = "Invalid CVV")]
        public string CVV { get; set; } = string.Empty;

        [Required(ErrorMessage = "Card Holder Name is required")]
        public string CardHolderName { get; set; } = string.Empty;
    }
}