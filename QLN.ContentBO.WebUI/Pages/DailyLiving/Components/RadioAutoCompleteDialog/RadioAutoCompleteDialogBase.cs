using Microsoft.AspNetCore.Components;
using QLN.Common.Infrastructure.DTO_s;
using QLN.ContentBO.WebUI.Models;
using System.Text.RegularExpressions;
using MudBlazor;
public class RadioAutoCompleteDialogBase : ComponentBase
{
    [Parameter] public string Title { get; set; } = "Featured Event";
    public string youTubelink { get; set; } = "";
    [Parameter] public string origin { get; set; }
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
        "Article" => "Article Title*",
        "Video" => "Video Link*",
        "Event" => "Event Title*",
        _ => "Search"
    };
    protected string TopicType = "Article";
    protected async Task AddClicked()
    {
        _autocompleteError = null;
        if (TopicType == "Video")
        {
            if (!IsValidYouTubeUrl(youTubelink))
            {
                _autocompleteError = "Please enter a valid Video link.";
                StateHasChanged();
                return;
            }
            else
            {
                _autocompleteError = null;
            }
            SelectedArticle.ContentURL = youTubelink;
            SelectedArticle.ContentType = 3;
        }
        else
        {
            var isValid = articles.Any(e => e.Title.Equals(SelectedArticle.Title, StringComparison.InvariantCultureIgnoreCase));
            if (!isValid)
            {
                _autocompleteError = "No matching event found.";
                StateHasChanged();
                return;
            }
            SelectedArticle.ContentType = 1;
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
    private bool IsValidYouTubeUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;
       var pattern = @"^(https?:\/\/)?(www\.)?(youtube\.com\/watch\?v=|youtu\.be\/)[\w\-]{11}(&\S*)?$";
        return Regex.IsMatch(url, pattern, RegexOptions.IgnoreCase);
    }
}

