using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using MudBlazor;
using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Components;
using QLN.ContentBO.WebUI.Pages.Services.Modal;
using static QLN.ContentBO.WebUI.Models.ClassifiedLanding;

namespace QLN.ContentBO.WebUI.Pages.Services.LandingPage
{
    public class ServicesLandingPageComponentBase : QLComponentBase
    {
        [Parameter]
        public bool IsLoading { get; set; }
        [Parameter]
        public List<LandingPageItem> Items { get; set; } = new();
        [Parameter]
        public ServiceLandingPageItemType ItemType { get; set; }
         [Inject]
        public IDialogService DialogService { get; set; } = default!;
        [Parameter]
        public EventCallback<LandingPageItem> ReplaceItem { get; set; }
         [Inject]
        public IClassifiedService ClassifiedService { get; set; }
        [Parameter]
        public EventCallback<string> OnDelete { get; set; }
        [Inject] public ISnackbar Snackbar { get; set; }
        [Inject] protected IJSRuntime JS { get; set; }
        [Inject] protected IEventsService EventsService { get; set; }
        [Inject] protected ILogger<ServicesLandingPageComponentBase> Logger { get; set; }
        private string UserId => CurrentUserId.ToString();
        private bool shouldInitializeSortable = false;
        private List<Slot> featuredEventSlots = new();
        protected override async Task OnInitializedAsync()
        {
            await AuthorizedPage();
            await base.OnInitializedAsync();

            if (Items != null && Items.Any())
            {
                featuredEventSlots = Items.Select((item, index) => new Slot
                {
                    SlotNumber = index + 1,
                    Event = item
                }).ToList();
            }
        }
        protected override async Task OnParametersSetAsync()
        {
            await base.OnParametersSetAsync();

            if (Items != null && Items.Any())
            {
                shouldInitializeSortable = true;
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (shouldInitializeSortable)
            {
                await JS.InvokeVoidAsync("initializeSortable", ".classified-table", DotNetObjectReference.Create(this));
                shouldInitializeSortable = false;
            }
        }
        protected string GetCurrentTabAddButtonText()
    {
        return ItemType switch
        {
            ServiceLandingPageItemType.FeaturedCategory => "Featured Category",
            ServiceLandingPageItemType.SeasonalPick => "Seasonal Pick",
            ServiceLandingPageItemType.FeaturedStore => "Featured Store",
            _ => "Item"
        };
    }

        protected async Task NavigateToAddItem()
        {
            var title = $"Edit {GetCurrentTabAddButtonText()}";
            var options = new DialogOptions
            {
                CloseOnEscapeKey = true
            };
            IDialogReference dialog;
            if (ItemType == ServiceLandingPageItemType.FeaturedCategory)
            {
                 var parameters = new DialogParameters
                {
                    { nameof(ServicesEditFeaturedCategoryModalBase.Title), title },
                    // { nameof(ServicesEditFeaturedCategoryModalBase.CategoryId), title },
                };
                dialog = await DialogService.ShowAsync<ServicesEditFeaturedCategoryModel>("", parameters, options);
            }
            else if (ItemType == ServiceLandingPageItemType.SeasonalPick)
            {
                  var parameters = new DialogParameters
                {
                    { nameof(ServicesEditSeasonPickModalBase.Title), title },
                    // { nameof(ServicesEditSeasonPickModalBase.CategoryId), title },
                };
                dialog = await DialogService.ShowAsync<ServicesEditSeasonalPickModel>("", parameters, options);
            }
            else
            {
                return;
            }
            var result = await dialog.Result;
            // if (!result.Canceled)
            // {
            //     await LoadDataForCurrentTab();
            //     await LoadAllFeaturedCategory();
            //     await LoadAllSeasonalPicks();
            //     StateHasChanged();
            // }
        }


        [JSInvokable]
        public async Task OnTableReordered(List<string> newOrder)
        {
            try
            {
                var options = new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true };
                var dialog = await DialogService.ShowAsync<ReOrderConfirmDialog>("", options);
                var result = await dialog.Result;

                if (result is not null)
                {
                    if (result.Canceled)
                    {
                        await ResetOrder(); 
                        return;
                    }
                    var newSlotOrder = newOrder.Select(int.Parse).ToList();
                    var pickMap = Items
                        .Where(item => item != null)
                        .ToDictionary(item => item.SlotOrder, item => item.Id);
                    List<object> slotAssignments;
                    switch (ItemType)
                    {
                        case ServiceLandingPageItemType.FeaturedCategory:
                            slotAssignments = newSlotOrder.Select((originalSlotNumber, newIndex) => new
                            {
                                slotOrder = newIndex + 1,
                                categoryId = pickMap.TryGetValue(originalSlotNumber, out var id) && id != Guid.Empty ? (Guid?)id : null
                            }).Cast<object>().ToList();
                            break;

                        case ServiceLandingPageItemType.SeasonalPick:
                         slotAssignments = newSlotOrder.Select((originalSlotNumber, newIndex) => new
                            {
                                slotOrder = newIndex + 1,
                                categoryId = pickMap.TryGetValue(originalSlotNumber, out var id) && id != Guid.Empty ? (Guid?)id : null
                            }).Cast<object>().ToList();
                            break;

                        default:
                            Snackbar.Add("Unknown item type for reordering.", Severity.Warning);
                            Logger.LogWarning("Unhandled ItemType in reorder: {ItemType}", ItemType);
                            return;
                    }
                    HttpResponseMessage? response = null;
                    switch (ItemType)
                    {
                        case ServiceLandingPageItemType.FeaturedCategory:
                            response = await ClassifiedService.ReorderFeaturedCategoryAsync(slotAssignments, "services");
                            break;

                        case ServiceLandingPageItemType.SeasonalPick:
                            response = await ClassifiedService.ReorderSeasonalPicksAsync(slotAssignments, "services");
                            break;
                        default:
                            Snackbar.Add("Unknown item type for reordering.", Severity.Warning);
                            Logger.LogWarning("Unhandled ItemType in reorder: {ItemType}", ItemType);
                            return;
                    }
                    if (response?.IsSuccessStatusCode == true)
                    {
                        Snackbar.Add("Items reordered successfully here.", Severity.Success);
                    }
                    else
                    {
                        Snackbar.Add("Failed to reorder items.", Severity.Error);
                        Logger.LogError("Reorder API failed: {StatusCode}", response?.StatusCode);
                    }
                    StateHasChanged();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error during table reordering");
                Snackbar.Add("Failed to reorder items", Severity.Error);
            }
        }
        protected async Task ResetOrder()
        {
            try
            {
                await JS.InvokeVoidAsync("resetTableOrder");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "ResetOrder");
            }
        }
        protected string GetTableTitle()
        {
            return ItemType switch
            {
                ServiceLandingPageItemType.FeaturedCategory => "Featured Categories",
                ServiceLandingPageItemType.SeasonalPick => "Seasonal Picks",
                ServiceLandingPageItemType.FeaturedStore => "Featured Stores",
                _ => "Items"
            };
        }

        protected string GetItemTypeName()
        {
            return ItemType switch
            {
                ServiceLandingPageItemType.FeaturedCategory => "Featured Category",
                ServiceLandingPageItemType.SeasonalPick => "Seasonal Pick",
                ServiceLandingPageItemType.FeaturedStore => "Featured Store",
                _ => "Item"
            };
        }
        protected string EmptyCardTitle => $"No {GetItemTypeName()} found";

    }
}