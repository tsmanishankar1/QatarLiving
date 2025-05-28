using System.ComponentModel.DataAnnotations;

namespace QLN.Web.Shared.Model

{
    public class NewsLetterSubscriptionModel
    {
        
            [Required(ErrorMessage = "Email is required.")]
            [EmailAddress(ErrorMessage = "Invalid email address.")]
            public string Email { get; set; }
        
    }
}
