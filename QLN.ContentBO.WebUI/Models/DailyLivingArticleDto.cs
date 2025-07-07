namespace QLN.ContentBO.WebUI.Models
{
    public class DailyLivingArticleDto
    {
        public string Id { get; set; }
        public int SlotType { get; set; }
        public string Title { get; set; }
        public string Category { get; set; }
        public string Subcategory { get; set; }
        public string RelatedContentId { get; set; }
        public int ContentType { get; set; }
        public string ContentURL { get; set; }
        public DateTime PublishedDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int SlotNumber { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
    public enum DailyLivingTab
    {
        TopSection = 0,
        FeaturedEvents = 1,
        EverythingQatar = 2,
        Lifestyle = 3,
        SportsNews = 4,
        QLExclusive = 5,
        AdviceHelp = 6
    }
    public class DailyTopic
    {
        public string Id { get; set; }
        public string topicName { get; set; }
        public bool isPublished { get; set; }
    }
}
public enum DailySlotType
   {
       TopStory = 1,
       HighlightedEvent = 2,
       Article1 = 3,
       Article2 = 4,
       Article3 = 5,
       Article4 = 6,
       Article5 = 7,
       Article6 = 8,
       Article7 = 9
   }
