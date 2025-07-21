using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using System.Web;
using QLN.ContentBO.WebUI.Models;

namespace QLN.ContentBO.WebUI.Pages.Classified.Items.EditAd
{
    public class EditDealsBase : ComponentBase
    {
        [Inject] public NavigationManager Navigation { get; set; }
        protected EditAdPost adPostModel { get; set; } = new();
        [Parameter]
        public int AdId { get; set; }

        protected void GoBack()
        {
            Navigation.NavigateTo("/manage/classified/deals");
        }
        
    }
}
