using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Web.Shared.Pages.Content.Community
{
    public class PostModel
    {
        public string Category { get; set; }
        public string Title { get; set; }
        public string? ImageUrl { get; set; }
        public string BodyPreview { get; set; }
        public string Author { get; set; }
        public DateTime Time { get; set; }
        public int LikeCount { get; set; }
        public int CommentCount { get; set; }
    }

}
