using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s.Subscription
{
    public sealed record FreeAdsRefundRequest(
     Guid SubscriptionId,
     string Category,
     string? L1Category,
     string? L2Category,
     int Amount
 );

    public sealed record FreeAdsRefundResult(bool Success, int Refunded);
}
