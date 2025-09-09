using Microsoft.AspNetCore.Components;
using QLN.Common.Infrastructure.DTO_s;
using QLN.ContentBO.WebUI.Models;
using MudBlazor;
public class MessageBoxBase : ComponentBase
{
    [Parameter] public string Title { get; set; } = "Featured Event";
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = default!;
    [Parameter] public string Placeholder { get; set; } = "Article Title*";
    [Parameter] public EventCallback<FeaturedSlot> OnAdd { get; set; }
    public void Cancel() => MudDialog.Cancel();
    [Parameter]
    public List<EventDTO> events { get; set; } = new();
    protected string? _autocompleteError;
    protected string? _searchText;
    protected FeaturedSlot SelectedEvent { get; set; } = new FeaturedSlot();
    protected async Task AddClicked()
    {
        _autocompleteError = null;
        var isValid = events.Any(e => e.EventTitle.Equals(SelectedEvent?.Event?.EventTitle, StringComparison.InvariantCultureIgnoreCase));
        if (!isValid)
        {
            _autocompleteError = "No matching event found.";
            StateHasChanged();
            return;
        }
        if (OnAdd.HasDelegate && SelectedEvent != null)
        {
            await OnAdd.InvokeAsync(SelectedEvent);
        }
        Cancel();
    }
    protected Task<IEnumerable<EventDTO>> SearchEventTitles(string value, CancellationToken token)
    {
        IEnumerable<EventDTO> result;
        if (string.IsNullOrWhiteSpace(value))
        {
            result = events;
        }
        else
        {
            result = events
                .Where(e => e.EventTitle.Contains(value, StringComparison.InvariantCultureIgnoreCase));
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
        var matched = events.Any(e => e.EventTitle.Equals(text, StringComparison.InvariantCultureIgnoreCase));
        _autocompleteError = matched ? null : "No matching event found.";
    }
    
}