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
    [Parameter] public bool IsHighlightedEvent { get; set; }
    [Parameter] public bool IsTopStory { get; set; }
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = default!;
    [Parameter] public EventCallback<DailyLivingArticleDto> OnAdd { get; set; }
    public void Cancel() => MudDialog.Cancel();
    [Parameter]
    public List<DailyLivingArticleDto> articles { get; set; } = new();
    public List<DailyLivingArticleDto> optionsList { get; set; } = new();
     [Parameter]
    public List<DailyLivingArticleDto> eventList { get; set; } = new();
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
    protected override async Task OnParametersSetAsync()
    {
        Console.WriteLine("Received articels are!!");
        foreach (var item in articles)
        {
            Console.WriteLine($"Title: {item.Title}, Location: {item.Id}, ");
        }
        if (IsHighlightedEvent)
        {
            TopicType = "Event";
        }
        optionsList = origin == "dailyTopic" ? articles : IsHighlightedEvent ? articles : articles;
        Console.WriteLine("the options list");
        foreach (var item in articles)
        {
            Console.WriteLine($"Title: {item.Title}, Location: {item.Id}, ");
        }
    }
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
            var isValid = optionsList.Any(e => e.Title.Equals(SelectedArticle.Title, StringComparison.InvariantCultureIgnoreCase));
            if (!isValid)
            {
                _autocompleteError = "No matching event found.";
                StateHasChanged();
                return;
            }
            SelectedArticle.ContentType = 1;
        }
        if (origin == "dailyTopic")
        {
            SelectedArticle.ContentType = TopicType switch
        {
            "Article" => 1,
            "Video" => 3,
            "Event" => 2,
            _ => 1
        };
            
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
            result = optionsList;
        }
        else
        {
            result = optionsList
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
        var matched = optionsList.Any(e => e.Title.Equals(text, StringComparison.InvariantCultureIgnoreCase));
        _autocompleteError = matched ? null : "No matching event found.";
    }
    private bool IsValidYouTubeUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;
        var pattern = @"^(https?:\/\/)?(www\.)?(youtube\.com\/watch\?v=|youtu\.be\/)[\w\-]{11}(&\S*)?$";
        return Regex.IsMatch(url, pattern, RegexOptions.IgnoreCase);
    }
    protected void OnTopicTypeChanged(string value)
{
    Console.WriteLine($"Selected Topic Type: {value}");
    TopicType = value;

    if (value == "Event")
    {
        optionsList = eventList;
    }
    else if(value == "Article")
    {
        optionsList = articles;
    }
}
}

