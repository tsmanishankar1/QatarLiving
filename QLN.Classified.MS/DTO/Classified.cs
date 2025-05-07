namespace QLN.Classified.MS.DTO
{
    public class Classified
    {
        public class FAQItem
        {
            public string Question { get; set; }
            public string Answer { get; set; }
            public List<string> AnswerList { get; set; }
            public bool Expanded { get; set; }
        }

        public class SocialPlatform
        {
            public string Name { get; set; }
            public string Icon { get; set; }
            public string Link { get; set; }
        }

        public class VideoCard
        {
            public string VideoUrl { get; set; }
            public string Title { get; set; }
        }

        public class CategoryItem
        {
            public string Title { get; set; }
            public string Subtitle { get; set; }
            public string ImageUrl { get; set; }
        }

        public class StoreItem
        {
            public string Name { get; set; }
            public string LogoUrl { get; set; }
            public string ItemCount { get; set; }
        }

        public class PostStats
        {
            public int PostCount { get; set; }
        }
    }
}
