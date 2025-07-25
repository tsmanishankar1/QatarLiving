using Dapr.Client;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.IService.V2IContent;
using QLN.Common.Infrastructure.Subscriptions;
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
        public string ValidateReorderProcess(List<Guid> source, List<Guid> target)
        {
            if (source == null || source.Count == 0)
                return "Source list is empty or null.";

            if (target == null || target.Count == 0)
                return "Target list is empty or null.";

            if (source.Count != target.Count)
                return "Source and target lists do not contain the same number of elements.";

            var targetSet = new HashSet<Guid>(target);
            if (!source.All(targetSet.Contains))
                return "Source and target lists do not contain the same elements.";

            return string.Empty;
        }
        public async Task<string> ReorderAsync(Vertical verticalId, SubVertical? subVerticalId,Guid pageId, List<Guid> banners,CancellationToken cancellationToken = default)
        {
            try
            {
               

                var allBannerTypes = await _dapr.GetStateAsync<List<V2BannerTypeDto>>(
                   V2Content.ContentStoreName,
                   V2Content.BannerTypeIndexKey,
                   cancellationToken: cancellationToken);

                    if (allBannerTypes == null)
                    {
                        
                        return null;
                    }

                    var bannerType = allBannerTypes
                        .FirstOrDefault(x => x.VerticalId == verticalId && x.SubVerticalId == subVerticalId);

                    if (bannerType == null)
                    {
                      
                        return null;
                    }
                    var matchingPage = bannerType.Pages?.FirstOrDefault(p => p.Id == pageId);
                    if (matchingPage == null)
                    {
                        
                        return null;
                    }
                var banner = await _dapr.GetStateAsync<V2BannerDto>(
                 V2Content.ContentStoreName,
                 banners[0].ToString(),
               cancellationToken: cancellationToken);

                if (banner == null) {
                   
                    return null;
                }
                if (matchingPage.bannertypes != null)
                    {
                   
                    foreach (var location in matchingPage.bannertypes)
                        {
                            if (location.Id == banner.BannerTypeId)
                            {
                            if (string.IsNullOrEmpty(ValidateReorderProcess(location?.BannerIds ?? [], banners)))
                            {
                                location?.BannerIds?.Clear();
                                location?.BannerIds?.AddRange(banners);
                            }
                           
                                break;
                        }
                        }
                    };
                    await _dapr.SaveStateAsync(
                            V2Content.ContentStoreName,
                            V2Content.BannerTypeIndexKey,
                            allBannerTypes,
                            metadata: null,
                            cancellationToken: cancellationToken
                        );
                return "Banner slot reordered successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating banner.");
                throw new Exception("Internal error occurred while creating banner.", ex);
            }
        }
        public async Task<string> CreateBannerAsync(string uid, V2CreateBannerDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var allBannerTypes = await _dapr.GetStateAsync<List<V2BannerTypeDto>>(
                    V2Content.ContentStoreName,
                    V2Content.BannerTypeIndexKey,
                    cancellationToken: cancellationToken);

                if (allBannerTypes == null)
                {
                    _logger.LogWarning("No banner types found in storage.");
                    return "No banner types configuration found.";
                }

                var createdBanners = new List<string>();
                bool hasUpdates = false;

                foreach (var bannerTypeRequest in dto.BannerTypeIds)
                {
                    var id = Guid.NewGuid();
                    var banner = new V2BannerDto
                    {
                        Id = id,
                        Status = dto.Status,
                        BannerTypeId = bannerTypeRequest.BannerTypeId,
                        AnalyticsTrackingId = dto.AnalyticsTrackingId,
                        AltText = dto.AltText,
                        LinkUrl = dto.LinkUrl,
                        Duration = dto.Duration,
                        StartDate = dto.StartDate,
                        EndDate = dto.EndDate,
                        BannerSize = dto.BannerSize,
                        IsDesktopAvailability = dto.IsDesktopAvailability,
                        IsMobileAvailability = dto.IsMobileAvailability,
                        DesktopImage = dto.DesktopImage,
                        MobileImage = dto.MobileImage,
                        Createdby = uid,
                        CreatedAt = DateTime.Now
                    };

                   
                    await _dapr.SaveStateAsync(
                        storeName: ConstantValues.V2Content.ContentStoreName,
                        key: banner.Id.ToString(),
                        value: banner,
                        metadata: null,
                        cancellationToken: cancellationToken
                    );

                   
                    var bannerType = allBannerTypes
                        .FirstOrDefault(x => x.VerticalId == bannerTypeRequest.VerticalId &&
                                           x.SubVerticalId == bannerTypeRequest.SubVerticalId);

                    if (bannerType == null)
                    {
                        _logger.LogWarning("No matching banner type found for Vertical: {VerticalId}, SubVertical: {SubVerticalId}",
                            bannerTypeRequest.VerticalId, bannerTypeRequest.SubVerticalId);
                        continue;
                    }

                  
                    var matchingPage = bannerType.Pages?.FirstOrDefault(p => p.Id == bannerTypeRequest.PageId);
                    if (matchingPage == null)
                    {
                        _logger.LogWarning("No matching page found for PageId: {PageId}", bannerTypeRequest.PageId);
                        continue;
                    }

                  
                    if (matchingPage.bannertypes != null)
                    {
                        foreach (var location in matchingPage.bannertypes)
                        {
                            if (location.Id == bannerTypeRequest.BannerTypeId)
                            {
                                location.BannerIds ??= new List<Guid>();
                                location.BannerIds.Add(id);
                                hasUpdates = true;
                                createdBanners.Add($"Banner {id} added to BannerType {bannerTypeRequest.BannerTypeId}");
                                break;
                            }
                        }
                    }
                }
                if (hasUpdates)
                {
                    await _dapr.SaveStateAsync(
                        V2Content.ContentStoreName,
                        V2Content.BannerTypeIndexKey,
                        allBannerTypes,
                        metadata: null,
                        cancellationToken: cancellationToken
                    );
                }

                var successMessage = createdBanners.Any()
                    ? $"Banners created successfully. Details: {string.Join(", ", createdBanners)}"
                    : "Banners were created but no valid banner type configurations were found to associate them with.";

                return successMessage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating banner.");
                throw new Exception("Internal error occurred while creating banner.", ex);
            }
        }
        public async Task<string> EditBannerAsync(string uid, V2BannerDto dto, CancellationToken cancellationToken = default)
        {
            try
            {

                var existingBanner = await _dapr.GetStateAsync<V2BannerDto>(
                    ConstantValues.V2Content.ContentStoreName,
                    dto.Id.ToString(),
                    cancellationToken: cancellationToken);

                if (existingBanner == null)
                    throw new ArgumentException("Banner not found.");
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
                _logger.LogError(ex, "Error editing banner.");
                throw new Exception("Internal error occurred while editing banner.", ex);
            }
        }
        public async Task<string> DeleteBannerAsync(string uid, Guid bannerId, CancellationToken cancellationToken = default)
        {
            try
            {
                var existingBanner = await _dapr.GetStateAsync<V2BannerDto>(
                    ConstantValues.V2Content.ContentStoreName,
                    bannerId.ToString(),
                    cancellationToken: cancellationToken);

                if (existingBanner == null)
                    throw new ArgumentException("Banner not found.");

                existingBanner.Status = false; 
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
                _logger.LogError(ex, "Error deleting banner.");
                throw new Exception("Internal error occurred while deleting banner.", ex);
            }
        }
        public async Task<V2BannerDto?> GetBannerByIdAsync(Guid id, CancellationToken cancellationToken=default)
        {
            try
            {
                var banner = await _dapr.GetStateAsync<V2BannerDto>(
                    storeName: ConstantValues.V2Content.ContentStoreName,
                    key: id.ToString(),
                    cancellationToken: cancellationToken);

                if (banner == null)
                {
                    _logger.LogWarning("No banner found with ID: {BannerId}", id);
                    return null;
                }

                return banner;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving banner with ID: {BannerId}", id);
                throw new Exception("Internal error occurred while retrieving banner.", ex);
            }
        }
        public async Task<string> CreateBannerTypeAsync(V2BannerTypeDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var bannerTypes = await _dapr.GetStateAsync<List<V2BannerTypeDto>>(
                    ConstantValues.V2Content.ContentStoreName,
                    ConstantValues.V2Content.BannerTypeIndexKey,
                    cancellationToken: cancellationToken
                ) ?? new List<V2BannerTypeDto>();
                dto.Id = Guid.NewGuid();
                if (dto.Pages != null && dto.Pages.Any())
                {
                    foreach (var page in dto.Pages)
                    {
                        page.Id = Guid.NewGuid();

                        if (page.bannertypes != null && page.bannertypes.Any())
                        {
                            foreach (var bannerType in page.bannertypes)
                            {
                                bannerType.Id = Guid.NewGuid();
                            }
                        }
                    }
                }

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
                _logger.LogError(ex, "CreateBannerTypeAsync failed");
                throw;
            }
        }


        public async Task<List<V2BannerTypeDto>> GetAllBannerTypesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var bannerTypes = await _dapr.GetStateAsync<List<V2BannerTypeDto>>(
                    ConstantValues.V2Content.ContentStoreName,
                    ConstantValues.V2Content.BannerTypeIndexKey,
                    cancellationToken: cancellationToken
                );

                if (bannerTypes == null)
                    return new List<V2BannerTypeDto>();

                foreach (var bannerType in bannerTypes)
                {
                    bannerType.VerticalName = bannerType.VerticalId.ToString();
                    bannerType.SubVerticalName = bannerType.SubVerticalId?.ToString();
                    if (bannerType.Pages != null)
                    {
                        foreach (var page in bannerType.Pages)
                        {
                            if (page.bannertypes != null)
                            {
                                foreach (var location in page.bannertypes)
                                {
                                    location.BannerDetails = null; 
                                }
                            }
                        }
                    }
                }

                return bannerTypes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetAllBannerTypesAsync failed");
                throw;
            }
        }
        public async Task<List<V2BannerTypeDto>?> GetBannerTypesByFilterAsync( Vertical verticalId, SubVertical? subVerticalId, Guid pageId,CancellationToken cancellationToken)
        {
            try
            {
                var allBannerTypes = await _dapr.GetStateAsync<List<V2BannerTypeDto>>(
                    V2Content.ContentStoreName,
                    V2Content.BannerTypeIndexKey,
                    cancellationToken: cancellationToken);
                if (allBannerTypes == null)
                {
                    Console.WriteLine("⚠️ No banner types found in storage.");
                    return null;
                }
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
                var matchingPage = bannerType.Pages?.FirstOrDefault(p => p.Id == pageId);
                if (matchingPage == null)
                {
                    return null;
                }
                if (matchingPage.bannertypes != null)
                {
                    foreach (var location in matchingPage.bannertypes)
                    {
                        foreach (var item in location.BannerIds ?? [])
                        {
                            var banner = await _dapr.GetStateAsync<V2BannerDto>(
                        V2Content.ContentStoreName,
                        item.ToString(),
                        cancellationToken: cancellationToken);
                            location?.BannerDetails?.Add(banner);
                        }

                    }
                }
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
        public async Task<List<V2BannerTypeDto>> GetBannerTypesWithBannersByStatusAsync( Vertical? verticalId, bool? status, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Fetching banners. Vertical={Vertical}, Status={Status}", verticalId, status);

                var allBannerTypes = await _dapr.GetStateAsync<List<V2BannerTypeDto>>(
                    V2Content.ContentStoreName,
                    V2Content.BannerTypeIndexKey,
                    cancellationToken: cancellationToken);

                if (allBannerTypes == null)
                {
                    _logger.LogWarning("No banner types found.");
                    return [];
                }

                var filteredBannerTypes = !verticalId.HasValue
                    ? allBannerTypes
                    : allBannerTypes.Where(bt => bt.VerticalId == verticalId.Value).ToList();

                if (!filteredBannerTypes.Any())
                {
                    _logger.LogWarning("No banner types found after filtering by vertical.");
                    return [];
                }

                foreach (var bannerType in filteredBannerTypes)
                {
                    if (bannerType.Pages == null) continue;

                    foreach (var page in bannerType.Pages)
                    {
                        if (page.bannertypes == null) continue;

                        foreach (var location in page.bannertypes)
                        {
                            if (location?.BannerIds == null) continue;

                            foreach (var key in location.BannerIds)
                            {
                                var bannerContent = await _dapr.GetStateAsync<V2BannerDto>(
                                    V2Content.ContentStoreName,
                                    key.ToString(),
                                    cancellationToken: cancellationToken);

                                if (bannerContent == null) continue;

                                bool statusMatches = !status.HasValue || bannerContent.Status == status.Value;
                                bool dateValid = bannerContent.EndDate >= DateOnly.FromDateTime(DateTime.Now);

                                if (statusMatches && dateValid)
                                {
                                    location.BannerDetails ??= [];
                                    location.BannerDetails.Add(bannerContent);
                                }
                            }
                        }
                    }
                }

                return filteredBannerTypes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in GetBannerTypesWithBannersByStatusAsync (internal)");
                throw new Exception("Error occurred while retrieving banners with hierarchy.", ex);
            }
        }














    }
}
