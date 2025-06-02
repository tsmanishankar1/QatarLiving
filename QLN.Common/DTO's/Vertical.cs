using System.Text.Json.Serialization;

namespace QLN.Common.Infrastructure.Subscriptions
{
    public enum Vertical
    {

        Vehicles = 0,
        Properties = 1,
        Rewards = 2,
        Classifieds=3 
    }
    public enum SubscriptionCategory
    {
        Deals = 1,
        Stores = 2,
        Preloved = 3
    }
    public enum Status
    {
        Inactive = 0,
        Active = 1,
        Suspended = 2,
        Expired = 3
    }
}
