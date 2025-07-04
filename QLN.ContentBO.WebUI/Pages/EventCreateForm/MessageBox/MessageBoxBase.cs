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
    protected FeaturedSlot SelectedEvent { get; set; } = new FeaturedSlot();
    protected async Task AddClicked()
    {
        // if (OnAdd.HasDelegate)
        //     await OnAdd.InvokeAsync(SelectedEvent);
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
}