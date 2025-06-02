using Microsoft.AspNetCore.Components;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Web.Shared.Model;
using QLN.Web.Shared.Models;
public class NewsCardBase : ComponentBase
{
    [Inject]
    protected NavigationManager navManager { get; set; }
     [Parameter]
    public ContentPost news { get; set; } = new ContentPost();
    protected bool imageLoaded = false;
    protected override void OnParametersSet()
    {
            imageLoaded = false; 
    }
    protected void OnImageLoaded()
    {
        imageLoaded = true;
        StateHasChanged();
    }
    protected void OnImageError()
    {
            imageLoaded = true; 
            StateHasChanged();
    }
 
 
    [Parameter]
    public bool IsHorizontal { get; set; } = false;
    [Parameter] public EventCallback<ContentPost> OnClick { get; set; }
}