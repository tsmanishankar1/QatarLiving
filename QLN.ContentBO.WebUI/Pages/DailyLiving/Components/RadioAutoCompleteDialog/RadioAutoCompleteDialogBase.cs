using Microsoft.AspNetCore.Components;
using QLN.Common.Infrastructure.DTO_s;
using MudBlazor;
public class RadioAutoCompleteDialogBase : ComponentBase
{
    [Parameter] public string Title { get; set; } = "Featured Event";
    protected string Placeholder => TopicType switch
    {
        "Article" => "Article Link*",
        "Video" => "Video Link*",
        "Event" => "Event Link*",
        _ => "Search"
    };
    [Parameter] public EventCallback<string> OnAdd { get; set; }
    [Parameter]
    public List<string> AllEventTitles { get; set; } = new();
    protected string SelectedValue { get; set; }
    protected string TopicType = "Article";

    protected async Task AddClicked()
    {
        if (OnAdd.HasDelegate)
            await OnAdd.InvokeAsync(SelectedValue);
    }
    protected Task<IEnumerable<string>> SearchEventTitles(string value, CancellationToken token)
    {
        IEnumerable<string> result;
        if (string.IsNullOrWhiteSpace(value))
        {
            result = AllEventTitles;
        }
        else
        {
            result = AllEventTitles
                .Where(e => e.Contains(value, StringComparison.InvariantCultureIgnoreCase));
        }
        return Task.FromResult(result);
    }
}