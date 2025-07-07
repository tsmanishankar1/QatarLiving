using Microsoft.AspNetCore.Components;
using QLN.ContentBO.WebUI.Models;

namespace QLN.ContentBO.WebUI.Components
{
    public class MudArticleTableBase : ComponentBase
    {
        [Parameter]
        public List<DailyLivingArticleDto> Articles { get; set; } = new();
    }
}