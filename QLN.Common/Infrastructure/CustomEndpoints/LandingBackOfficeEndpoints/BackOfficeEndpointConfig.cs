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
            var servicesGroup = app.MapGroup("/api/services/landing").WithTags("ServicesLandingPageMaster");

            servicesGroup.MapBackOfficeMasterEndpoints(
              vertical: ConstantValues.Verticals.Services,
              routeSegment: "hero-banner",
              entityType: ConstantValues.EntityTypes.HeroBanner
          );

            servicesGroup.MapBackOfficeMasterEndpoints(
               vertical: ConstantValues.Verticals.Services,
               routeSegment: "featured-services",
               entityType: ConstantValues.EntityTypes.FeaturedServices
           );

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


            var classifiedsGroup = app.MapGroup("/api/classifieds/landing").WithTags("ClassifiedsLandingPageMaster");

            classifiedsGroup.MapBackOfficeMasterEndpoints(
              vertical: ConstantValues.Verticals.Classifieds,
              routeSegment: "hero-banner",
              entityType: ConstantValues.EntityTypes.HeroBanner
            );

            classifiedsGroup.MapBackOfficeMasterEndpoints(
               vertical: ConstantValues.Verticals.Classifieds,
               routeSegment: "featured-items",
               entityType: ConstantValues.EntityTypes.FeaturedItems
            );

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
