using Microsoft.AspNetCore.Components;
using QLN.Web.Shared.Model;
using QLN.Web.Shared.Models;
public class NewsCardBase : ComponentBase
{
    [Inject]
    protected NavigationManager navManager { get; set; }
    
    [Parameter]
    public NewsItem Item { get; set; }
    [Parameter]
    public bool IsHorizontal { get; set; } = false;
     protected void NavigateToDetails()
    {
        navManager.NavigateTo("/article/details");
    }

}