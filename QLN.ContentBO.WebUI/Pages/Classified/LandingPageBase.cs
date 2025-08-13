using Microsoft.AspNetCore.Components;
using MudBlazor;
using QLN.ContentBO.WebUI.Components;
using QLN.ContentBO.WebUI.Components.ConfirmationDialog;
using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Pages.Classified.Modal;
using System.Text.Json;
using static QLN.ContentBO.WebUI.Models.ClassifiedLanding;

public class LandingPageBase : QLComponentBase
{
    protected int activeIndex = 0;
    protected bool IsLoading = true;
    protected string searchTerm = string.Empty;
    protected bool showItemModal = false;
    protected string modalTitle = string.Empty;

    protected List<LandingPageItem> currentItems = new();
    protected LandingPageItem currentItem = new();
    protected LandingPageItemType currentItemType;
    [Inject]
    public IDialogService DialogService { get; set; } = default!;
    [Inject]
    public IClassifiedService ClassifiedService { get; set; }

    private List<SeasonalPickDto> _seasonalPicks = new();
    private List<SeasonalPickDto> _featuredCategory = new();
    private List<SeasonalPickDto> _featuredStores = new();

    protected override async Task OnInitializedAsync()
    {
        try
        {
            IsLoading = true;

            if (!NavigationPath.Value.IsLocal)
            {
                await AuthorizedPage();
            }

            await LoadDataForCurrentTab();
            await LoadAllSeasonalPicks();
            await LoadAllFeaturedCategory();
            await LoadAllFeaturedStores();

        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "OnInitializedAsync");
            Snackbar.Add("Failed to load landing page data", Severity.Error);
        }
        finally
        {
            IsLoading = false;
            StateHasChanged();
        }
    }

    protected async Task OnTabChanged(int index)
    {
        activeIndex = index;
        currentItemType = (LandingPageItemType)index;
        await LoadDataForCurrentTab();
    }

    protected string GetCurrentTabAddButtonText()
    {
        return currentItemType switch
        {
            LandingPageItemType.FeaturedCategory => "Featured Category",
            LandingPageItemType.SeasonalPick => "Seasonal Pick",
            LandingPageItemType.FeaturedStore => "Featured Store",
            _ => "Item"
        };
    }


    protected async Task LoadDataForCurrentTab()
    {
        IsLoading = true;
        StateHasChanged();

        try
        {
            switch (currentItemType)
            {
                case LandingPageItemType.FeaturedCategory:
                    currentItems = await LoadFeaturedCategories();
                    break;
                case LandingPageItemType.SeasonalPick:
                    currentItems = await LoadSeasonalPicks();
                    break;
                case LandingPageItemType.FeaturedStore:
                    currentItems = await LoadFeaturedStores();
                    break;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Error loading {currentItemType}");
            Snackbar.Add($"Failed to load {currentItemType}", Severity.Error);
        }
        finally
        {
            IsLoading = false;
            StateHasChanged();
        }
    }

    private async Task<List<LandingPageItem>> LoadFeaturedCategories()
    {
        var picks = new List<LandingPageItem>();
        HttpResponseMessage? response = await ClassifiedService.GetFeaturedCategory(Vertical.Classifieds);

        if (response?.IsSuccessStatusCode == true)
        {
            var content = await response.Content.ReadAsStringAsync();
            var apiItems = JsonSerializer.Deserialize<List<SeasonalPickDto>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var realItemsBySlot = apiItems?
                .Where(x => x.SlotOrder >= 1 && x.SlotOrder <= 6)
                .ToDictionary(x => x.SlotOrder, x => new LandingPageItem
                {
                    Id = Guid.Parse(x.Id),
                    Category = x.CategoryName,
                    EndDate = x.EndDate,
                    SlotOrder = x.SlotOrder,
                    IsPlaceholder = false
                }) ?? new Dictionary<int, LandingPageItem>();

            for (int slot = 1; slot <= 6; slot++)
            {
                if (realItemsBySlot.ContainsKey(slot))
                {
                    picks.Add(realItemsBySlot[slot]);
                }
                else
                {
                    picks.Add(new LandingPageItem
                    {
                        Id = null,
                        Category = "Select a Featured Category",
                        EndDate = null,
                        SlotOrder = slot,
                        IsPlaceholder = true
                    });
                }
            }
        }

        return picks;
    }

    private async Task<List<LandingPageItem>> LoadSeasonalPicks()
    {
        var picks = new List<LandingPageItem>();
        HttpResponseMessage? response = await ClassifiedService.GetFeaturedSeasonalPicks(Vertical.Classifieds);

        if (response?.IsSuccessStatusCode == true)
        {
            var content = await response.Content.ReadAsStringAsync();
            var apiItems = JsonSerializer.Deserialize<List<SeasonalPickDto>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var realItemsBySlot = apiItems?
                .Where(x => x.SlotOrder >= 1 && x.SlotOrder <= 6)
                .ToDictionary(x => x.SlotOrder, x => new LandingPageItem
                {
                    Id = Guid.Parse(x.Id),
                    Category = x.CategoryName,
                    EndDate = x.EndDate,
                    SlotOrder = x.SlotOrder,
                    IsPlaceholder = false
                }) ?? new Dictionary<int, LandingPageItem>();

            for (int slot = 1; slot <= 6; slot++)
            {
                if (realItemsBySlot.ContainsKey(slot))
                {
                    picks.Add(realItemsBySlot[slot]);
                }
                else
                {
                    picks.Add(new LandingPageItem
                    {
                        Id = null,
                        Category = "Select a Seasonal Pick",
                        EndDate = null,
                        SlotOrder = slot,
                        IsPlaceholder = true
                    });
                }
            }
        }

        return picks;
    }


    private async Task<List<LandingPageItem>> LoadFeaturedStores()
    {
        var picks = new List<LandingPageItem>();
        HttpResponseMessage? response = await ClassifiedService.GetFeaturedStores(Vertical.Classifieds);

        if (response?.IsSuccessStatusCode == true)
        {
            var content = await response.Content.ReadAsStringAsync();
            var apiItems = JsonSerializer.Deserialize<List<SeasonalPickDto>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var realItemsBySlot = apiItems?
                .Where(x => x.SlotOrder >= 1 && x.SlotOrder <= 6)
                .ToDictionary(x => x.SlotOrder, x => new LandingPageItem
                {
                     Id = Guid.Parse(x.Id),
                    Category = x.StoreName,
                    EndDate = x.EndDate,
                    SlotOrder = x.SlotOrder,
                    IsPlaceholder = false
                }) ?? new Dictionary<int, LandingPageItem>();

            for (int slot = 1; slot <= 6; slot++)
            {
                if (realItemsBySlot.ContainsKey(slot))
                {
                    picks.Add(realItemsBySlot[slot]);
                }
                else
                {
                    picks.Add(new LandingPageItem
                    {
                        Id = null,
                        Category = "Select a store to feature",
                        EndDate = null,
                        SlotOrder = slot,
                        IsPlaceholder = true
                    });
                }
            }
        }

        return picks;
    }

    private async Task LoadAllSeasonalPicks()
    {
        var response = await ClassifiedService.GetAllSeasonalPicks(Vertical.Classifieds);

        if (response?.IsSuccessStatusCode == true)
        {
            var content = await response.Content.ReadAsStringAsync();
            _seasonalPicks = JsonSerializer.Deserialize<List<SeasonalPickDto>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<SeasonalPickDto>();
        }
        else
        {
            Snackbar.Add("Failed to load the seasonal pick items", Severity.Error);
        }
    }
    private async Task LoadAllFeaturedStores()
    {
        var response = await ClassifiedService.GetAllFeaturedStores(Vertical.Classifieds);

        if (response?.IsSuccessStatusCode == true)
        {
            var content = await response.Content.ReadAsStringAsync();
            _featuredStores = JsonSerializer.Deserialize<List<SeasonalPickDto>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<SeasonalPickDto>();
            foreach (var store in _featuredStores)
        {
            store.CategoryName = store.StoreName; 
        }
        }
        else
        {
            Snackbar.Add("Failed to load the seasonal pick items", Severity.Error);
        }
    }
    

    private async Task LoadAllFeaturedCategory()
    {
        var response = await ClassifiedService.GetAllFeatureCategory(Vertical.Classifieds);

        if (response?.IsSuccessStatusCode == true)
        {
            var content = await response.Content.ReadAsStringAsync();
            _featuredCategory = JsonSerializer.Deserialize<List<SeasonalPickDto>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<SeasonalPickDto>();
        }
        else
        {
            Snackbar.Add("Failed to load the seasonal pick items", Severity.Error);
        }
    }
    protected async Task SearchItems()
    {
        try
        {
            IsLoading = true;
            StateHasChanged();

            var allItems = currentItemType switch
            {
                LandingPageItemType.FeaturedCategory => await LoadFeaturedCategories(),
                LandingPageItemType.SeasonalPick => await LoadSeasonalPicks(),
                LandingPageItemType.FeaturedStore => await LoadFeaturedStores(),
                _ => new List<LandingPageItem>()
            };

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                currentItems = allItems.Where(item =>
                    item.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    item.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    item.Category.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            else
            {
                currentItems = allItems;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error searching items");
            Snackbar.Add("Search failed", Severity.Error);
        }
        finally
        {
            IsLoading = false;
            StateHasChanged();
        }
    }


    protected async Task NavigateToAddItem()
    {
        var title = $"Add {GetCurrentTabAddButtonText()}";

        var parameters = new DialogParameters
            {
                { nameof(MessageBoxBase.Title), title },
            };
        var options = new DialogOptions
        {
            CloseOnEscapeKey = true
        };
        IDialogReference dialog;
        if (currentItemType == LandingPageItemType.FeaturedCategory)
        {
            dialog = await DialogService.ShowAsync<AddFeaturedCategoryModal>("", parameters, options);
        }
        else if (currentItemType == LandingPageItemType.SeasonalPick)
        {
            dialog = await DialogService.ShowAsync<AddSeasonalPickModal>("", parameters, options);
        }
        else 
        {
            dialog = await DialogService.ShowAsync<AddStoreModal>("", parameters, options);
        }
        var result = await dialog.Result;
        if (!result.Canceled)
        {
            await LoadDataForCurrentTab();
            await LoadAllFeaturedCategory();
            await LoadAllSeasonalPicks();
            await LoadAllFeaturedStores();
            StateHasChanged();
        }
    }

    protected async Task ReplaceItem(LandingPageItem item)
    {
        var title = activeIndex switch
        {
            0 => "Replace Featured Category",
            1 => "Replace Seasonal Pick",
            2 => "Replace Featured Store",
            _ => "Replace Item"
        };
        var data  = activeIndex switch
        {
            0 => _featuredCategory,
            1 => _seasonalPicks,
            2 => _featuredStores,
            _ => _seasonalPicks
        };
        var parameters = new DialogParameters
    {
        { nameof(MessageBoxBase.Title), title },
        { nameof(MessageBoxBase.Placeholder), "Please type search item*" },
        { nameof(ReplaceDialogModal.events), data },
        { nameof(ReplaceDialogModal.SlotNumber), item.SlotOrder },
        { nameof(ReplaceDialogModal.ActiveIndex), activeIndex }

    };

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Small,
            FullWidth = true,
            CloseOnEscapeKey = true
        };

        //await DialogService.ShowAsync<ReplaceDialogModal>("", parameters, options);
        var dialog = await DialogService.ShowAsync<ReplaceDialogModal>("", parameters, options);
        var result = await dialog.Result;

        if (!result.Canceled)
        {
            await LoadDataForCurrentTab();
        }
    }

    protected Task OpenDialogAsync()
    {
        var parameters = new DialogParameters
            {
                { nameof(MessageBoxBase.Title), "Replace Seasonal  Pick" },
                { nameof(MessageBoxBase.Placeholder), "Plese type search item*" }
            };
        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Small,
            FullWidth = true,
            CloseOnEscapeKey = true
        };
        return DialogService.ShowAsync<ReplaceDialogModal>("", parameters, options);
    }

    protected async Task DeleteItem(string id)
    {
        var title = $"{GetCurrentTabAddButtonText()}";


        await ShowConfirmationDialog(id);
    }
   

    protected async Task ConfirmDeleteItem(string id)
    {
        try
        {
            string GetItemTypeName() => currentItemType switch
            {
                LandingPageItemType.FeaturedCategory => "Featured Category",
                LandingPageItemType.SeasonalPick => "Seasonal Pick",
                LandingPageItemType.FeaturedStore => "Featured Store",
                _ => "Item"
            };

            HttpResponseMessage? response = null;

            switch (currentItemType)
            {
                case LandingPageItemType.FeaturedCategory:
                    response = await ClassifiedService.DeleteFeaturedCategory(id, Vertical.Classifieds);
                    break;

                case LandingPageItemType.SeasonalPick:
                    response = await ClassifiedService.DeleteSeasonalPicks(id, Vertical.Classifieds);
                    break;

                case LandingPageItemType.FeaturedStore:
                    response = await ClassifiedService.DeleteFeaturedStores(id, Vertical.Classifieds);
                    break;

                default:
                    Snackbar.Add("Unknown item type selected.", Severity.Warning);
                    Logger.LogWarning("Unhandled item type in delete: {ItemType}", currentItemType);
                    return;
            }
            if (response?.IsSuccessStatusCode == true)
            {
                Snackbar.Add($"{GetItemTypeName()} deleted successfully", Severity.Success);
                await LoadDataForCurrentTab();
            }
            else
            {
                Snackbar.Add($"Failed to delete {GetItemTypeName()}", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Error deleting {currentItemType}");
            Snackbar.Add($"Error occurred while deleting {currentItemType}", Severity.Error);
        }
    }


    protected void CloseModal()
    {
        showItemModal = false;
        StateHasChanged();
    }

    protected async Task SaveItem()
    {
        try
        {
            IsLoading = true;
            StateHasChanged();

            await Task.Delay(500);

            Snackbar.Add($"{GetCurrentTabAddButtonText()} saved successfully", Severity.Success);
            showItemModal = false;
            await LoadDataForCurrentTab();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error saving item");
            Snackbar.Add("Failed to save item", Severity.Error);
        }
        finally
        {
            IsLoading = false;
            StateHasChanged();
        }
    }

    private async Task ShowConfirmationDialog(string id)
    {
        var title = $"{GetCurrentTabAddButtonText()}";

        var parameters = new DialogParameters
        {
            { "Title", "Delete Confirmation" },
            { "Descrption",$"Do you want to delete this {title}?" },
            { "ButtonTitle", "Delete" },
            { "OnConfirmed",  EventCallback.Factory.Create(this, async () => await ConfirmDeleteItem(id))}
        };
        var options = new DialogOptions { CloseButton = false, MaxWidth = MaxWidth.Small, FullWidth = true };
        var dialog = DialogService.Show<ConfirmationDialog>("", parameters, options);
        var result = await dialog.Result;
    }
    protected async Task DeleteFeatureEvent(string eventId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(eventId))
            {
                Snackbar.Add("Invalid event ID.", Severity.Warning);
                return;
            }
          
        }
        catch (Exception ex)
        {
            Snackbar.Add("Something went wrong while deleting the featured event.", Severity.Error);
        }
    }
}

public enum LandingPageItemType
{
    FeaturedCategory = 0,
    SeasonalPick = 1,
    FeaturedStore = 2
}

