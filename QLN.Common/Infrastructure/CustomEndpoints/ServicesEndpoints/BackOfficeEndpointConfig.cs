using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using QLN.Common.Infrastructure.CustomEndpoints;
namespace QLN.Common.Infrastructure.CustomEndpoints
{
    /// <summary>
    /// Maps all “Back-Office Master‐Data” endpoints for each vertical.
    /// Each vertical (“services”, “classifieds”, etc.) gets its own RouteGroup
    /// with separate segments (featured‐categories, seasonal‐picks, social‐links, faqs, ready‐to‐grow).
    /// Under the hood, these endpoints call ISearchService (ExternalSearchService via Dapr).
    /// </summary>
    public static class BackOfficeEndpointConfig
    {
        public static void MapAllBackOfficeEndpoints(this WebApplication app)
        {
            // ======== Services Vertical ========
            var servicesGroup = app.MapGroup("/api/services").WithTags("LandingPageMaster");

            // 1) Featured Categories
            servicesGroup.MapBackOfficeMasterEndpoints(
                vertical: "services",
                routeSegment: "featured-categories",
                entityType: "FeaturedCategory"
            );

            servicesGroup.MapBackOfficeMasterEndpoints(
                vertical: "services",
                routeSegment: "categories",
                entityType: "Category"
            );
            // 2) Seasonal Picks
            servicesGroup.MapBackOfficeMasterEndpoints(
                vertical: "services",
                routeSegment: "seasonal-picks",
                entityType: "SeasonalPick"
            );

            // 3) Social Media Links
            servicesGroup.MapBackOfficeMasterEndpoints(
                vertical: "services",
                routeSegment: "social-links",
                entityType: "SocialMediaLink"
            );

            // 4) FAQs
            servicesGroup.MapBackOfficeMasterEndpoints(
                vertical: "services",
                routeSegment: "faqs",
                entityType: "FaqItem"
            );

            // 5) Ready-to-Grow CTA
            servicesGroup.MapBackOfficeMasterEndpoints(
                vertical: "services",
                routeSegment: "ready-to-grow",
                entityType: "CallToAction"
            );


            // ======== Classifieds Vertical ========
            var classifiedsGroup = app.MapGroup("/api/classifieds").WithTags("LandingPageMaster");

            // 1) Featured Categories
            classifiedsGroup.MapBackOfficeMasterEndpoints(
                vertical: "classifieds",
                routeSegment: "featured-categories",
                entityType: "FeaturedCategory"
            );
            classifiedsGroup.MapBackOfficeMasterEndpoints(
                vertical: "classifieds",
                routeSegment: "categories",
                entityType: "Category"
            );
            // 2) Seasonal Picks
            classifiedsGroup.MapBackOfficeMasterEndpoints(
                vertical: "classifieds",
                routeSegment: "seasonal-picks",
                entityType: "SeasonalPick"
            );

            // 3) Social Media Links
            classifiedsGroup.MapBackOfficeMasterEndpoints(
                vertical: "classifieds",
                routeSegment: "social-links",
                entityType: "SocialMediaLink"
            );

            // 4) FAQs
            classifiedsGroup.MapBackOfficeMasterEndpoints(
                vertical: "classifieds",
                routeSegment: "faqs",
                entityType: "FaqItem"
            );

            // 5) Ready-to-Grow CTA
            classifiedsGroup.MapBackOfficeMasterEndpoints(
                vertical: "classifieds",
                routeSegment: "ready-to-grow",
                entityType: "CallToAction"
            );

            // … add other verticals here as needed …
        }
    }
}
