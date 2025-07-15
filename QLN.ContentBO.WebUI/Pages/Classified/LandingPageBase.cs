using Microsoft.AspNetCore.Components;
using MudBlazor;
using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Components.ConfirmationDialog;
using System.Text.Json;
using QLN.Common.Infrastructure.IService;
using QLN.ContentBO.WebUI.Components;
using System;

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

    protected void NavigateToAddItem()
    {
        currentItem = new LandingPageItem();
        modalTitle = $"Add {GetCurrentTabAddButtonText()}";
        showItemModal = true;
        StateHasChanged();
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
                Title = "Featured Category 1",
                Description = "Description 1",
                Category = "Electronics",
                EndDate = DateTime.Now.AddDays(30),
                ImageUrl = ""
            },
            new LandingPageItem
            {
                Id = Guid.Parse("d5d89b0b-eaae-4853-90c3-238d4531bd1a"),
                Title = "Featured Category 2",
                Description = "Description 2",
                Category = "Fashion",
                EndDate = DateTime.Now.AddDays(60),
                ImageUrl = ""
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
                Title = "Summer Collection",
                Description = "Summer seasonal items",
                Category = "Seasonal",
                EndDate = DateTime.Now.AddMonths(3),
                ImageUrl = ""
            },
            new LandingPageItem
            {
                Id = Guid.Parse("d5d89b0b-eaae-4853-90c3-238d4531bd1a"),
                Title = "Winter Specials",
                Description = "Winter seasonal items",
                Category = "Seasonal",
                EndDate = DateTime.Now.AddMonths(6),
                ImageUrl = ""
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
                Title = "Electronics Store",
                Description = "Featured electronics store",
                Category = "Electronics",
                EndDate = DateTime.Now.AddYears(1),
                ImageUrl = ""
            },
            new LandingPageItem
            {
                Id = Guid.Parse("d5d89b0b-eaae-4853-90c3-238d4531bd1a"),
                Title = "Fashion Boutique",
                Description = "Featured fashion store",
                Category = "Fashion",
                EndDate = DateTime.Now.AddYears(1),
                ImageUrl = ""
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

    protected async Task ReplaceItem(LandingPageItem item)
    {
        currentItem = item;
        modalTitle = $"Edit {GetCurrentTabAddButtonText()}";
        showItemModal = true;
        StateHasChanged();
    }

    protected async Task DeleteItem(string id)
    {
        var parameters = new DialogParameters
        {
            { "Title", "Delete Confirmation" },
            { "Description", $"Do you want to delete this {currentItemType}?" },
            { "ButtonTitle", "Delete" },
            { "OnConfirmed", EventCallback.Factory.Create(this, async () => await ConfirmDeleteItem(id)) }
        };

        await ShowConfirmationDialog(parameters);
    }

    protected async Task ConfirmDeleteItem(string id)
    {
        try
        {
            await Task.Delay(500); // Simulate API call
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

            await Task.Delay(500); // Simulate API call

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

    private async Task ShowConfirmationDialog(DialogParameters parameters)
    {
        var options = new DialogOptions
        {
            CloseButton = false,
            MaxWidth = MaxWidth.Small,
            FullWidth = true
        };

        //var dialog = await DialogService.ShowAsync<Dia>("", parameters, options);
        //await dialog.Result;
    }
}

public enum LandingPageItemType
{
    FeaturedCategory = 0,
    SeasonalPick = 1,
    FeaturedStore = 2
}

public class LandingPageItem
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string Category { get; set; }
    public DateTime? EndDate { get; set; }
    public string ImageUrl { get; set; }
}