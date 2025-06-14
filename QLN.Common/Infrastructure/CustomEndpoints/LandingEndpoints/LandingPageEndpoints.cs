using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.IService.ISearchService;

namespace QLN.Common.Infrastructure.CustomEndpoints.LandingEndpoints
{
    public static class LandingPageEndpoints
    {
        public static void MapLandingPageEndpoints(this WebApplication app)
        {
            app.MapGet("/api/landing/services", async (
                    [FromServices] ISearchService searchSvc
                ) =>
            {
                var vertical = ConstantValues.Verticals.Services;

                Task<IEnumerable<LandingBackOfficeIndex>> FetchSegmentAsync(string entityType)
                {
                    var sr = new CommonSearchRequest
                    {
                        Top = 100,
                        Filters = new Dictionary<string, object>
                        {
                            { "Vertical", vertical },
                            { "EntityType", entityType }
                        }
                    };

                    return FetchDocsFromIndexAsync(searchSvc, ConstantValues.LandingBackOffice, sr, vertical, entityType);
                }

                var bannerTask = FetchSegmentAsync(ConstantValues.EntityTypes.HeroBanner);
                var featuredServicesTask = FetchSegmentAsync(ConstantValues.EntityTypes.FeaturedServices);
                var featuredTask = FetchSegmentAsync(ConstantValues.EntityTypes.FeaturedCategory);
                var categoriesTask = FetchSegmentAsync(ConstantValues.EntityTypes.Category);
                var seasonalTask = FetchSegmentAsync(ConstantValues.EntityTypes.SeasonalPick);
                var socialPostTask = FetchSegmentAsync(ConstantValues.EntityTypes.SocialPostSection);
                var socialMediaLinkTask = FetchSegmentAsync(ConstantValues.EntityTypes.SocialMediaLink);
                var socialMediaVideosTask = FetchSegmentAsync(ConstantValues.EntityTypes.SocialMediaVideos);
                var faqTask = FetchSegmentAsync(ConstantValues.EntityTypes.FaqItem);
                var ctaTask = FetchSegmentAsync(ConstantValues.EntityTypes.ReadyToGrow);
                var popularSearchTask = FetchSegmentAsync(ConstantValues.EntityTypes.PopularSearch);

                await Task.WhenAll(bannerTask, featuredServicesTask, featuredTask, categoriesTask, seasonalTask,
                                   socialPostTask, socialMediaLinkTask, socialMediaVideosTask,
                                   faqTask, ctaTask, popularSearchTask);

                var dto = new LandingPageDto
                {
                    HeroBanner = await bannerTask,
                    FeaturedServices = await featuredServicesTask,
                    FeaturedCategories = await featuredTask,
                    Categories = await categoriesTask,
                    SeasonalPicks = await seasonalTask,
                    SocialPostDetail = await socialPostTask,
                    SocialLinks = await socialMediaLinkTask,
                    SocialMediaVideos = await socialMediaVideosTask,
                    FaqItems = await faqTask,
                    ReadyToGrow = await ctaTask,
                    PopularSearches = BuildHierarchy((await popularSearchTask).ToList(), null)
                };

                return TypedResults.Ok(dto);
            })
            .WithName("GetServicesLandingPage")
            .WithTags("LandingPage")
            .Produces<LandingPageDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status500InternalServerError)
            .WithSummary("Get all landing‐page data for Services")
            .WithDescription("Returns featured categories, categories, seasonal picks, social links, faqs, CTAs, and popular searches for the Services landing page.");


            app.MapGet("/api/landing/classifieds", async (
                    [FromServices] ISearchService searchSvc
                ) =>
            {
                var vertical = ConstantValues.Verticals.Classifieds;

                Task<IEnumerable<LandingBackOfficeIndex>> FetchSegmentAsync(string entityType)
                {
                    var sr = new CommonSearchRequest
                    {
                        Top = 100,
                        Filters = new Dictionary<string, object>
                        {
                            { "Vertical", vertical },
                            { "EntityType", entityType }
                        }
                    };

                    return FetchDocsFromIndexAsync(searchSvc, ConstantValues.LandingBackOffice, sr, vertical, entityType);
                }

                var bannerTask = FetchSegmentAsync(ConstantValues.EntityTypes.HeroBanner);
                var featuredItemsTask = FetchSegmentAsync(ConstantValues.EntityTypes.FeaturedItems);
                var featuredCategoryTask = FetchSegmentAsync(ConstantValues.EntityTypes.FeaturedCategory);
                var featuredStoresTask = FetchSegmentAsync(ConstantValues.EntityTypes.FeaturedStores);
                var categoriesTask = FetchSegmentAsync(ConstantValues.EntityTypes.Category);
                var seasonalTask = FetchSegmentAsync(ConstantValues.EntityTypes.SeasonalPick);
                var socialPostTask = FetchSegmentAsync(ConstantValues.EntityTypes.SocialPostSection);
                var socialMediaLinkTask = FetchSegmentAsync(ConstantValues.EntityTypes.SocialMediaLink);
                var socialMediaVideosTask = FetchSegmentAsync(ConstantValues.EntityTypes.SocialMediaVideos);
                var faqTask = FetchSegmentAsync(ConstantValues.EntityTypes.FaqItem);
                var ctaTask = FetchSegmentAsync(ConstantValues.EntityTypes.ReadyToGrow);
                var popularSearchTask = FetchSegmentAsync(ConstantValues.EntityTypes.PopularSearch);

                await Task.WhenAll(bannerTask, featuredItemsTask, featuredCategoryTask, featuredStoresTask, categoriesTask,
                                   seasonalTask, socialPostTask, socialMediaLinkTask, socialMediaVideosTask,
                                   faqTask, ctaTask, popularSearchTask);

                var dto = new LandingPageDto
                {
                    HeroBanner = await bannerTask,
                    FeaturedItems = await featuredItemsTask,
                    FeaturedCategories = await featuredCategoryTask,
                    FeaturedStores = await featuredStoresTask,
                    Categories = await categoriesTask,
                    SeasonalPicks = await seasonalTask,
                    SocialPostDetail = await socialPostTask,
                    SocialLinks = await socialMediaLinkTask,
                    SocialMediaVideos = await socialMediaVideosTask,
                    FaqItems = await faqTask,
                    ReadyToGrow = await ctaTask,
                    PopularSearches = BuildHierarchy((await popularSearchTask).ToList(), null)
                };

                return TypedResults.Ok(dto);
            })
            .WithName("GetClassifiedsLandingPage")
            .WithTags("LandingPage")
            .Produces<LandingPageDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status500InternalServerError)
            .WithSummary("Get all landing‐page data for Classifieds")
            .WithDescription("Returns featured categories, categories, seasonal picks, social links, faqs, CTAs, and popular searches for the Classifieds landing page.");
        }

        public static async Task<IEnumerable<LandingBackOfficeIndex>> FetchDocsFromIndexAsync(
            ISearchService searchSvc,
            string indexName,
            CommonSearchRequest request,
            string expectedVertical,
            string expectedEntityType)
        {
            try
            {
                var response = await searchSvc.SearchAsync(indexName, request);
                var all = response.MasterItems ?? new List<LandingBackOfficeIndex>();

                return all
                    .Where(d => d.Vertical == expectedVertical && d.EntityType == expectedEntityType)
                    .ToList();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Error fetching '{expectedEntityType}' for vertical '{expectedVertical}'", ex);
            }
        }

        private static IEnumerable<object> BuildHierarchy(List<LandingBackOfficeIndex> items, string? parentId)
        {
            return items
                .Where(i => i.ParentId == parentId)
                .OrderBy(i => i.Order)
                .Select(i => new
                {
                    i.Id,
                    i.Title,
                    i.ParentId,
                    i.Vertical,
                    i.EntityType,
                    i.Order,
                    i.RediectUrl,
                    i.ImageUrl,
                    i.IsActive,
                    Children = BuildHierarchy(items, i.Id)
                }).ToList<object>();
        }
    }
}
