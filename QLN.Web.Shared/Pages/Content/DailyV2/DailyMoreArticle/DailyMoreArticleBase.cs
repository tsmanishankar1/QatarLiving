using Microsoft.AspNetCore.Components;
using QLN.Common.Infrastructure.DTO_s;

namespace QLN.Web.Shared.Pages.Content.DailyV2.DailyMoreArticle
{
    public class DailyMoreArticleBase : ComponentBase
    {
        [Inject] NavigationManager NavigationManager { get; set; }
        [Parameter]
        public List<ContentEvent> Items { get; set; } = [];
        [Parameter]
        public bool isLoading { get; set; } = false;

        protected void NavigatetoArticle()
        {
            NavigationManager.NavigateTo("content/V2/events");
        }

        protected void NavigatetoArticle(ContentEvent article)
        {
            NavigationManager.NavigateTo($"/content/V2/daily/article/details/{article.Slug}");
        }

    }
}