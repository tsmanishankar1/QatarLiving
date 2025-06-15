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
            app.MapGet("/api/landing/services", async ([FromServices] ISearchService searchSvc) =>
            {
                var vertical = ConstantValues.Verticals.Services;

                var bannerTask = FetchSegmentAsync(searchSvc, vertical, ConstantValues.EntityTypes.HeroBanner);
                var featuredServicesTask = FetchSegmentAsync(searchSvc, vertical, ConstantValues.EntityTypes.FeaturedServices);
                var featuredCatTask = FetchSegmentAsync(searchSvc, vertical, ConstantValues.EntityTypes.FeaturedCategory);
                var categoriesTask = FetchSegmentAsync(searchSvc, vertical, ConstantValues.EntityTypes.Category);
                var seasonalTask = FetchSegmentAsync(searchSvc, vertical, ConstantValues.EntityTypes.SeasonalPick);
                var socialPostTask = FetchSegmentAsync(searchSvc, vertical, ConstantValues.EntityTypes.SocialPostSection);
                var socialLinksTask = FetchSegmentAsync(searchSvc, vertical, ConstantValues.EntityTypes.SocialMediaLink);
                var socialVideosTask = FetchSegmentAsync(searchSvc, vertical, ConstantValues.EntityTypes.SocialMediaVideos);
                var faqTask = FetchSegmentAsync(searchSvc, vertical, ConstantValues.EntityTypes.FaqItem);
                var ctaTask = FetchSegmentAsync(searchSvc, vertical, ConstantValues.EntityTypes.ReadyToGrow);
                var popularSearchTask = FetchSegmentAsync(searchSvc, vertical, ConstantValues.EntityTypes.PopularSearch);

                await Task.WhenAll(
                    bannerTask, featuredServicesTask, featuredCatTask, categoriesTask,
                    seasonalTask, socialPostTask, socialLinksTask, socialVideosTask,
                    faqTask, ctaTask, popularSearchTask
                );

                var basicServices = (await featuredServicesTask)
                    .OrderBy(i => i.Order)
                    .ToList();

                var enrichedServices = await Task.WhenAll(
                    basicServices.Select(async idx => new {
                        Index = idx,
                        ItemDetails = await searchSvc.GetByIdAsync<ServicesIndex>(
                            vertical,
                            idx.AdId ?? throw new InvalidOperationException("AdId missing")
                        )
                    })
                );

                var result = new LandingPageDto
                {
                    HeroBanner = await bannerTask,
                    FeaturedServices = enrichedServices.Cast<object>(),
                    FeaturedCategories = await featuredCatTask,
                    Categories = await categoriesTask,
                    SeasonalPicks = await seasonalTask,
                    SocialPostDetail = await socialPostTask,
                    SocialLinks = await socialLinksTask,
                    SocialMediaVideos = await socialVideosTask,
                    FaqItems = await faqTask,
                    ReadyToGrow = await ctaTask,
                    PopularSearches = BuildHierarchy((await popularSearchTask).ToList(), null)
                };

                return TypedResults.Ok(result);
            })
            .WithName("GetServicesLandingPage")
            .WithTags("LandingPage")
            .Produces<object>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status500InternalServerError);

            app.MapGet("/api/landing/classifieds", async ([FromServices] ISearchService searchSvc) =>
            {
                var vertical = ConstantValues.Verticals.Classifieds;

                var bannerTask = FetchSegmentAsync(searchSvc, vertical, ConstantValues.EntityTypes.HeroBanner);
                var featuredItemsTask = FetchSegmentAsync(searchSvc, vertical, ConstantValues.EntityTypes.FeaturedItems);
                var featuredCatTask = FetchSegmentAsync(searchSvc, vertical, ConstantValues.EntityTypes.FeaturedCategory);
                var featuredStoresTask = FetchSegmentAsync(searchSvc, vertical, ConstantValues.EntityTypes.FeaturedStores);
                var categoriesTask = FetchSegmentAsync(searchSvc, vertical, ConstantValues.EntityTypes.Category);
                var seasonalTask = FetchSegmentAsync(searchSvc, vertical, ConstantValues.EntityTypes.SeasonalPick);
                var socialPostTask = FetchSegmentAsync(searchSvc, vertical, ConstantValues.EntityTypes.SocialPostSection);
                var socialLinksTask = FetchSegmentAsync(searchSvc, vertical, ConstantValues.EntityTypes.SocialMediaLink);
                var socialVideosTask = FetchSegmentAsync(searchSvc, vertical, ConstantValues.EntityTypes.SocialMediaVideos);
                var faqTask = FetchSegmentAsync(searchSvc, vertical, ConstantValues.EntityTypes.FaqItem);
                var ctaTask = FetchSegmentAsync(searchSvc, vertical, ConstantValues.EntityTypes.ReadyToGrow);
                var popularSearchTask = FetchSegmentAsync(searchSvc, vertical, ConstantValues.EntityTypes.PopularSearch);

                await Task.WhenAll(
                    bannerTask, featuredItemsTask, featuredCatTask, featuredStoresTask,
                    categoriesTask, seasonalTask, socialPostTask, socialLinksTask,
                    socialVideosTask, faqTask, ctaTask, popularSearchTask
                );

                var basicItems = (await featuredItemsTask)
                    .OrderBy(i => i.Order)
                    .ToList();

                var enrichedItems = await Task.WhenAll(
                    basicItems.Select(async idx => new {
                        Index = idx,
                        Detail = await searchSvc.GetByIdAsync<ClassifiedsIndex>(
                            vertical,
                            idx.AdId ?? throw new InvalidOperationException("AdId missing")
                        )
                    })
                );

                var result = new LandingPageDto
                {
                    HeroBanner = await bannerTask,
                    FeaturedItems = enrichedItems.Cast<object>(),
                    FeaturedCategories = await featuredCatTask,
                    FeaturedStores = await featuredStoresTask,
                    Categories = await categoriesTask,
                    SeasonalPicks = await seasonalTask,
                    SocialPostDetail = await socialPostTask,
                    SocialLinks = await socialLinksTask,
                    SocialMediaVideos = await socialVideosTask,
                    FaqItems = await faqTask,
                    ReadyToGrow = await ctaTask,
                    PopularSearches = BuildHierarchy((await popularSearchTask).ToList(), null)
                };

                return TypedResults.Ok(result);
            })
            .WithName("GetClassifiedsLandingPage")
            .WithTags("LandingPage")
            .Produces<object>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status500InternalServerError);
        }

        private static Task<IEnumerable<LandingBackOfficeIndex>> FetchSegmentAsync(
            ISearchService searchSvc,
            string vertical,
            string entityType)
        {
            var sr = new CommonSearchRequest
            {
                Top = 100,
                Filters = new Dictionary<string, object>
                {
                    { "Vertical",   vertical },
                    { "EntityType", entityType }
                }
            };

            return FetchDocsFromIndexAsync(
                searchSvc,
                ConstantValues.LandingBackOffice,
                sr,
                vertical,
                entityType
            );
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

        private static IEnumerable<object> BuildHierarchy(
            List<LandingBackOfficeIndex> items,
            string? parentId)
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
                })
                .ToList<object>();
        }
    }
}
