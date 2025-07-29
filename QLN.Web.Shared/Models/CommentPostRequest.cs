namespace QLN.Web.Shared.Models
{
    public class CommentPostRequest
    {
        public int nid { get; set; }
        public int uid { get; set; }
        public string comment { get; set; }
    }
    public class CommentPostRequestDto
    {
        public string CommunityPostId { get; set; }
        public string Content {  get; set; }
    }
}
