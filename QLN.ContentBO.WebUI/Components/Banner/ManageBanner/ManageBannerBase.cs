using MudBlazor;
using Microsoft.AspNetCore.Components.Forms;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Interfaces;
using Microsoft.AspNetCore.Components.Forms;
using QLN.ContentBO.WebUI.Models;
using Microsoft.AspNetCore.Components;
using System.Text.Json;
using System.Net;
using QLN.ContentBO.WebUI.Interfaces;
using System.Threading.Tasks;

namespace QLN.ContentBO.WebUI.Components.Banner
{
    public class ManageBannerBase : QLComponentBase
    {
        [Inject] protected NavigationManager Navigation { get; set; }
        public class DropDownItem
        {
            public string DisplayName { get; set; } = string.Empty;
            public int Value { get; set; }
        }
        public class StatusItem
        {
            public string DisplayName { get; set; } = string.Empty;
            public string Value { get; set; } = string.Empty;
        }
        [Inject] IBannerService bannerService { get; set; }
        protected string? SelectedStatus { get; set; }
        protected bool? isActive { get; set; }
        protected int SelectedVertical;
        public List<BannerType> bannerTypes { get; set; } = new();
        public List<BannerPageLocationDto> bannerPageTypes { get; set; } = new();

        protected List<StatusItem> StatusList = new()
        {
            new StatusItem { DisplayName = "All", Value = "All" },
            new StatusItem { DisplayName = "Active", Value = "Active" },
            new StatusItem { DisplayName = "InActive", Value = "InActive" }
        };
        protected override async Task OnInitializedAsync()
        {
            bannerTypes = await GetBannerTypes();
            bannerPageTypes = bannerTypes
                .Where(bt => bt.Pages != null)
                .SelectMany(bt => bt.Pages!)
                .ToList();
        }
        protected async Task OnStatusChanged(string value)
        {
            SelectedStatus = value;
            isActive = value switch
            {
                "Active" => true,
                "InActive" => false,
                "All" => null,
                _ => null
            };
            bannerTypes = await GetBannerTypes();
            bannerPageTypes = bannerTypes
                .Where(bt => bt.Pages != null)
                .SelectMany(bt => bt.Pages!)
                .ToList();
        }
        protected async Task OnVerticalChanged(int value)
        {
            SelectedVertical = value;
            bannerTypes = await GetBannerTypes();
            bannerPageTypes = bannerTypes
                .Where(bt => bt.Pages != null)
                .SelectMany(bt => bt.Pages!)
                .ToList();
        }


        protected List<DropDownItem> Verticals = Enum.GetValues(typeof(Vertical))
            .Cast<Vertical>()
            .Select(v => new DropDownItem
            {
                DisplayName = v.ToString(),
                Value = (int)v
            }).ToList();
        protected void ResetFilters()
        {
            SelectedVertical = 0;
            SelectedStatus = "All";
        }

        public class BannerLocationModel
        {
            public string Name { get; set; } = string.Empty;
            public List<BadgeModel> Tags { get; set; } = new();
            public int BannerCount { get; set; }
        }

        public class BadgeModel
        {
            public string Label { get; set; } = string.Empty;
            public Color Color { get; set; } = Color.Default;
        }

        private async Task<List<BannerType>> GetBannerTypes()
        {
            try
            {
                var selectedVertical = 5;
                var apiResponse = await bannerService.GetBannerByVerticalAndStatus(selectedVertical, isActive);
                if (apiResponse.IsSuccessStatusCode)
                {
                    var responseContent = await apiResponse.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    var bannerTypes = JsonSerializer.Deserialize<List<BannerType>>(responseContent, options) ?? [];
                    foreach (var bannerType in bannerTypes)
                    {
                        if (bannerType.Pages != null)
                        {
                            foreach (var page in bannerType.Pages)
                            {
                                page.VerticalId = (int)bannerType.VerticalId;
                                page.SubVerticalId = (int?)bannerType.SubVerticalId;
                            }
                        }
                    }

                    return bannerTypes;
                }
                else
                {
                    Snackbar.Add("Internal API Error");
                }

                return [];
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetBannerTypes");
                return [];
            }
        }
        protected async Task ReOrderBanners((List<string> NewOrder, int Vertical, int SubVertical, string PageId) args)
        {
            var (newOrder, vertical, subVertical, pageId) = args;

            try
            {
                var apiResponse = await bannerService.ReorderBanner(newOrder, vertical, subVertical, pageId);
                if (apiResponse.IsSuccessStatusCode)
                {
                    var responseContent = await apiResponse.Content.ReadAsStringAsync();
                    Snackbar.Add("Banner Reordered successfully", severity: Severity.Success);
                }
                else
                {
                    Snackbar.Add("Internal API Error");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetBannerTypes");
            }
        }
        protected void NavigateToCreateBanner(Guid bannerTypeId)
        {
            Navigation.NavigateTo($"/manage/banner/createbanner/{bannerTypeId}");
        }


    }
}
