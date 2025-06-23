using Microsoft.AspNetCore.Components;
using QLN.Common.Infrastructure.DTO_s;
using MudBlazor;
public class SelectionDialogBase : ComponentBase
{
     public class ContentItem
    {
        public string Title { get; set; }
        public string Id { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    protected List<ContentItem> ExistingItems = new()
    {
        new ContentItem { Title = "Qatar Daily News", Id = "1000001330", LastUpdated = new DateTime(2025, 4, 2) },
        new ContentItem { Title = "Featured Stories", Id = "1000001352", LastUpdated = new DateTime(2025, 3, 30) },
        new ContentItem { Title = "Featured Stories", Id = "1000001352", LastUpdated = new DateTime(2025, 3, 30) }
    };
   
}