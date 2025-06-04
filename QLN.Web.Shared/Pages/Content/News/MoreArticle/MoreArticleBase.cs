using Microsoft.AspNetCore.Components;
using QLN.Common.Infrastructure.DTO_s;
public class MoreArticleBase : ComponentBase
{
    [Parameter]
    public List<ContentPost> Articles { get; set; } = new List<ContentPost>();
    [Inject]
    protected NavigationManager navManager { get; set; }
    protected bool imageLoaded = false;

    [Parameter]
    public bool loading { get; set; } = false;

    [Parameter]
    public string selectedTab { get; set; }

    [Parameter] public EventCallback<ContentPost> OnClick { get; set; }
    protected void onclick(ContentPost news)
    {
        navManager.NavigateTo($"/content/article/details/{selectedTab}/{news.Slug}");
    }
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
}