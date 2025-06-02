using Microsoft.AspNetCore.Components;
using QLN.Common.Infrastructure.DTO_s;
public class DailyNewsCardBase : ComponentBase
{
    [Inject] NavigationManager NavigationManager { get; set; }
    [Parameter]
    public ContentPost Item { get; set; }
    [Parameter]
    public bool IsHorizontal { get; set; } = false;

    protected void NavigateToEventDetail()
    {
        NavigationManager.NavigateTo($"/content/events/details/{Item.Slug}");
    }

}