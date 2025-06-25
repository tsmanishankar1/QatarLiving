using Microsoft.AspNetCore.Components;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Web.Shared.Model;
using QLN.Web.Shared.Models;
public class VideoCardBase : ComponentBase
{
    [Inject]
    protected NavigationManager navManager { get; set; }
     [Parameter]
    public ContentVideo video { get; set; } = new ContentVideo();
    protected bool imageLoaded = false;
    protected bool imageFailed = false;
    protected string? currentImageUrl;
    protected override void OnParametersSet()
    {
       if (currentImageUrl != video.ImageUrl)
        {
            currentImageUrl = video.ImageUrl;
            imageLoaded = false;
            imageFailed = false;
        }
    }
    protected void OnImageLoaded()
    {
        imageLoaded = true;
        StateHasChanged();
    }
    protected void OnImageError()
    {
        imageLoaded = true;
        imageFailed = false;
        StateHasChanged();
    }
    protected bool ShowEmptyCard =>
        string.IsNullOrWhiteSpace(video?.ImageUrl) || imageFailed;
 
 
    [Parameter]
    public bool IsHorizontal { get; set; } = false;
}