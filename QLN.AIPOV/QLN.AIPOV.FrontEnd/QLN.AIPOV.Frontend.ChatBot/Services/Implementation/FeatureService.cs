using QLN.AIPOV.Frontend.ChatBot.Services.Interfaces;

namespace QLN.AIPOV.Frontend.ChatBot.Services.Implementation
{
    public class FeatureService : IFeatureService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<FeatureService> _logger;

        public FeatureService(IConfiguration configuration, ILogger<FeatureService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public bool IsFeatureEnabled(string featureName)
        {
            var featureEnabled = _configuration.GetValue<bool>($"FeatureManagement:{featureName}", false);
            _logger.LogInformation("Feature {FeatureName} is {Status}", featureName, featureEnabled ? "enabled" : "disabled");
            return featureEnabled;
        }

        public Dictionary<string, bool> GetEnabledNavigationFeatures()
        {
            // Get all navigation features from configuration
            var navigationFeatures = new Dictionary<string, bool>
            {
                { "ChatBot", IsFeatureEnabled("ChatBot") },
                { "CVAnalyzer", IsFeatureEnabled("CVAnalyzer") },
                { "HybridSearch", IsFeatureEnabled("HybridSearch") }
            };
            
            return navigationFeatures;
        }
    }
}