using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.CustomEndpoints;
namespace QLN.Common.Infrastructure.CustomEndpoints
{
    /// <summary>
    /// Maps all “Back-Office Master‐Data” endpoints for each vertical.
    /// Each vertical (“services”, “classifieds”, etc.) gets its own RouteGroup
    /// with separate EntityTypes (featured‐categories, seasonal‐picks, social‐links, faqs, ready‐to‐grow).
    /// Under the hood, these endpoints call ISearchService (ExternalSearchService via Dapr).
    /// </summary>
    public static class BackOfficeEndpointConfig
    {
        public static void MapAllBackOfficeEndpoints(this WebApplication app)
        {
            // ======== Services Vertical ========
            var servicesGroup = app.MapGroup("/api/services").WithTags("LandingPageMaster");

            servicesGroup.MapBackOfficeMasterEndpoints(
                vertical: ConstantValues.Verticals.Services,
                routeSegment: "featured-categories",
                entityType: ConstantValues.EntityTypes.FeaturedCategory
            );

            servicesGroup.MapBackOfficeMasterEndpoints(
                vertical: ConstantValues.Verticals.Services,
                routeSegment: "categories",
                entityType: ConstantValues.EntityTypes.Category
            );
            servicesGroup.MapBackOfficeMasterEndpoints(
                vertical: ConstantValues.Verticals.Services,
                routeSegment: "seasonal-picks",
                entityType: ConstantValues.EntityTypes.SeasonalPick
            );

            servicesGroup.MapBackOfficeMasterEndpoints(
                vertical: ConstantValues.Verticals.Services,
                routeSegment: "social-links",
                entityType: ConstantValues.EntityTypes.SocialMediaLink
            );

            servicesGroup.MapBackOfficeMasterEndpoints(
                vertical: ConstantValues.Verticals.Services,
                routeSegment: "faqs",
                entityType: ConstantValues.EntityTypes.FaqItem
            );

            servicesGroup.MapBackOfficeMasterEndpoints(
                vertical: ConstantValues.Verticals.Services,
                routeSegment: "ready-to-grow",
                entityType: ConstantValues.EntityTypes.CallToAction
            );


            // ======== Classifieds Vertical ========
            var classifiedsGroup = app.MapGroup("/api/classifieds").WithTags("LandingPageMaster");

            classifiedsGroup.MapBackOfficeMasterEndpoints(
                vertical: ConstantValues.Verticals.Classifieds,
                routeSegment: "featured-categories",
                entityType: ConstantValues.EntityTypes.FeaturedCategory
            );
            classifiedsGroup.MapBackOfficeMasterEndpoints(
                vertical: ConstantValues.Verticals.Classifieds,
                routeSegment: "categories",
                entityType: ConstantValues.EntityTypes.Category
            );

            classifiedsGroup.MapBackOfficeMasterEndpoints(
                vertical: ConstantValues.Verticals.Classifieds,
                routeSegment: "seasonal-picks",
                entityType: ConstantValues.EntityTypes.SeasonalPick
            );

            classifiedsGroup.MapBackOfficeMasterEndpoints(
                vertical: ConstantValues.Verticals.Classifieds,
                routeSegment: "social-links",
                entityType: ConstantValues.EntityTypes.SocialMediaLink
            );

            classifiedsGroup.MapBackOfficeMasterEndpoints(
                vertical: ConstantValues.Verticals.Classifieds,
                routeSegment: "faqs",
                entityType: ConstantValues.EntityTypes.FaqItem
            );

            classifiedsGroup.MapBackOfficeMasterEndpoints(
                vertical: ConstantValues.Verticals.Classifieds,
                routeSegment: "ready-to-grow",
                entityType: ConstantValues.EntityTypes.CallToAction
            );
        }
    }
}
