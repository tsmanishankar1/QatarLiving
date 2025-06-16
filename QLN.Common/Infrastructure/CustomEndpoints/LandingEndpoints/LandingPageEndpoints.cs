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

                var indexEntries = (await featuredServicesTask)
                    .OrderBy(ix => ix.Order)
                    .ToList();

                var featuredServices = new List<LandingFeaturedItemDto>();
                foreach (var ix in indexEntries)
                {
                    var detail = await searchSvc.GetByIdAsync<ServicesIndex>(
                        vertical,
                        ix.AdId ?? throw new InvalidOperationException("AdId missing")
                    );
                    if (detail == null) continue;

                    featuredServices.Add(new LandingFeaturedItemDto
                    {
                        Title = detail.Title,
                        Description = detail.Description,
                        Category = detail.Category,
                        Price = detail.Price,
                        Order = ix.Order,
                        IsFeatured = true,
                        ImageURLs = detail.Images
                    });
                }

                var dto = new LandingPageDto
                {
                    HeroBanner = await bannerTask,
                    FeaturedServices = featuredServices,
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

                return TypedResults.Ok(dto);
            })
            .WithName("GetServicesLandingPage")
            .WithTags("LandingPage")
            .Produces<LandingPageDto>(StatusCodes.Status200OK)
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

                var indexEntries = (await featuredItemsTask)
                    .OrderBy(ix => ix.Order)
                    .ToList();

                var featuredItems = new List<LandingFeaturedItemDto>();
                foreach (var ix in indexEntries)
                {
                    var detail = await searchSvc.GetByIdAsync<ClassifiedsIndex>(
                        vertical,
                        ix.AdId ?? throw new InvalidOperationException("AdId missing")
                    );
                    if (detail == null) continue;

                    featuredItems.Add(new LandingFeaturedItemDto
                    {
                        Title = detail.Title,
                        Description = detail.Description,
                        Category = detail.Category,
                        Price = detail.Price,
                        Order = ix.Order,
                        Color = detail.Colour,
                        Location = detail.Location,
                        IsFeatured = detail.IsFeatured,
                        ImageURLs = detail.Images
                    });
                }

                var dto = new LandingPageDto
                {
                    HeroBanner = await bannerTask,
                    FeaturedItems = featuredItems,
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

                return TypedResults.Ok(dto);
            })
            .WithName("GetClassifiedsLandingPage")
            .WithTags("LandingPage")
            .Produces<LandingPageDto>(StatusCodes.Status200OK)
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
