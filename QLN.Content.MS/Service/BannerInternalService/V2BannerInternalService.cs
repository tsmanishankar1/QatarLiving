using Dapr.Client;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.IService.V2IContent;
using QLN.Common.Infrastructure.Subscriptions;
using System.Text.Json;
using static QLN.Common.Infrastructure.Constants.ConstantValues;

namespace QLN.Content.MS.Service.BannerInternalService
{
    public class V2BannerInternalService : IV2BannerService
    {
        private readonly DaprClient _dapr;
        private readonly ILogger<V2BannerInternalService> _logger;
       
        public V2BannerInternalService(DaprClient dapr, ILogger<V2BannerInternalService> logger)
        {
            _dapr = dapr;
            _logger = logger;
        }
        public async Task<string> CreateBannerAsync(string uid, V2BannerDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                Console.WriteLine("🔧 INTERNAL SERVICE - Creating Banner");

                var id = Guid.NewGuid();

                var banner = new V2BannerDto
                {
                    Id = id,
                    Status = dto.Status,
                    BannerTypeId = dto.BannerTypeId,
                    AnalyticsTrackingId = dto.AnalyticsTrackingId,
                    AltText = dto.AltText,
                    LinkUrl = dto.LinkUrl,
                    Duration = dto.Duration,
                    StartDate=dto.StartDate,
                    EndDate=dto.EndDate,
                    BannerSize = dto.BannerSize,
                    IsDesktopAvailability = dto.IsDesktopAvailability,
                    IsMobileAvailability = dto.IsMobileAvailability,
                    DesktopImage = dto.DesktopImage,
                    MobileImage = dto.MobileImage,
                    Createdby=uid,
                    CreatedAt=DateTime.Now
                };

                Console.WriteLine("Saving Banner:");
                Console.WriteLine(JsonSerializer.Serialize(banner));

                await _dapr.SaveStateAsync(
                    storeName: ConstantValues.V2Content.ContentStoreName,
                    key: banner.Id.ToString(),
                    value: banner,
                    metadata: null,
                    cancellationToken: cancellationToken
                );

                var keys = await _dapr.GetStateAsync<List<string>>(
                    storeName: ConstantValues.V2Content.ContentStoreName,
                    key: ConstantValues.V2Content.BannerIndexKey,
                    metadata: null,
                    consistencyMode: null,
                    cancellationToken: cancellationToken
                ) ?? new List<string>();

                if (!keys.Contains(banner.Id.ToString()))
                {
                    keys.Add(banner.Id.ToString());
                    await _dapr.SaveStateAsync(
                        ConstantValues.V2Content.ContentStoreName,
                        ConstantValues.V2Content.BannerIndexKey,
                        keys,
                        metadata: null,
                        cancellationToken: cancellationToken
                    );
                }

                Console.WriteLine("✅ Banner created successfully.");
                return "Banner created successfully.";
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ EXCEPTION in CreateBannerAsync (INTERNAL)");
                Console.WriteLine(ex.ToString());
                _logger.LogError(ex, "Error creating banner.");
                throw new Exception("Internal error occurred while creating banner.", ex);
            }
        }
        public async Task<string> EditBannerAsync(string uid, V2BannerDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                Console.WriteLine("🔧 INTERNAL SERVICE - Editing Banner");

                var existingBanner = await _dapr.GetStateAsync<V2BannerDto>(
                    ConstantValues.V2Content.ContentStoreName,
                    dto.Id.ToString(),
                    cancellationToken: cancellationToken);

                if (existingBanner == null)
                    throw new ArgumentException("Banner not found.");

                // Update mutable fields
                existingBanner.Status = dto.Status;
                existingBanner.BannerTypeId = dto.BannerTypeId;
                existingBanner.AnalyticsTrackingId = dto.AnalyticsTrackingId;
                existingBanner.AltText = dto.AltText;
                existingBanner.LinkUrl = dto.LinkUrl;
                existingBanner.Duration = dto.Duration;
                existingBanner.StartDate = dto.StartDate;
                existingBanner.EndDate = dto.EndDate;
                existingBanner.BannerSize = dto.BannerSize;
                existingBanner.IsDesktopAvailability = dto.IsDesktopAvailability;
                existingBanner.IsMobileAvailability = dto.IsMobileAvailability;
                existingBanner.DesktopImage = dto.DesktopImage;
                existingBanner.MobileImage = dto.MobileImage;
                existingBanner.UpdatedAt = DateTime.UtcNow;
                existingBanner.Updatedby = uid;

                await _dapr.SaveStateAsync(
                    ConstantValues.V2Content.ContentStoreName,
                    existingBanner.Id.ToString(),
                    existingBanner,
                    cancellationToken: cancellationToken);

                return "Banner updated successfully.";
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ EXCEPTION in EditBannerAsync (INTERNAL)");
                _logger.LogError(ex, "Error editing banner.");
                throw new Exception("Internal error occurred while editing banner.", ex);
            }
        }
        public async Task<string> DeleteBannerAsync(string uid, Guid bannerId, CancellationToken cancellationToken = default)
        {
            try
            {
                Console.WriteLine("🔧 INTERNAL SERVICE - Deleting Banner");

                var existingBanner = await _dapr.GetStateAsync<V2BannerDto>(
                    ConstantValues.V2Content.ContentStoreName,
                    bannerId.ToString(),
                    cancellationToken: cancellationToken);

                if (existingBanner == null)
                    throw new ArgumentException("Banner not found.");

                existingBanner.Status = false; // Soft delete
                existingBanner.Updatedby = uid;
                existingBanner.UpdatedAt = DateTime.UtcNow;

                await _dapr.SaveStateAsync(
                    ConstantValues.V2Content.ContentStoreName,
                    bannerId.ToString(),
                    existingBanner,
                    cancellationToken: cancellationToken);

                return "Banner deleted (status set to false).";
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ EXCEPTION in DeleteBannerAsync (INTERNAL)");
                _logger.LogError(ex, "Error deleting banner.");
                throw new Exception("Internal error occurred while deleting banner.", ex);
            }
        }
        public async Task<V2BannerDto?> GetBannerByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                Console.WriteLine($"🔍 INTERNAL SERVICE - Fetching Banner ID: {id}");

                // Step 1: Check if ID is in index
                var indexKeys = await _dapr.GetStateAsync<List<string>>(
                    storeName: ConstantValues.V2Content.ContentStoreName,
                    key: ConstantValues.V2Content.BannerIndexKey,
                    cancellationToken: cancellationToken
                );

                if (indexKeys == null || !indexKeys.Contains(id.ToString()))
                {
                    Console.WriteLine("⚠️ Banner ID not found in index.");
                    return null;
                }

                // Step 2: Retrieve banner from state store
                var banner = await _dapr.GetStateAsync<V2BannerDto>(
                    storeName: ConstantValues.V2Content.ContentStoreName,
                    key: id.ToString(),
                    cancellationToken: cancellationToken);

                // Step 3: Validate result and filter out soft-deleted banners
                if (banner == null || banner.Id == Guid.Empty || banner.Status == false)
                {
                    Console.WriteLine("⚠️ Banner not found or marked as inactive.");
                    return null;
                }

                Console.WriteLine("✅ Banner Retrieved:");
                Console.WriteLine(JsonSerializer.Serialize(banner, new JsonSerializerOptions { WriteIndented = true }));

                return banner;
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Exception in GetBannerByIdAsync (INTERNAL)");
                _logger.LogError(ex, "Error retrieving banner.");
                throw new Exception("Error occurred while retrieving banner.", ex);
            }
        }
        public async Task<string> CreateBannerTypeAsync(V2BannerTypeDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                Console.WriteLine(" INTERNAL SERVICE - Creating BannerType");

                var bannerTypes = await _dapr.GetStateAsync<List<V2BannerTypeDto>>(
                    ConstantValues.V2Content.ContentStoreName,
                    ConstantValues.V2Content.BannerTypeIndexKey,
                    cancellationToken: cancellationToken
                ) ?? new List<V2BannerTypeDto>();

                dto.Id = Guid.NewGuid();
                bannerTypes.Add(dto);

                await _dapr.SaveStateAsync(
                    ConstantValues.V2Content.ContentStoreName,
                    ConstantValues.V2Content.BannerTypeIndexKey,
                    bannerTypes,
                    cancellationToken: cancellationToken
                );

                return "BannerType created successfully.";
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Error creating BannerType");
                _logger.LogError(ex, "CreateBannerTypeAsync failed");
                throw;
            }
        }
        public async Task<List<V2BannerTypeDto>> GetAllBannerTypesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                Console.WriteLine("🔍 INTERNAL SERVICE - Fetching all BannerTypes");

                var bannerTypes = await _dapr.GetStateAsync<List<V2BannerTypeDto>>(
                    ConstantValues.V2Content.ContentStoreName,
                    ConstantValues.V2Content.BannerTypeIndexKey,
                    cancellationToken: cancellationToken
                );

                if (bannerTypes == null)
                    return new List<V2BannerTypeDto>();

                foreach (var bannerType in bannerTypes)
                {
                    // Add vertical and subvertical name as string
                    bannerType.VerticalName = bannerType.VerticalId.ToString();
                    bannerType.SubVerticalName = bannerType.SubVerticalId?.ToString();

                    // Iterate over Pages (which is a list)
                    if (bannerType.Pages != null)
                    {
                        foreach (var page in bannerType.Pages)
                        {
                            if (page.bannertypes != null)
                            {
                                foreach (var location in page.bannertypes)
                                {
                                    location.BannerDetails = null; // Remove banner details
                                }
                            }
                        }
                    }
                }

                return bannerTypes;
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Error fetching BannerTypes");
                _logger.LogError(ex, "GetAllBannerTypesAsync failed");
                throw;
            }
        }

        public async Task<List<V2BannerTypeDto>?> GetBannerTypesByFilterAsync(
     Vertical verticalId,
     SubVertical? subVerticalId,
     Guid pageId,
     CancellationToken cancellationToken)
        {
            try
            {
                Console.WriteLine($"Fetching BannerTypes by filter: Vertical={verticalId}, SubVertical={subVerticalId}, PageId={pageId}");
                
                // Get all banner types from Dapr
                var allBannerTypes = await _dapr.GetStateAsync<List<V2BannerTypeDto>>(
                    V2Content.ContentStoreName,
                    V2Content.BannerTypeIndexKey,
                    cancellationToken: cancellationToken);

                if (allBannerTypes == null)
                {
                    Console.WriteLine("⚠️ No banner types found in storage.");
                    return null;
                }

                // Match by vertical + subvertical
                var bannerType = allBannerTypes
                    .FirstOrDefault(x => x.VerticalId == verticalId && x.SubVerticalId == subVerticalId);

                if (bannerType == null)
                {
                    Console.WriteLine("⚠️ No matching banner type found.");
                    return null;
                }

                var bannerKeys = await _dapr.GetStateAsync<List<string>>(
     V2Content.ContentStoreName,
     V2Content.BannerIndexKey,
     cancellationToken: cancellationToken) ?? new List<string>();


                // Get all active banners
                var allBanners = new List<V2BannerDto>();
                foreach (var bannerKey in bannerKeys)
                {
                    var banner = await _dapr.GetStateAsync<V2BannerDto>(
     V2Content.ContentStoreName,
     bannerKey,
     cancellationToken: cancellationToken);


                    if (banner?.Status == true)
                    {
                        allBanners.Add(banner);
                    }
                }

                // Filter just the page we want
                Console.WriteLine(JsonSerializer.Serialize(bannerType));
                var matchingPage = bannerType.Pages?.FirstOrDefault(p => p.Id == pageId);
                if (matchingPage == null)
                {
                    Console.WriteLine("⚠️ No matching page found.");
                    return null;
                }

                // Fill in banners for that page
                if (matchingPage.bannertypes != null)
                {
                    foreach (var location in matchingPage.bannertypes)
                    {
                        var matchingBanner = allBanners.FindAll(b => b.BannerTypeId == location.Id && b.EndDate >= DateOnly.FromDateTime(DateTime.Now));
                        location.BannerDetails = matchingBanner ?? [];

                        Console.WriteLine(matchingBanner != null
                            ? $"✅ Found banner for location {location.Id}"
                            : $"⚠️ No banner for location {location.Id}");
                    }
                }

                // Wrap back into the full structure
                var result = new V2BannerTypeDto
                {
                    Id = bannerType.Id,
                    VerticalId = bannerType.VerticalId,
                    VerticalName = bannerType.VerticalName,
                    SubVerticalId = bannerType.SubVerticalId,
                    SubVerticalName = bannerType.SubVerticalName,
                    Pages = new List<V2BannerPageLocationDto> { matchingPage }
                };

                return new List<V2BannerTypeDto> { result };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Exception in GetBannerTypesByFilterAsync");
                throw new Exception("Error occurred while retrieving banner types by filter.", ex);
            }
        }
    //    public async Task<List<V2BannerTypeDto>?> GetBOBannerTypesByFilterAsync(
    //Vertical verticalId,
    //SubVertical? subVerticalId,
    //string sortOrder = "desc", string? searchTerm = null,
    //CancellationToken cancellationToken)
    //    {
    //        try
    //        {
    //            Console.WriteLine($"Fetching BannerTypes by filter: Vertical={verticalId}, SubVertical={subVerticalId}, SortOrder={sortOrder} ,SearchTerm={searchTerm}");

    //            // Get all banner types from Dapr
    //            var allBannerTypes = await _dapr.GetStateAsync<List<V2BannerTypeDto>>(
    //                V2Content.ContentStoreName,
    //                V2Content.BannerTypeIndexKey,
    //                cancellationToken: cancellationToken);

    //            if (allBannerTypes == null)
    //            {
    //                Console.WriteLine("⚠️ No banner types found in storage.");
    //                return null;
    //            }

    //            // Match by vertical + subvertical
    //            var bannerType = allBannerTypes
    //                .FirstOrDefault(x => x.VerticalId == verticalId && x.SubVerticalId == subVerticalId);

    //            if (bannerType == null)
    //            {
    //                Console.WriteLine("⚠️ No matching banner type found.");
    //                return null;
    //            }

    //            var bannerKeys = await _dapr.GetStateAsync<List<string>>(
    // V2Content.ContentStoreName,
    // V2Content.BannerIndexKey,
    // cancellationToken: cancellationToken) ?? new List<string>();


    //            // Get all active banners
    //            var allBanners = new List<V2BannerDto>();
    //            foreach (var bannerKey in bannerKeys)
    //            {
    //                var banner = await _dapr.GetStateAsync<V2BannerDto>(
    // V2Content.ContentStoreName,
    // bannerKey,
    // cancellationToken: cancellationToken);


    //                if (banner?.Status == true)
    //                {
    //                    allBanners.Add(banner);
    //                }
    //            }

    //            // Filter just the page we want
    //            Console.WriteLine(JsonSerializer.Serialize(bannerType));
    //            var bannerPages = bannerType.Pages;
    //            if (!string.IsNullOrWhiteSpace(searchTerm))
    //            {
    //                string lowerSearch = searchTerm.ToLower();
    //                bannerType.Pages = bannerPages.Where(r =>
    //                    (!string.IsNullOrEmpty(r.) && r.Post.ToLower().Contains(lowerSearch)) ||
    //                    (!string.IsNullOrEmpty(r.Comment) && r.Comment.ToLower().Contains(lowerSearch)) ||
    //                    (!string.IsNullOrEmpty(r.Reporter) && r.Reporter.ToLower().Contains(lowerSearch)) ||
    //                    (!string.IsNullOrEmpty(r.UserName) && r.UserName.ToLower().Contains(lowerSearch))
    //                ).ToList();
    //            }

    //            // Apply sorting
    //            var sortedResult = sortOrder?.ToLower() switch
    //            {
    //                "asc" => bannerPages.OrderBy(r => r.ReportDate),
    //                "desc" => bannerPages.OrderByDescending(r => r.ReportDate),
    //                _ => bannerPages.OrderByDescending(r => r.ReportDate)
    //            };

    //            if (matchingPage == null)
    //            {
    //                Console.WriteLine("⚠️ No matching page found.");
    //                return null;
    //            }

    //            // Fill in banners for that page
    //            if (matchingPage.bannertypes != null)
    //            {
    //                foreach (var location in matchingPage.bannertypes)
    //                {
    //                    var matchingBanner = allBanners.FirstOrDefault(b => b.BannerTypeId == location.Id);
    //                    location.BannerDetails = matchingBanner;

    //                    Console.WriteLine(matchingBanner != null
    //                        ? $"✅ Found banner for location {location.Id}"
    //                        : $"⚠️ No banner for location {location.Id}");
    //                }
    //            }

    //            // Wrap back into the full structure
    //            var result = new V2BannerTypeDto
    //            {
    //                Id = bannerType.Id,
    //                VerticalId = bannerType.VerticalId,
    //                VerticalName = bannerType.VerticalName,
    //                SubVerticalId = bannerType.SubVerticalId,
    //                SubVerticalName = bannerType.SubVerticalName,
    //                Pages = new List<V2BannerPageLocationDto> { matchingPage }
    //            };

    //            return new List<V2BannerTypeDto> { result };
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "❌ Exception in GetBannerTypesByFilterAsync");
    //            throw new Exception("Error occurred while retrieving banner types by filter.", ex);
    //        }
    //    }













    }
}
