using Microsoft.AspNetCore.Components;
using QLN.Common.Infrastructure.DTO_s;
public class MoreArticleSectionBase : ComponentBase
{
    [Parameter]
    public List<ContentPost> Articles { get; set; } = new List<ContentPost>();
     [Parameter]
    public bool loading { get; set; } 
    public ContentPost selectedPost { get; set; }
     protected void SelectArticle(ContentPost article)
    {
        selectedPost = article;
    }

}