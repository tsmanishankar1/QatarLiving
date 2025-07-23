using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using QLN.ContentBO.WebUI.Models;
using MudExRichTextEditor;
using Microsoft.JSInterop;
using MudBlazor;

namespace QLN.ContentBO.WebUI.Pages.Classified.Stores.EditCompnay
{
    public class EditCompnayBase : ComponentBase
    {
        [Inject] public NavigationManager Navigation { get; set; }
        protected EditCompany Company { get; set; } = new();

        [Parameter]
        public string? CompanyName { get; set; }

        protected void GoBack()
        {
            Navigation.NavigateTo("/manage/classified/stores");
        }
    }
}
