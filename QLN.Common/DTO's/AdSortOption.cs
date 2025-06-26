using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public enum AdSortOption
    {
        Default,

        // Creation Date
        CreationDateOldest,
        CreationDateRecent,

        // Expiry Date
        ExpiryDateRecent,
        ExpiryDateOldest,

        // Add-on Expiry Date
        AddOnExpiryDateOldest,
        AddOnExpiryDateRecent,

        // Price
        PriceHighToLow,
        PriceLowToHigh
    }

}
