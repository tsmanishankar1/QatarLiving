using Microsoft.AspNetCore.Components;
using QLN.Common.Infrastructure.DTO_s;
using MudBlazor;
public class MessageBoxBase : ComponentBase
{
    [Parameter] public string Title { get; set; } = "Featured Event";
    [Parameter] public string Placeholder { get; set; }
    [Parameter] public EventCallback<string> OnAdd { get; set; }
    protected string EventTitle;

    protected async Task AddClicked()
    {
        if (OnAdd.HasDelegate)
            await OnAdd.InvokeAsync(EventTitle);
    }
   
}