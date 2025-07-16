using Microsoft.AspNetCore.Components;

namespace QLN.ContentBO.WebUI.Pages.Classified.Items.EditAd
{
    public class EditAdActionBase : ComponentBase
    {
        [Parameter] public string UserName { get; set; } = "Rashid";
        [Parameter] public string Category { get; set; } = "Electronics";
        [Parameter] public int AdId { get; set; } = 21660;
        [Parameter] public int OrderId { get; set; } = 24578;
    }
}
