using Microsoft.AspNetCore.Components;
using QLN.Web.Shared.Model;
using QLN.Web.Shared.Models;
public class ArticleDetailCardBase : ComponentBase
{
    public string DescriptionHtml { get; set; }
    protected MarkupString ParsedDescription => new MarkupString(DescriptionHtml);
}
