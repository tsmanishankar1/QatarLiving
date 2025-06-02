using Microsoft.AspNetCore.Components;
using QLN.Common.Infrastructure.DTO_s;
public class MoreArticleSectionBase : ComponentBase
{
    [Parameter]
    public List<ContentPost> Articles { get; set; } = new List<ContentPost>();
    [Parameter]
    public bool loading { get; set; }
    protected bool imageLoaded = false;
    public ContentPost selectedPost { get; set; }
    protected void SelectArticle(ContentPost article)
    {
        selectedPost = article;
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