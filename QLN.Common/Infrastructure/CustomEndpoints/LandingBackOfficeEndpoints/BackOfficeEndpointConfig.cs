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
            var servicesGroup = app.MapGroup("/api/services/landing").WithTags("ServicesBackOffice");

            servicesGroup.MapBackOfficeMasterEndpoints(
              vertical: ConstantValues.Verticals.Services,
              routeSegment: ConstantValues.EntityRoutes.HeroBanner,
              entityType: ConstantValues.EntityTypes.HeroBanner
          );

            servicesGroup.MapBackOfficeMasterEndpoints(
               vertical: ConstantValues.Verticals.Services,
               routeSegment: ConstantValues.EntityRoutes.FeaturedServices,
               entityType: ConstantValues.EntityTypes.FeaturedServices
           );

            servicesGroup.MapBackOfficeMasterEndpoints(
                vertical: ConstantValues.Verticals.Services,
                routeSegment: ConstantValues.EntityRoutes.FeaturedCategory,
                entityType: ConstantValues.EntityTypes.FeaturedCategory
            );

            servicesGroup.MapBackOfficeMasterEndpoints(
                vertical: ConstantValues.Verticals.Services,
                routeSegment: ConstantValues.EntityRoutes.Category,
                entityType: ConstantValues.EntityTypes.Category
            );
            servicesGroup.MapBackOfficeMasterEndpoints(
                vertical: ConstantValues.Verticals.Services,
                routeSegment: ConstantValues.EntityRoutes.SeasonalPick,
                entityType: ConstantValues.EntityTypes.SeasonalPick
            );

            servicesGroup.MapBackOfficeMasterEndpoints(
                vertical: ConstantValues.Verticals.Services,
                routeSegment: ConstantValues.EntityRoutes.SocialPostSection,
                entityType: ConstantValues.EntityTypes.SocialPostSection
            );

            servicesGroup.MapBackOfficeMasterEndpoints(
                vertical: ConstantValues.Verticals.Services,
                routeSegment: ConstantValues.EntityRoutes.SocialMediaLink,
                entityType: ConstantValues.EntityTypes.SocialMediaLink
            );

            servicesGroup.MapBackOfficeMasterEndpoints(
                vertical: ConstantValues.Verticals.Services,
                routeSegment: ConstantValues.EntityRoutes.SocialMediaVideos,
                entityType: ConstantValues.EntityTypes.SocialMediaVideos
            );

            servicesGroup.MapBackOfficeMasterEndpoints(
                vertical: ConstantValues.Verticals.Services,
                routeSegment: ConstantValues.EntityRoutes.FaqItem,
                entityType: ConstantValues.EntityTypes.FaqItem
            );

            servicesGroup.MapBackOfficeMasterEndpoints(
                vertical: ConstantValues.Verticals.Services,
                routeSegment: ConstantValues.EntityRoutes.ReadyToGrow,
                entityType: ConstantValues.EntityTypes.ReadyToGrow
            );

            servicesGroup.MapBackOfficeMasterEndpoints(
                vertical: ConstantValues.Verticals.Services,
                routeSegment: ConstantValues.EntityRoutes.PopularSearch,
                entityType: ConstantValues.EntityTypes.PopularSearch
            );


            var classifiedsGroup = app.MapGroup("/api/classifieds/landing").WithTags("ClassifiedsBackOffice");

            classifiedsGroup.MapBackOfficeMasterEndpoints(
              vertical: ConstantValues.Verticals.Classifieds,
              routeSegment: ConstantValues.EntityRoutes.HeroBanner,
              entityType: ConstantValues.EntityTypes.HeroBanner
            );

            classifiedsGroup.MapBackOfficeMasterEndpoints(
               vertical: ConstantValues.Verticals.Classifieds,
               routeSegment: ConstantValues.EntityRoutes.FeaturedItems,
               entityType: ConstantValues.EntityTypes.FeaturedItems
            );

            classifiedsGroup.MapBackOfficeMasterEndpoints(
               vertical: ConstantValues.Verticals.Classifieds,
               routeSegment: ConstantValues.EntityRoutes.FeaturedStores,
               entityType: ConstantValues.EntityTypes.FeaturedStores
            );

            classifiedsGroup.MapBackOfficeMasterEndpoints(
                vertical: ConstantValues.Verticals.Classifieds,
                routeSegment: ConstantValues.EntityRoutes.FeaturedCategory,
                entityType: ConstantValues.EntityTypes.FeaturedCategory
            );
            classifiedsGroup.MapBackOfficeMasterEndpoints(
                vertical: ConstantValues.Verticals.Classifieds,
                routeSegment: ConstantValues.EntityRoutes.Category,
                entityType: ConstantValues.EntityTypes.Category
            );

            classifiedsGroup.MapBackOfficeMasterEndpoints(
                vertical: ConstantValues.Verticals.Classifieds,
                routeSegment: ConstantValues.EntityRoutes.SeasonalPick,
                entityType: ConstantValues.EntityTypes.SeasonalPick
            );

            classifiedsGroup.MapBackOfficeMasterEndpoints(
                vertical: ConstantValues.Verticals.Classifieds,
                routeSegment: ConstantValues.EntityRoutes.SocialPostSection,
                entityType: ConstantValues.EntityTypes.SocialPostSection
            );

            classifiedsGroup.MapBackOfficeMasterEndpoints(
                vertical: ConstantValues.Verticals.Classifieds,
                routeSegment: ConstantValues.EntityRoutes.SocialMediaLink,
                entityType: ConstantValues.EntityTypes.SocialMediaLink
            );

            classifiedsGroup.MapBackOfficeMasterEndpoints(
                vertical: ConstantValues.Verticals.Classifieds,
                routeSegment: ConstantValues.EntityRoutes.SocialMediaVideos,
                entityType: ConstantValues.EntityTypes.SocialMediaVideos
            );

            classifiedsGroup.MapBackOfficeMasterEndpoints(
                vertical: ConstantValues.Verticals.Classifieds,
                routeSegment: ConstantValues.EntityRoutes.FaqItem,
                entityType: ConstantValues.EntityTypes.FaqItem
            );

            classifiedsGroup.MapBackOfficeMasterEndpoints(
                vertical: ConstantValues.Verticals.Classifieds,
                routeSegment: ConstantValues.EntityRoutes.ReadyToGrow,
                entityType: ConstantValues.EntityTypes.ReadyToGrow
            );

            classifiedsGroup.MapBackOfficeMasterEndpoints(
                vertical: ConstantValues.Verticals.Classifieds,
                routeSegment: ConstantValues.EntityRoutes.PopularSearch,
                entityType: ConstantValues.EntityTypes.PopularSearch
            );
        }
    }
}
