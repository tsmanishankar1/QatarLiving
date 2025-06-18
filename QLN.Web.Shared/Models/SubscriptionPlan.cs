namespace QLN.Web.Shared.Models
{
    public class SubscriptionPlan
    {
        public string Id { get; set; } = "";
        public string SubscriptionName { get; set; } = "";
        public decimal Price { get; set; }
        public string Currency { get; set; } = "";
        public string Duration { get; set; } = "";
        public string Description { get; set; } = "";

        public int VerticalId { get; set; }
        public string VerticalName { get; set; } = "";
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = "";
        public override int GetHashCode()
        {
            return HashCode.Combine(SubscriptionName, Price, Duration);
        }
    }


    public class SubscriptionPaymentRequest
    {
        public string SubscriptionId { get; set; } = string.Empty;
        public int VerticalId { get; set; }
        public int SubcategoryId { get; set; }

        public CardDetails CardDetails { get; set; } = new();
    }

    public class CardDetails
    {
        public string CardNumber { get; set; } = string.Empty;
        public string ExpiryMonth { get; set; } = string.Empty;
        public string ExpiryYear { get; set; } = string.Empty;
        public string CVV { get; set; } = string.Empty;
        public string CardHolderName { get; set; } = string.Empty;
    }


}
