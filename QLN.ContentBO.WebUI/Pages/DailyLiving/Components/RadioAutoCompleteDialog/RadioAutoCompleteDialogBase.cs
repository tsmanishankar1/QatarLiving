using Microsoft.AspNetCore.Components;
using QLN.Common.Infrastructure.DTO_s;
using QLN.ContentBO.WebUI.Models;
using MudBlazor;
public class RadioAutoCompleteDialogBase : ComponentBase
{
    [Parameter] public string Title { get; set; } = "Featured Event";
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = default!;
    [Parameter] public EventCallback<DailyLivingArticleDto> OnAdd { get; set; }
    public void Cancel() => MudDialog.Cancel();
    [Parameter]
    public List<DailyLivingArticleDto> articles { get; set; } = new();
    protected string? _autocompleteError;
    protected string? _searchText;
    protected DailyLivingArticleDto SelectedArticle { get; set; } = new DailyLivingArticleDto();
    protected string Placeholder => TopicType switch
    {
        "Article" => "Article Link*",
        "Video" => "Video Link*",
        "Event" => "Event Link*",
        _ => "Search"
    };
    protected string TopicType = "Article";
    protected async Task AddClicked()
    {
        _autocompleteError = null;
        var isValid = articles.Any(e => e.Title.Equals(SelectedArticle.Title, StringComparison.InvariantCultureIgnoreCase));
        if (!isValid)
        {
            _autocompleteError = "No matching event found.";
            StateHasChanged();
            return;
        }
        if (OnAdd.HasDelegate && SelectedArticle != null)
        {
            await OnAdd.InvokeAsync(SelectedArticle);
        }
        Cancel();
    }
    protected Task<IEnumerable<DailyLivingArticleDto>> SearchEventTitles(string value, CancellationToken token)
    {
        IEnumerable<DailyLivingArticleDto> result;
        if (string.IsNullOrWhiteSpace(value))
        {
            result = articles;
        }
        else
        {
            result = articles
                .Where(e => e.Title.Contains(value, StringComparison.InvariantCultureIgnoreCase));
        }
        return Task.FromResult(result);
    }
    protected void ValidateTypedText(string text)
    {
        _searchText = text;
        if (string.IsNullOrWhiteSpace(text))
        {
            _autocompleteError = null;
            return;
        }
        var matched = articles.Any(e => e.Title.Equals(text, StringComparison.InvariantCultureIgnoreCase));
        _autocompleteError = matched ? null : "No matching event found.";
    }
}

