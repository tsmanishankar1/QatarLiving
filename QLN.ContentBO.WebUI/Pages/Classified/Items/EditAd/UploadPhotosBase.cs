using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using QLN.ContentBO.WebUI.Models;
using Microsoft.JSInterop;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace QLN.ContentBO.WebUI.Pages.Classified.Items.EditAd
{
    public partial class UploadPhotosBase : ComponentBase
    {
        [Inject] private IJSRuntime JS { get; set; }
        [Inject] private ILogger<UploadPhotosBase> Logger { get; set; }

        [Parameter] public EditAdPost AdModel { get; set; } = new();

        protected const int MaxImages = 9;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await JS.InvokeVoidAsync("initSortable", DotNetObjectReference.Create(this));
            }
        }

         protected void AddImageBox()
        {
            if (AdModel.Images.Count < MaxImages)
            {
                AdModel.Images.Add(new AdImage { Order = AdModel.Images.Count });
            }
        }


        protected async Task OnMultipleFilesSelected(InputFileChangeEventArgs e, Guid targetImageId)
        {
            var startIndex = AdModel.Images.FindIndex(i => i.Id == targetImageId);
            if (startIndex == -1) return;

            var files = e.GetMultipleFiles(MaxImages - AdModel.Images.Count + 1);
            int insertIndex = startIndex;

            foreach (var file in files)
            {
                if (insertIndex >= MaxImages) break;

                var resized = await file.RequestImageFileAsync("image/png", 600, 600);
                using var stream = resized.OpenReadStream(10 * 1024 * 1024);
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms);
                var base64 = Convert.ToBase64String(ms.ToArray());
                var url = $"data:image/png;base64,{base64}";

                if (insertIndex < AdModel.Images.Count)
                {
                    AdModel.Images[insertIndex].Url = url;
                    AdModel.Images[insertIndex].AdImageFileName = file.Name;
                }
                else
                {
                    AdModel.Images.Add(new AdImage
                    {
                        Order = insertIndex,
                        Url = url,
                        AdImageFileName = file.Name
                    });
                }

                insertIndex++;
            }

            ReorderImages();
            StateHasChanged();
        }

        protected void RemoveImage(Guid id)
        {
            var item = AdModel.Images.FirstOrDefault(i => i.Id == id);
            if (item != null)
            {
                AdModel.Images.Remove(item);
                ReorderImages();
            }
        }

        private void ReorderImages()
        {
            for (int i = 0; i < AdModel.Images.Count; i++)
            {
                AdModel.Images[i].Order = i;
            }
        }

        [JSInvokable]
        public void UpdateOrder(string[] newOrder)
        {
            var reordered = new List<AdImage>();
            foreach (var idStr in newOrder)
            {
                if (Guid.TryParse(idStr, out Guid id))
                {
                    var image = AdModel.Images.FirstOrDefault(i => i.Id == id);
                    if (image != null)
                    {
                        image.Order = reordered.Count;
                        reordered.Add(image);
                        // Logger.LogInformation($"Image reordered: {image.AdImageFileName} to Order {image.Order}");
                    }
                }
            }

            AdModel.Images = reordered;
            StateHasChanged();
        }
    }
}
