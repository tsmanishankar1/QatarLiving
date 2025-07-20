using Microsoft.AspNetCore.Components;
using QLN.ContentBO.WebUI.Models;
using Microsoft.AspNetCore.WebUtilities;
using System.Web;

namespace QLN.ContentBO.WebUI.Pages.Classified.Items.CreateAd
{
    public class CreateAdBase : ComponentBase
    {
        [Inject] public NavigationManager Navigation { get; set; }

        protected void GoBack()
        {
            Navigation.NavigateTo("/manage/classified/items");
        }
        protected AdPost adPostModel { get; set; } = new();

        protected string? UserEmail { get; set; }

        protected override void OnInitialized()
        {
            var uri = Navigation.ToAbsoluteUri(Navigation.Uri);

            if (QueryHelpers.ParseQuery(uri.Query).TryGetValue("email", out var email))
            {
                UserEmail = email;
            }
        }
        
    }
}
