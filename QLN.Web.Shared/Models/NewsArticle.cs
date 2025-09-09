namespace QLN.Web.Shared.Services.Models

{
    public class NewsArticle
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string TitleImage { get; set; }
        public string Author { get; set; }
        public string Description { get; set; }
        public DateTime PublishedAt { get; set; }
    }
}
