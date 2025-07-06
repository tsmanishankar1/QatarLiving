using Microsoft.AspNetCore.Components;
using QLN.ContentBO.WebUI.Models;

namespace QLN.ContentBO.WebUI.Components
{
    public class MudArticleTableBase : ComponentBase
    {
        [Parameter]
        public List<DailyLivingArticleDto> Articles { get; set; } = new();
        //    public class Article
        //{
        //    public string Slot { get; set; }
        //    public string Title { get; set; }
        //    public string? Link { get; set; }
        //    public string Category { get; set; }
        //    public string Subcategory { get; set; }
        //    public DateTime CreationDate { get; set; }
        //}

        //protected List<Article> articles = new()
        //{
        //    new() { Slot = "Top Story", Title = "From the Sands to the Stands: A journey through sports", Link = "#", Category = "News", Subcategory = "Qatar", CreationDate = new DateTime(2025, 4, 12) },
        //    new() { Slot = "Top Event", Title = "Qatar’s Abdulwahab creates history, enters Round of 64 in TT Worlds", Category = "Sports", Subcategory = "Olympics", CreationDate = new DateTime(2025, 4, 12) },
        //    new() { Slot = "Article 1", Title = "10 sports activities to keep your teens busy", Category = "Sports", Subcategory = "Olympics", CreationDate = new DateTime(2025, 4, 12) },
        //    new() { Slot = "Article 2", Title = "Doha gears up for 2025 World Table Tennis Championships from S...", Category = "Sports", Subcategory = "Olympics", CreationDate = new DateTime(2025, 4, 12) },
        //    new() { Slot = "Article 3", Title = "Qatar to play Bahrain in the final of the Arab Handball Cup on Sunday", Category = "Sports", Subcategory = "Olympics", CreationDate = new DateTime(2025, 4, 12) },
        //    new() { Slot = "Article 4", Title = "Gib and Slim set for dream boxing showdown in Doha on Novembe...", Link = "#", Category = "Sports", Subcategory = "Olympics", CreationDate = new DateTime(2025, 4, 12) },
        //    new() { Slot = "Article 5", Title = "Qatar’s Abdulwahab creates history, enters Round of 64 in TT Worlds", Category = "Sports", Subcategory = "Olympics", CreationDate = new DateTime(2025, 4, 12) },
        //    new() { Slot = "Article 6", Title = "National fencer Ghoroor Abdulwaheed: Girls don’t have to choose b...", Category = "Sports", Subcategory = "Olympics", CreationDate = new DateTime(2025, 4, 12) },
        //    new() { Slot = "Article 7", Title = "Qatar’s Abdulwahab creates history, enters Round of 64 in TT Worlds", Category = "Sports", Subcategory = "Olympics", CreationDate = new DateTime(2025, 4, 12) }
        //};

        //}
    }
}