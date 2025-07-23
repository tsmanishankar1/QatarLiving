using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QLN.AIPOV.Frontend.ChatBot.Services.Interfaces
{
    /// <summary>
    /// Service for managing feature flags in the application
    /// </summary>
    public interface IFeatureService
    {
        /// <summary>
        /// Checks if a feature is enabled
        /// </summary>
        /// <param name="featureName">Name of the feature to check</param>
        /// <returns>True if the feature is enabled, false otherwise</returns>
        bool IsFeatureEnabled(string featureName);

        /// <summary>
        /// Gets all enabled navigation features
        /// </summary>
        /// <returns>A dictionary of feature names and their enabled status</returns>
        Dictionary<string, bool> GetEnabledNavigationFeatures();
    }
}