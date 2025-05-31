using Microsoft.AspNetCore.Components;
using QLN.Common.Infrastructure.DTO_s;
public class DailyNewsCardBase : ComponentBase
{
    [Inject] NavigationManager NavigationManager { get; set; }
    [Parameter]
    public ContentEvent Item { get; set; }
    [Parameter]
    public bool IsHorizontal { get; set; } = false;

    protected void NavigateToEventDetail()
    {
        NavigationManager.NavigateTo($"/events/details/{Item.Slug}");
    }

}