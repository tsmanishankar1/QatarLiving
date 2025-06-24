using Microsoft.AspNetCore.Components;
using QLN.Common.Infrastructure.DTO_s;
using MudBlazor;
public class MessageBoxBase : ComponentBase
{
    [Parameter] public string Title { get; set; } = "Featured Event";
    [Parameter] public string Placeholder { get; set; } = "Article Title*";
    [Parameter] public EventCallback<string> OnAdd { get; set; }
    [Parameter]
    public List<string> AllEventTitles { get; set; } = new();
    protected string SelectedValue { get; set; }

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