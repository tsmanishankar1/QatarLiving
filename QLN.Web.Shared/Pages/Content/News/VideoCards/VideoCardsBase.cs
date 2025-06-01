using Microsoft.AspNetCore.Components;
using QLN.Common.Infrastructure.DTO_s;
using Microsoft.JSInterop;
using QLN.Web.Shared.Model;
using QLN.Web.Shared.Models;
using Microsoft.EntityFrameworkCore.Diagnostics;
public class VideoCardsBase : ComponentBase
{
    [Parameter]
    public List<ContentVideo> Articles { get; set; } = new List<ContentVideo>();
    [Parameter] public EventCallback<ContentVideo> OnClick { get; set; }
    [Parameter]
    public string selectedTab { get; set; }
    [Parameter] public bool loading { get; set; }
    [Inject] protected NavigationManager navManager { get; set; }
    protected ContentVideo SelectedArticle { get; set; }
    

    protected override void OnParametersSet()
    {
        if (Articles != null && Articles.Any())
        {
            SelectedArticle = Articles.First();
        }
    }

    protected void SelectVideo(ContentVideo article)
    {
        SelectedArticle = article;
    }
    protected void onclick(ContentVideo news)
    {
        Console.WriteLine("the clicked aritcle is " + news.Title);
        Console.WriteLine("the clicked aritcle is " + news.Slug);
         if (news == null || string.IsNullOrEmpty(news.Slug))
            return;
        navManager.NavigateTo($"/content/article/details/{news.Slug}");
    }
}