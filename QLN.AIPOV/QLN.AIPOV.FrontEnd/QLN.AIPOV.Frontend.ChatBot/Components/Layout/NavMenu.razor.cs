using Microsoft.AspNetCore.Components;
using QLN.AIPOV.Frontend.ChatBot.Services.Interfaces;

namespace QLN.AIPOV.Frontend.ChatBot.Components.Layout
{
    public partial class NavMenu
    {
        [Inject] public required IFeatureService FeatureService { get; set; }

        private Dictionary<string, bool> EnabledFeatures { get; set; } = new();

        protected override void OnInitialized()
        {
            EnabledFeatures = FeatureService.GetEnabledNavigationFeatures();
            base.OnInitialized();
        }
    }
}