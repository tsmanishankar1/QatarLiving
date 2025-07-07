using Grpc.Core;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using MudExRichTextEditor;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.DTO_s;
using QLN.ContentBO.WebUI.Components;
using QLN.ContentBO.WebUI.Components.News;
using QLN.ContentBO.WebUI.Pages.DailyLiving.Components.RadioAutoCompleteDialog;
namespace QLN.ContentBO.WebUI.Pages
{
    public class DailyLivingBase : QLComponentBase
    {
        protected int activeIndex = 0;
        [Inject]
        public IDialogService DialogService { get; set; }

        [Inject] ILogger<DailyLivingBase> Logger { get; set; }

        protected override async Task OnInitializedAsync()
        {
            try
            {
                AuthorizedPage();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "OnInitializedAsync");
                throw;
            }
        }
       
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