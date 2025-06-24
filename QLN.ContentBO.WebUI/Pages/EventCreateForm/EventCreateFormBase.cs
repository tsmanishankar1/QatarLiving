using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudExRichTextEditor;
using QLN.Common.Infrastructure.DTO_s;
using MudBlazor;
using QLN.ContentBO.WebUI.Pages.EventCreateForm.MessageBox;
namespace QLN.ContentBO.WebUI.Pages
{
    public class EventCreateFormBase : ComponentBase
    {
        [Inject]
        public IDialogService DialogService { get; set; }
        protected string? uploadedImage;
        protected MudExRichTextEdit Editor;
        protected string Category;
        public double EventLat { get; set; } = 48.8584;
        public double EventLong { get; set; } = 2.2945;
        protected List<string> Categories = new()
        {
            "Sports",
            "Music",
            "Education"
        };
        protected string EventTitle;
        protected string AccessType = "Free Access";
        protected string LocationType = "Location";
        protected string Price;
        protected string Location;
        protected string RedirectionLink;
        protected string Venue;
        protected DateTime? EventDate;
        protected TimeSpan? EventTime;
        protected string ArticleContent;
        protected Task OpenDialogAsync()
        {
            var options = new DialogOptions
            {
                MaxWidth = MaxWidth.Small,
                FullWidth = true,
                CloseOnEscapeKey = true
            };
            return DialogService.ShowAsync<MessageBox>(string.Empty, options);
        }
        protected async Task UploadFiles(IBrowserFile file)
        {
            if (file is not null)
            {
                var buffer = new byte[file.Size];
                await file.OpenReadStream().ReadAsync(buffer);
                var base64 = Convert.ToBase64String(buffer);
                uploadedImage = $"data:{file.ContentType};base64,{base64}";
            }
        }
        protected void EditImage()
        {
            uploadedImage = null;
        }

        protected void DeleteImage()
        {
            uploadedImage = null;
        }
        private Task EventAdded(string value)
        {
            return Task.CompletedTask;
        }
    };
    

    
}
