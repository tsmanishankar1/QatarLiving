using Microsoft.AspNetCore.Components;
using QLN.ContentBO.WebUI.Components;
using QLN.ContentBO.WebUI.Models;

namespace QLN.ContentBO.WebUI.Pages.Classified.Stores.EditCompnay
{
    public class EditCompanyBase : QLComponentBase
    {
        [Inject] public NavigationManager Navigation { get; set; }
        protected CompanyProfileItem Company { get; set; } = new();

        [Parameter]
        public string? CompanyName { get; set; }

        protected void GoBack()
        {
            NavManager.NavigateTo("/manage/classified/stores");
        }
    }
}
