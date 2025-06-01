// File: QLN.Common.Infrastructure.CustomEndpoints.LandingPageEndpoints.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;         // for WebApplication, RouteHandlerBuilder
using Microsoft.AspNetCore.Http;            // for IResult, TypedResults, StatusCodes
using Microsoft.AspNetCore.Mvc;             // for [FromServices]
using QLN.Common.DTO_s;                     // for LandingPageDto, BackofficemasterIndex
using QLN.Common.Infrastructure.Constants;  // for ConstantValues and BackOfficeConstants
using QLN.Common.Infrastructure.IService.ISearchService;  // for ISearchService

namespace QLN.Common.Infrastructure.CustomEndpoints.LandingEndpoints
{
    /// <summary>
    /// Maps two “landing‐page” endpoints:
    ///   • GET /api/landing/services     → Returns a LandingPageDto for the Services vertical
    ///   • GET /api/landing/classifieds  → Returns a LandingPageDto for the Classifieds vertical
    ///
    /// Each handler calls ISearchService.SearchAsync against the “backofficemaster” index,
    /// filtering by Vertical and EntityType for each segment, then bundles all results into
    /// a single LandingPageDto.
    /// </summary>
    public static class LandingPageEndpoints
    {
        public static void MapLandingPageEndpoints(this WebApplication app)
        {
            // ──────────────────────────────────────────────────────────
            // 1) GET /api/landing/services
            //    Fetch all back‐office segments for vertical = "services"
            // ──────────────────────────────────────────────────────────
            app.MapGet("/api/landing/services", async (
                    [FromServices] ISearchService searchSvc
                ) =>
            {
                // The vertical we’re targeting:
                var vertical = ConstantValues.Verticals.Services;

                // Helper: build a search request for a single entityType
                Task<IEnumerable<BackofficemasterIndex>> FetchSegmentAsync(string entityType)
                {
                    var sr = new CommonSearchRequest
                    {
                        Top = 100,
                        Filters = new Dictionary<string, object>
                        {
                            // filter by Vertical and EntityType in the back‐office index
                            { "Vertical",   vertical     },
                            { "EntityType", entityType   }
                        }
                    };

                    return FetchDocsFromIndexAsync(searchSvc, ConstantValues.backofficemaster, sr,
                        vertical, entityType);
                }

                // Fire off one call per segment
                var featuredTask = FetchSegmentAsync( ConstantValues.EntityTypes.FeaturedCategory);
                var categoriesTask = FetchSegmentAsync( ConstantValues.EntityTypes.Category);
                var seasonalTask = FetchSegmentAsync( ConstantValues.EntityTypes.SeasonalPick);
                var socialTask = FetchSegmentAsync( ConstantValues.EntityTypes.SocialMediaLink);
                var faqTask = FetchSegmentAsync( ConstantValues.EntityTypes.FaqItem);
                var ctaTask = FetchSegmentAsync( ConstantValues.EntityTypes.CallToAction);

                await Task.WhenAll(featuredTask, categoriesTask, seasonalTask, socialTask, faqTask, ctaTask);

                var dto = new LandingPageDto
                {
                    FeaturedCategories = await featuredTask,
                    Categories = await categoriesTask,
                    SeasonalPicks = await seasonalTask,
                    SocialLinks = await socialTask,
                    FaqItems = await faqTask,
                    ReadyToGrow = await ctaTask
                };

                return TypedResults.Ok(dto);
            })
            .WithName("GetServicesLandingPage")
            .WithTags("LandingPage")
            .Produces<LandingPageDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status500InternalServerError)
            .WithSummary("Get all landing‐page data for Services")
            .WithDescription("Returns featured categories, categories, seasonal picks, social links, faqs, and CTAs for the Services landing page.");

            // ──────────────────────────────────────────────────────────
            // 2) GET /api/landing/classifieds
            //    Fetch all back‐office segments for vertical = "classifieds"
            // ──────────────────────────────────────────────────────────
            app.MapGet("/api/landing/classifieds", async (
                    [FromServices] ISearchService searchSvc
                ) =>
            {
                var vertical =  ConstantValues.Verticals.Classifieds;

                Task<IEnumerable<BackofficemasterIndex>> FetchSegmentAsync(string entityType)
                {
                    var sr = new CommonSearchRequest
                    {
                        Top = 100,
                        Filters = new Dictionary<string, object>
                        {
                            { "Vertical",   vertical     },
                            { "EntityType", entityType   }
                        }
                    };

                    return FetchDocsFromIndexAsync(searchSvc, ConstantValues.backofficemaster, sr,
                        vertical, entityType);
                }

                var featuredTask = FetchSegmentAsync( ConstantValues.EntityTypes.FeaturedCategory);
                var categoriesTask = FetchSegmentAsync( ConstantValues.EntityTypes.Category);
                var seasonalTask = FetchSegmentAsync( ConstantValues.EntityTypes.SeasonalPick);
                var socialTask = FetchSegmentAsync( ConstantValues.EntityTypes.SocialMediaLink);
                var faqTask = FetchSegmentAsync( ConstantValues.EntityTypes.FaqItem);
                var ctaTask = FetchSegmentAsync( ConstantValues.EntityTypes.CallToAction);

                await Task.WhenAll(featuredTask, categoriesTask, seasonalTask, socialTask, faqTask, ctaTask);

                var dto = new LandingPageDto
                {
                    FeaturedCategories = await featuredTask,
                    Categories = await categoriesTask,
                    SeasonalPicks = await seasonalTask,
                    SocialLinks = await socialTask,
                    FaqItems = await faqTask,
                    ReadyToGrow = await ctaTask
                };

                return TypedResults.Ok(dto);
            })
            .WithName("GetClassifiedsLandingPage")
            .WithTags("LandingPage")
            .Produces<LandingPageDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status500InternalServerError)
            .WithSummary("Get all landing‐page data for Classifieds")
            .WithDescription("Returns featured categories, categories, seasonal picks, social links, faqs, and CTAs for the Classifieds landing page.");
        }

        /// <summary>
        /// Helper to call ISearchService.SearchAsync and return
        /// exactly those BackofficemasterIndex docs that match
        /// both “Vertical” and “EntityType.”
        /// </summary>
        private static async Task<IEnumerable<BackofficemasterIndex>> FetchDocsFromIndexAsync(
            ISearchService searchSvc,
            string indexName,
            CommonSearchRequest request,
            string expectedVertical,
            string expectedEntityType)
        {
            try
            {
                // Call the search service
                var response = await searchSvc.SearchAsync(indexName, request);

                // If the search returned MasterItems, filter again just in case
                var all = response.MasterItems ?? new List<BackofficemasterIndex>();
                return all
                    .Where(d => d.Vertical == expectedVertical && d.EntityType == expectedEntityType)
                    .ToList();
            }
            catch (Exception ex)
            {
                // Log or rethrow as needed. For brevity, we rethrow here.
                throw new InvalidOperationException(
                    $"Error fetching '{expectedEntityType}' for vertical '{expectedVertical}'", ex);
            }
        }
    }
}
