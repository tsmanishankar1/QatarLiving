using Microsoft.AspNetCore.Components;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Web.Shared.Components;

namespace QLN.Web.Shared.Pages.Content.DailyV2.DailyMoreArticle
{
    public class DailyMoreArticleBase : QLComponentBase
    {
        [Inject] NavigationManager NavigationManager { get; set; }
        [Parameter]
        public List<ContentEvent> Items { get; set; } = [];
        [Parameter]
        public bool isLoading { get; set; } = false;

        protected void NavigatetoArticle(ContentEvent article)
        {
            NavigationManager.NavigateTo($"{NavigationPath.Value.ContentNewsDetail}{article.Slug}");
        }

    }
}