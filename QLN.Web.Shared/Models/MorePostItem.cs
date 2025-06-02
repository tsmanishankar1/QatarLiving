using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Web.Shared.Model
{
    public class MorePostsResponse
    {
        public QlnCommunityPost qln_community_post { get; set; }
    }

    public class QlnCommunityPost
    {
        public QlnCommunityPostMorePosts qln_community_post_more_posts { get; set; }
    }

    public class QlnCommunityPostMorePosts
    {
        public string queue_label { get; set; }
        public List<MorePostItem> items { get; set; }
    }

    public class MorePostItem
    {
        public string title { get; set; }
        public string slug { get; set; }
    }
}
