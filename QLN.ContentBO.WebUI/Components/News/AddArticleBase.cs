using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.ComponentModel.DataAnnotations;

namespace QLN.ContentBO.WebUI.Components.News
{
    public class AddArticleBase : ComponentBase
    {
        protected RegisterAccountForm model = new RegisterAccountForm();
        protected  bool success;

        public class RegisterAccountForm
        {
            [Required]
            [StringLength(8, ErrorMessage = "Name length can't be more than 8.")]
            public string Username { get; set; }

            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            [StringLength(30, ErrorMessage = "Password must be at least 8 characters long.", MinimumLength = 8)]
            public string Password { get; set; }

            [Required]
            [Compare(nameof(Password))]
            public string Password2 { get; set; }

        }

        protected void OnValidSubmit(EditContext context)
        {
            success = true;
            StateHasChanged();
        }
    }
}
