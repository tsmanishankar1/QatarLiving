using Microsoft.AspNetCore.Components;

namespace QLN.ContentBO.WebUI.Components.News
{
    public class CustomMudDataGridBase : ComponentBase
    {
        protected ArticleDTO Article = new()
        {
            Id = 1,
            Title = "Example Article",
            Categories = new List<ArticleCategory>
        {
            new() { CategoryId = 1, SubcategoryId = 10, PositionId = 1 },
            new() { CategoryId = 2, SubcategoryId = 11, PositionId = 2 },
            new() { CategoryId = 3, SubcategoryId = 12, PositionId = 3 },
        }
        };

        protected void OnReorder(IReadOnlyList<ArticleCategory> newOrder)
        {
            Article.Categories = newOrder.ToList();

            // Recalculate PositionId based on new order
            for (int i = 0; i < Article.Categories.Count; i++)
            {
                Article.Categories[i].PositionId = i + 1;
            }

            Console.WriteLine("New Order:");
            foreach (var item in Article.Categories)
            {
                Console.WriteLine($"Position {item.PositionId} -> CategoryId {item.CategoryId}");
            }

            // Optional: await _httpClient.PostAsJsonAsync("api/article/update-order", Article.Categories);
        }

        protected void Edit(ArticleCategory item)
        {
            Console.WriteLine($"Edit CategoryId {item.CategoryId}");
        }

        protected void Delete(ArticleCategory item)
        {
            Article.Categories.Remove(item);
        }
        protected class ArticleDTO
        {
            public int Id { get; set; }
            public string Title { get; set; } = string.Empty;
            public DateTime PublishedDate { get; set; } = DateTime.Now;
            public List<ArticleCategory> Categories { get; set; } = new();
        }

        protected class ArticleCategory
        {
            public int CategoryId { get; set; }
            public int SubcategoryId { get; set; }
            public int PositionId { get; set; }
        }
    }
}
