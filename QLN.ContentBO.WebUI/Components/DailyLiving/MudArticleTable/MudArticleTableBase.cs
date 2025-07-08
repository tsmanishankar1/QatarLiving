using Microsoft.AspNetCore.Components;
using QLN.ContentBO.WebUI.Models;

namespace QLN.ContentBO.WebUI.Components
{
    public class MudArticleTableBase : ComponentBase
    {
        [Parameter]
        public List<DailyLivingArticleDto> Articles { get; set; } = new();
        [Parameter] public EventCallback<DailyLivingArticleDto> ReplaceItem { get; set; }
        public List<DailyLivingArticleDto> slotArticles = Enumerable.Range(1, 9)
                .Select(i => new DailyLivingArticleDto
                {
                    Title = "Add Item",
                    SlotType = i
                })
                .ToList();
        protected override async Task OnParametersSetAsync()
        {
            slotArticles = Enumerable.Range(1, 9)
            .Select(i => new DailyLivingArticleDto
            {
                Title = "Add Item",
                SlotType = i
            })
            .ToList();
            foreach (var article in Articles)
            {
                if (Enum.IsDefined(typeof(DailySlotType), article.SlotType))
                {
                    var index = article.SlotType - 1;
                    if (index >= 0 && index < slotArticles.Count)
                    {
                        slotArticles[index] = article;
                    }
                }
            }
        }
         protected async Task ReplaceArticle(DailyLivingArticleDto article) 
        {
            await ReplaceItem.InvokeAsync(article);
        }

        
    }
}