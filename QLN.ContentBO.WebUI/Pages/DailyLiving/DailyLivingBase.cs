using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudExRichTextEditor;
using QLN.Common.Infrastructure.DTO_s;
using MudBlazor;
using QLN.ContentBO.WebUI.Pages.DailyLiving.Components.RadioAutoCompleteDialog;
namespace QLN.ContentBO.WebUI.Pages
{
    public class DailyLivingBase : ComponentBase
    {
        protected int activeIndex = 0;
        [Inject]
        public IDialogService DialogService { get; set; }
        protected Task OpenDialogAsync()
        {
            var options = new DialogOptions
            {
                MaxWidth = MaxWidth.Small,
                FullWidth = true,
                CloseOnEscapeKey = true
            };
            return DialogService.ShowAsync<RadioAutoCompleteDialog>(string.Empty, options);
        }

    }
}