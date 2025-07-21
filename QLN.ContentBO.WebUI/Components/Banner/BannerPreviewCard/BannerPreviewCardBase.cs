using MudBlazor;
using Microsoft.AspNetCore.Components.Forms;
using QLN.ContentBO.WebUI.Models;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;
using QLN.ContentBO.WebUI.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using QLN.ContentBO.WebUI.Interfaces;
using Azure.Storage.Blobs.Models;


namespace QLN.ContentBO.WebUI.Components.Banner.BannerPreviewCard
{
    public class BannerPreviewCardBase : QLComponentBase
    {
        [Inject] protected IJSRuntime JS { get; set; }
        [Inject] protected NavigationManager Navigation { get; set; }
        [Inject] protected IDialogService DialogService { get; set; }
        [Parameter]
        public int verticalId { get; set; }
        protected List<string> updatedOrder = new();
        protected bool isReordering = false;
        [Parameter]
        public int subVerticalId { get; set; }
        [Parameter]
        public string pageId { get; set; }
        [Parameter]
        public List<BannerDTO> BannerDetailsList { get; set; } = [];
        [Parameter]
        public EventCallback<(List<string> NewOrder, int Vertical, int SubVertical, string PageId)> OnReorderCallBack { get; set; }
        public class BannerModel
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Locale { get; set; }
            public bool IsActive { get; set; }
            public string DesktopImageUrl { get; set; }
            public string MobileImageUrl { get; set; }
        }
        private bool shouldInitializeSortable = false;
        protected List<BannerModel> Banners = new()
    {
        new BannerModel {
            Id = "1",
            Name = "qlv_demo_banner",
            Locale = "qlv_detail_argentine",
            IsActive = true,
            DesktopImageUrl = "/qln-images/sample_banner.svg",
            MobileImageUrl = "/qln-images/sample_banner.svg"
        },
        new BannerModel {
            Id = "2",
            Name = "qlv_demo_banner_2",
            Locale = "qlv_detail_mexico",
            IsActive = false,
            DesktopImageUrl = "/qln-images/sample_banner.svg",
            MobileImageUrl = "/qln-images/sample_banner.svg"
        }
    };
        protected override async Task OnParametersSetAsync()
        {
            await base.OnParametersSetAsync();
            if (Banners != null && Banners.Any(s => s != null))
            {
                shouldInitializeSortable = true;
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (shouldInitializeSortable)
            {
                await JS.InvokeVoidAsync("initializeSortable", ".featured-table", DotNetObjectReference.Create(this));
                shouldInitializeSortable = false;
            }
        }

        protected void OnReorder(int oldIndex, int newIndex)
        {
            if (oldIndex == newIndex) return;

            var item = Banners[oldIndex];
            Banners.RemoveAt(oldIndex);
            Banners.Insert(newIndex, item);
        }
        [JSInvokable]
        public async Task OnTableReordered(List<string> newOrder)
        {
           updatedOrder = newOrder; 
        }
        protected void ToggleReorder()
        {
            isReordering = !isReordering;
            if (isReordering) { 
                shouldInitializeSortable = true;
            }
            StateHasChanged();
        }

        protected void NavigateToEditBanner(Guid id)
        {
            Navigation.NavigateTo($"/manage/banner/editbanner/{id}");
        }
        protected async Task SaveReorder()
        {
            var options = new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true };
            var dialog = await DialogService.ShowAsync<ReOrderConfirmDialog>("", options);
            var result = await dialog.Result;

            if (result is not null && !result.Canceled)
            {
                isReordering = false;

                if (OnReorderCallBack.HasDelegate && updatedOrder.Any())
                {
                    await OnReorderCallBack.InvokeAsync((updatedOrder, verticalId, subVerticalId, pageId));
                }
            }
            else
            {
                isReordering = false;
                await ResetOrder(); 
            }
            shouldInitializeSortable = true;
            StateHasChanged();
        }

        protected async Task ResetOrder()
        {
            try
            {
                isReordering = false;
                StateHasChanged();
                await JS.InvokeVoidAsync("resetTableOrder");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "ResetOrder");
            }
        }
    
    }
}









