namespace QLN.Web.Shared.Model
{
    public class PostInteractionRequest
    {
        public Guid PostId { get; set; }
        public bool IsLike { get; set; }
    }
}