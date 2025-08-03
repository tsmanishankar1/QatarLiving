using Microsoft.AspNetCore.Components;
using MudBlazor;
using QLN.ContentBO.WebUI.Components;
using QLN.ContentBO.WebUI.Components.ConfirmationDialog;
using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Pages.Services.Modal;
using System.Text.Json;
using static QLN.ContentBO.WebUI.Models.ClassifiedLanding;

public class ServiceLandingPageBase : QLComponentBase
{
    protected int activeIndex = 0;
    protected bool IsLoading = true;
    protected string searchTerm = string.Empty;
    protected bool showItemModal = false;
    protected string modalTitle = string.Empty;

    protected List<LandingPageItem> currentItems = new();
    protected LandingPageItem currentItem = new();
    protected ServiceLandingPageItemType currentItemType;
    [Inject]
    public IDialogService DialogService { get; set; } = default!;
    [Inject]
    public IClassifiedService ClassifiedService { get; set; }

    private List<SeasonalPickDto> _seasonalPicks = new();
    private List<SeasonalPickDto> _featuredCategory = new();

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
        currentItemType = (ServiceLandingPageItemType)index;
        await LoadDataForCurrentTab();
    }

    protected string GetCurrentTabAddButtonText()
    {
        return currentItemType switch
        {
            ServiceLandingPageItemType.FeaturedCategory => "Featured Category",
            ServiceLandingPageItemType.SeasonalPick => "Seasonal Pick",
            ServiceLandingPageItemType.FeaturedStore => "Featured Store",
            _ => "Item"
        };
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
        var data = activeIndex switch
        {
            0 => _featuredCategory,
            1 => _seasonalPicks,
            _ => _seasonalPicks
        };

        var parameters = new DialogParameters
    {
        { nameof(MessageBoxBase.Title), title },
        { nameof(MessageBoxBase.Placeholder), "Please type search item*" },
        { nameof(ServicesReplaceDialogModal.events), data },
        { nameof(ServicesReplaceDialogModal.SlotNumber), item.SlotOrder },
        { nameof(ServicesReplaceDialogModal.ActiveIndex), activeIndex }
    };

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Small,
            FullWidth = true,
            CloseOnEscapeKey = true
        };

       var dialog = await DialogService.ShowAsync<ServicesReplaceDialogModal>("", parameters, options);
        var result = await dialog.Result;

        if (!result.Canceled)
        {
            await LoadDataForCurrentTab();
        }
    }


    protected async Task LoadDataForCurrentTab()
    {
        IsLoading = true;
        StateHasChanged();

        try
        {
            switch (currentItemType)
            {
                case ServiceLandingPageItemType.FeaturedCategory:
                    currentItems = await LoadFeaturedCategories();
                    break;
                case ServiceLandingPageItemType.SeasonalPick:
                    currentItems = await LoadSeasonalPicks();
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
        HttpResponseMessage? response = await ClassifiedService.GetFeaturedCategory("services");

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
        HttpResponseMessage? response = await ClassifiedService.GetFeaturedSeasonalPicks("services");

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
                        Id = Guid.NewGuid(),
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
        HttpResponseMessage? response = await ClassifiedService.GetFeaturedSeasonalPicks("services");

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
                        Id = Guid.NewGuid(),
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
        try
        {
            var response = await ClassifiedService.GetAllSeasonalPicks("services");

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
                Snackbar.Add("Failed to load the seasonal pick items ", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "LoadAllSeasonalPicks");
            Snackbar.Add("Search failed", Severity.Error);
        }
    }
    private async Task LoadAllFeaturedCategory()
    {
        try
        {
            var response = await ClassifiedService.GetAllFeatureCategory("services");

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
        catch (Exception ex)
        {
            Logger.LogError(ex, "LoadAllFeaturedCategory");
            Snackbar.Add("Search failed", Severity.Error);
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
                ServiceLandingPageItemType.FeaturedCategory => await LoadFeaturedCategories(),
                ServiceLandingPageItemType.SeasonalPick => await LoadSeasonalPicks(),
                ServiceLandingPageItemType.FeaturedStore => await LoadFeaturedStores(),
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
        if (currentItemType == ServiceLandingPageItemType.FeaturedCategory)
        {
            dialog = await DialogService.ShowAsync<ServicesAddFeaturedCategoryModal>("", parameters, options);
        }
        else if (currentItemType == ServiceLandingPageItemType.SeasonalPick)
        {
            dialog = await DialogService.ShowAsync<ServicesAddSeasonalPickModal>("", parameters, options);
        }
        else
        {
            return; 
        }
        var result = await dialog.Result;
        if (!result.Canceled)
        {
            await LoadDataForCurrentTab();
            await LoadAllFeaturedCategory();
            await LoadAllSeasonalPicks();
            StateHasChanged();
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
        return DialogService.ShowAsync<ServicesReplaceDialogModal>("", parameters, options);
    }

    protected async Task DeleteItem(string id)
    {
        var title = $"{GetCurrentTabAddButtonText()}";

        var parameters = new DialogParameters
        {
            { "Title", "Delete Confirmation" },
            { "Description", $"Do you want to delete this {title}?" },
            { "ButtonTitle", "Delete" },
            { "OnConfirmed", EventCallback.Factory.Create(this, async () => await ConfirmDeleteItem(id)) }
        };

        await ShowConfirmationDialog();
    }

    protected async Task ConfirmDeleteItem(string id)
    {
        try
        {
            await Task.Delay(500); 
            Snackbar.Add($"{currentItemType} deleted successfully", Severity.Success);
            await LoadDataForCurrentTab();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Error deleting {currentItemType}");
            Snackbar.Add($"Failed to delete {currentItemType}", Severity.Error);
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

    private async Task ShowConfirmationDialog()
    {
        var title = $"{GetCurrentTabAddButtonText()}";

        var parameters = new DialogParameters
        {
            { "Title", "Delete Confirmation" },
            { "Descrption",$"Do you want to delete this {title}?" },
            { "ButtonTitle", "Delete" },
            { "OnConfirmed",  EventCallback.Factory.Create(this, async () => await DeleteFeatureEvent("d5d89b0b-eaae-4853-90c3-238d4531bd1a"))}
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

public enum ServiceLandingPageItemType
{
    FeaturedCategory = 0,
    SeasonalPick = 1,
    FeaturedStore = 2
}

