using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using QLN.Common.DTO_s;
using System.Linq;
using System.Threading.Tasks;

namespace QLN.Web.Shared.Components.Classifieds.StoreCard
{
    public class StoreCardBase : ComponentBase
    {
        [Parameter] public LandingBackOfficeIndex StoreData { get; set; } = new();
        [Parameter] public EventCallback<LandingBackOfficeIndex> OnShopNow { get; set; }
    }
}
