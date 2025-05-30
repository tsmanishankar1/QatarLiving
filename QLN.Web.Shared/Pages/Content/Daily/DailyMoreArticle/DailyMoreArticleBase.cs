using Microsoft.AspNetCore.Components;
using QLN.Common.Infrastructure.DTO_s;
public class DailyMoreArticleBase : ComponentBase
{
    [Parameter]
    public List<ContentPost> Items { get; set; } = [];
}