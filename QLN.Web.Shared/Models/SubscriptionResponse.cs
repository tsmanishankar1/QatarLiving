namespace QLN.Web.Shared.Models
{
    public class SubscriptionResponse
    {
        public int VerticalTypeId { get; set; }
        public string VerticalName { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public List<SubscriptionPlan> Subscriptions { get; set; }
    }


}
