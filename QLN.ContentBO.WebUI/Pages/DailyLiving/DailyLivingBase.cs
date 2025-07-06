using Microsoft.AspNetCore.Components;
using MudBlazor;
using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Pages.DailyLiving.Components.RadioAutoCompleteDialog;

public class DailyLivingBase : ComponentBase
{
    protected int activeIndex = 0;
    protected List<DailyLivingArticleDto> articles = new();
    protected bool isLoading = false;
    protected DailyLivingTab SelectedTab => (DailyLivingTab)activeIndex;

    [Inject] public IDailyLivingService DailyService { get; set; }
    [Inject] public IDialogService DialogService { get; set; }
    [Inject] public ILogger<DailyLivingBase> Logger { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await LoadArticlesAsync();
    }

    protected async Task OnTabChanged(int newIndex)
    {
        if (newIndex == activeIndex)
            return;

        activeIndex = newIndex;
        await LoadArticlesAsync();
    }


    private async Task LoadArticlesAsync()
    {
        isLoading = true;
        StateHasChanged();

        try
        {
            switch (SelectedTab)
            {
                case DailyLivingTab.TopSection:
                    articles = await DailyService.GetTopSectionAsync();
                    break;
                case DailyLivingTab.FeaturedEvents:
                    articles = await DailyService.GetFeaturedEventsAsync();
                    break;
                case DailyLivingTab.EverythingQatar:
                    articles = await DailyService.GetContentByTopicIdAsync("5da6688e-6018-48bb-8c17-d4b0219adc8c");
                    break;
                case DailyLivingTab.Lifestyle:
                    articles = await DailyService.GetContentByTopicIdAsync("3072ea6f-35b2-460a-b45c-796b942d0bad");
                    break;
                case DailyLivingTab.SportsNews:
                    articles = await DailyService.GetContentByTopicIdAsync("83a18b49-7ad7-4148-b5a0-5dc71e75a537");
                    break;
                case DailyLivingTab.QLExclusive:
                    articles = await DailyService.GetContentByTopicIdAsync("31e36a07-acb0-4a32-9d4e-ca55a68f9f8f");
                    break;
                case DailyLivingTab.AdviceHelp:
                    articles = await DailyService.GetContentByTopicIdAsync("4c1e7ed7-9424-413b-b846-81454183e87b");
                    break;
                default:
                    articles = new();
                    break;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load articles for tab {Tab}", SelectedTab);
            articles = new();
        }

        isLoading = false;
        StateHasChanged();
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
