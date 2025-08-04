using QLN.Common.Infrastructure.Subscriptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s.Payments
{
    public class ExternalPaymentRequest
    {
        /// <summary>
        /// Ad Id against which the payment has to be done
        /// </summary>
        public string? AdId { get; set; }

        /// <summary>
        /// Subscription Id against which the payment has to be done
        /// </summary>
        public string? SubscriptionId { get; set; }

        /// <summary>
        /// Subscription Type Id against which the payment has to be done
        /// </summary>
        public string? SubscriptionTypeId { get; set; }

        /// <summary>
        /// Number of features to purchase
        /// </summary>
        public int? NoOfFeatures { get; set; }

        /// <summary>
        /// Type of the product to purchase
        /// </summary>
        public ProductType? ProductType { get; set; }

        /// <summary>
        /// User Subscription Id for the associated payment
        /// </summary>
        public string? UserSubscriptionId { get; set; }

        /// <summary>
        /// Addon details with respective d365Id, featureBudget or refreshBudget
        /// </summary>
        public List<AddonPaymentDto>? Addons { get; set; }

        /// <summary>
        /// Vertical
        /// </summary>
        public Vertical Vertical { get; set; }

        /// <summary>
        /// Use reward points
        /// </summary>
        public bool? UsePoints { get; set; }

        /// <summary>
        /// Payment amount
        /// </summary>
        public decimal? Amount { get; set; }

        /// <summary>
        /// Order Id
        /// </summary>
        public int? OrderId { get; set; }
    }
}
