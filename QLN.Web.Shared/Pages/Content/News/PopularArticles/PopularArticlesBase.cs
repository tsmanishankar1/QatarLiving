using Microsoft.AspNetCore.Components;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Web.Shared.Model;
using QLN.Web.Shared.Models;
public class PopularArticlesBase : ComponentBase
{
    [Parameter]
    public List<ContentPost> Articles { get; set; } = new List<ContentPost>();
    [Inject]
    protected NavigationManager navManager { get; set; }
    [Parameter]
    public string selectedTab { get; set; }
    [Parameter]
    public bool loading { get; set; } = false;
     protected bool imageLoaded = false;
 

    protected void onclick(ContentPost news)
    {
        navManager.NavigateTo($"/content/article/details/{selectedTab}/{news.Slug}");
    }
     protected override void OnParametersSet()
        {
            // imageLoaded = false; 
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
}