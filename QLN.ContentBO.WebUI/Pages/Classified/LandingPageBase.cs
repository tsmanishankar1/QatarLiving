using Microsoft.AspNetCore.Components;
using MudBlazor;
using QLN.ContentBO.WebUI.Components;
using QLN.ContentBO.WebUI.Components.ConfirmationDialog;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Pages.Classified.Modal;

using static QLN.ContentBO.WebUI.Models.ClassifiedLanding;

public class LandingPageBase : QLComponentBase
{
    protected int activeIndex = 0;
    protected bool IsLoading = true;
    protected string searchTerm = string.Empty;
    protected bool showItemModal = false;
    protected string modalTitle = string.Empty;

    // Current data based on tab
    protected List<LandingPageItem> currentItems = new();
    protected LandingPageItem currentItem = new();
    protected LandingPageItemType currentItemType;
    [Inject]
    public IDialogService DialogService { get; set; } = default!;

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
        await Task.Delay(500);
        return new List<LandingPageItem>
        {
            new LandingPageItem
            {
                Id = Guid.Parse("d5d89b0b-eaae-4853-90c3-238d4531bd1a"),
                Category = "Electronics",
                EndDate = DateTime.Now.AddDays(30),            },
            new LandingPageItem
            {
                Id = Guid.Parse("d5d89b0b-eaae-4853-90c3-238d4531bd1a"),
                Category = "Fashion",
                EndDate = DateTime.Now.AddDays(60),
            }
        };
    }

    private async Task<List<LandingPageItem>> LoadSeasonalPicks()
    {
        await Task.Delay(500);
        return new List<LandingPageItem>
        {
            new LandingPageItem
            {
                Id = Guid.Parse("d5d89b0b-eaae-4853-90c3-238d4531bd1a"),
                Category = "Seasonal 1",
                EndDate = DateTime.Now.AddMonths(3),
            },
            new LandingPageItem
            {
                Id = Guid.Parse("d5d89b0b-eaae-4853-90c3-238d4531bd1a"),
                Category = "Seasonal 2",
                EndDate = DateTime.Now.AddMonths(6),
            }
        };
    }

    private async Task<List<LandingPageItem>> LoadFeaturedStores()
    {
        await Task.Delay(500);
        return new List<LandingPageItem>
        {
            new LandingPageItem
            {
               Id = Guid.Parse("d5d89b0b-eaae-4853-90c3-238d4531bd1a"),
                Category = "Electronics",
                EndDate = DateTime.Now.AddYears(1),
            },
            new LandingPageItem
            {
                Id = Guid.Parse("d5d89b0b-eaae-4853-90c3-238d4531bd1a"),
                Category = "Fashion",
                EndDate = DateTime.Now.AddYears(1),
            }
        };
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


    protected  Task NavigateToAddItem()
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
        return DialogService.ShowAsync<AddSeasonalPickModal>("", parameters, options);

    }

    protected async Task ReplaceItem(LandingPageItem item)
    {
        currentItem = item;
        OpenDialogAsync();
        await Task.CompletedTask;
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
        var parameters = new DialogParameters
        {
            { "Title", "Delete Confirmation" },
            { "Descrption", "Do you want to delete this Event?" },
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

public enum LandingPageItemType
{
    FeaturedCategory = 0,
    SeasonalPick = 1,
    FeaturedStore = 2
}

