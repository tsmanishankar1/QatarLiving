using Microsoft.AspNetCore.Components;
using QLN.ContentBO.WebUI.Models;

namespace QLN.ContentBO.WebUI.Components.Classified.OverviewCard
{
    public class OverviewCardBase : ComponentBase
    {
        [Parameter] public PreviewAdDto Item { get; set; } = default!;

        protected record OverviewItem(string Icon, string Label);

        protected List<OverviewItem> OverviewData { get; set; } = new();

        protected override void OnParametersSet()
        {
            if (Item == null)
            {
                OverviewData.Clear();
                return;
            }

            string? Format(string label, string? value, string fallback)
                => string.IsNullOrWhiteSpace(value) ? fallback : $"{label}: {value}";

            OverviewData = new()
            {
                new("/qln-images/classifieds/brand_icons.svg", Format("Brand", Item.DynamicFields.GetValueOrDefault("Brand"), "Not specified")),
                new("/qln-images/classifieds/model_icons.svg", Format("Model", Item.DynamicFields.GetValueOrDefault("Model"), "Unknown")),
                new("/qln-images/classifieds/colour_icon.svg", Format("Colour", Item.DynamicFields.GetValueOrDefault("Colour"), "N/A")),
                new("/qln-images/classifieds/storage_icon.svg", Format("Storage", Item.DynamicFields.GetValueOrDefault("Storage"), "N/A")),
                new("/qln-images/classifieds/battery_icons.svg", Format("Battery", Item.DynamicFields.GetValueOrDefault("Battery Life"), "Unknown")),
                new("/qln-images/classifieds/chip_icons.svg", Format("Chip", Item.DynamicFields.GetValueOrDefault("Processor"), "Not mentioned")),
                // new("/qln-images/classifieds/wireless_icon.svg", "Wireless Charging"),
                new("/qln-images/classifieds/warranty_icon.svg", Format("Warranty", Item.DynamicFields.GetValueOrDefault("Coverage"), "No Warranty")),
            };
        }
    }
}
