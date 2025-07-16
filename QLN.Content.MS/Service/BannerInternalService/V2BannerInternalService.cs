using Dapr.Client;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.IService.V2IContent;
using QLN.Common.Infrastructure.Subscriptions;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace QLN.Content.MS.Service.BannerInternalService
{
    public class V2BannerInternalService : IV2BannerService
    {
        private readonly DaprClient _dapr;
        private readonly ILogger<V2BannerInternalService> _logger;
        private const string StoreName = ConstantValues.V2Content.ContentStoreName;
        private const string IndexKey = "banner-location-index";
        private const string IndexPageKey = "banner-page-index";
        public V2BannerInternalService(DaprClient dapr, ILogger<V2BannerInternalService> logger)
        {
            _dapr = dapr;
            _logger = logger;
        }

        public async Task<string> CreateBannerTypeAsync(V2BannerTypeDto dto, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("CreateBannerTypeAsync called with DTO: {@Dto}", dto);

            try
            {
                if (dto == null)
                    throw new InvalidDataException("Banner type data cannot be null.");

                if (dto.VerticalId == 0 || dto.SubVerticalId == 0)
                    throw new InvalidDataException("VerticalId and SubVerticalId are required.");

                if (dto.BannerTypeIds == null || !dto.BannerTypeIds.Any())
                    throw new InvalidDataException("At least one BannerTypeId is required.");

                dto.Id = Guid.NewGuid();
                var key = dto.Id.ToString();

                await _dapr.SaveStateAsync(
                    storeName: ConstantValues.V2Content.ContentStoreName,
                    key: key,
                    value: dto,
                    cancellationToken: cancellationToken
                );

                var indexKey = $"{ConstantValues.V2Content.BannerTypeIndexKey}-{(int)dto.VerticalId}-{(int)dto.SubVerticalId}";

                var keys = await _dapr.GetStateAsync<List<string>>(
                    storeName: ConstantValues.V2Content.ContentStoreName,
                    key: indexKey,
                    cancellationToken: cancellationToken
                ) ?? new List<string>();

                if (!keys.Contains(key))
                {
                    keys.Add(key);

                    await _dapr.SaveStateAsync(
                        storeName: ConstantValues.V2Content.ContentStoreName,
                        key: indexKey,
                        value: keys,
                        cancellationToken: cancellationToken
                    );
                }

                return "Banner type(s) created successfully.";
            }
            catch (InvalidDataException)
            {
                throw;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error in CreateBannerTypeAsync");
                throw new InvalidDataException("Invalid banner data: " + ex.Message, ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in CreateBannerTypeAsync");
                throw;
            }
        }

        public async Task<string> CreateBannerLocationAsync(V2BannerLocationDto dto, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(dto.BannerTypeName))
                throw new InvalidDataException("BannerTypeName is required.");

            dto.Id = Guid.NewGuid();
            var key = dto.Id.ToString();

            await _dapr.SaveStateAsync(
     storeName: ConstantValues.V2Content.ContentStoreName,
     key: key,
     value: dto,
     cancellationToken: cancellationToken
 );
            var index = await _dapr.GetStateAsync<List<string>>(
       storeName: StoreName,
       key: IndexKey,
     
       cancellationToken: cancellationToken
   ) ?? new List<string>();


            if (!index.Contains(key))
            {
                index.Add(key);
                await _dapr.SaveStateAsync(
     storeName: StoreName,
     key: IndexKey,
     value: index,
   
     cancellationToken: cancellationToken
 );

            }

            return "Banner location created successfully.";
        }
        public async Task<string> CreateBannerPageLocationAsync(V2BannerPageLocationDto dto, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(dto.BannerPageName))
                throw new InvalidDataException("BannerTypeName is required.");

            dto.Id = Guid.NewGuid();
            var key = dto.Id.ToString();

            await _dapr.SaveStateAsync( storeName: ConstantValues.V2Content.ContentStoreName, key: key, value: dto, cancellationToken: cancellationToken);
            var index = await _dapr.GetStateAsync<List<string>>(storeName: StoreName,key: IndexPageKey,cancellationToken: cancellationToken) ?? new List<string>();
            if (!index.Contains(key))
            {
                index.Add(key);await _dapr.SaveStateAsync(storeName: StoreName,key: IndexPageKey,value: index,cancellationToken: cancellationToken);

            }

            return "Banner location created successfully.";
        }
        public async Task<List<V2BannerLocationDto>> GetAllBannerLocationsAsync(CancellationToken cancellationToken = default)
        {
            var index = await _dapr.GetStateAsync<List<string>>(storeName: StoreName,key: IndexKey,cancellationToken: cancellationToken) ?? new List<string>();
            var result = new List<V2BannerLocationDto>();
            foreach (var id in index)
            {
                var item = await _dapr.GetStateAsync<V2BannerLocationDto>(storeName: StoreName,key: id,cancellationToken: cancellationToken);
                if (item != null)
                {
                    result.Add(item);
                }
            }
            return result;
        }
        public async Task<List<BannerTypeDetailsDto>> GetBannerTypesByVerticalAsync(Vertical vertical, CancellationToken cancellationToken = default)
        {
            var result = new List<BannerTypeDetailsDto>();
            var storeName = ConstantValues.V2Content.ContentStoreName;

            var subVerticals = Enum.GetValues(typeof(SubVertical)).Cast<SubVertical>();

            foreach (var sub in subVerticals)
            {
                var indexKey = $"{ConstantValues.V2Content.BannerTypeIndexKey}-{(int)vertical}-{(int)sub}";
                var ids = await _dapr.GetStateAsync<List<string>>(
      storeName,
      indexKey,
      consistencyMode: null,
      metadata: null,
      cancellationToken: cancellationToken
  ) ?? new List<string>();



                foreach (var id in ids)
                {
                    var dto = await _dapr.GetStateAsync<V2BannerTypeDto>(
     storeName,
     id,
     consistencyMode: null,
     metadata: null,
     cancellationToken: cancellationToken
 );

                    if (dto == null) continue;

                    var detail = new BannerTypeDetailsDto
                    {
                        VerticalName = dto.VerticalId.ToString(),
                        SubVerticalName = dto.SubVerticalId.ToString(),
                        PageId = dto.PageId,
                        Dimensions=dto.Dimensions,
                        BannerslotId=dto.BannerslotId,
                        PageName = await GetPageNameByIdAsync(dto.PageId, cancellationToken),
                        BannerTypes = new List<BannerTypeItemDto>()
                    };

                    foreach (var bannerTypeId in dto.BannerTypeIds)
                    {
                        var bannerTypeDto = await _dapr.GetStateAsync<V2BannerLocationDto>(
     storeName,
     bannerTypeId.ToString(),
     consistencyMode: null,
     metadata: null,
     cancellationToken: cancellationToken
 );

                        if (bannerTypeDto != null)
                        {
                            detail.BannerTypes.Add(new BannerTypeItemDto
                            {
                                BannerTypeId = bannerTypeDto.Id,
                                BannerTypeName = bannerTypeDto.BannerTypeName
                            });
                        }
                    }

                    result.Add(detail);
                }
            }

            return result;
        }
        private async Task<string> GetPageNameByIdAsync(Guid pageId, CancellationToken cancellationToken)
        {
            var storeName = ConstantValues.V2Content.ContentStoreName;
            var pageDto = await _dapr.GetStateAsync<V2BannerPageLocationDto>(storeName,pageId.ToString(),consistencyMode: null,metadata: null,cancellationToken: cancellationToken );
            return pageDto?.BannerPageName ?? "Unknown";
        }
        public async Task<List<BannerTypeDetailsDto>> GetBannerTypesBySubVerticalAsync(SubVertical subVertical, CancellationToken cancellationToken = default)
        {
            var result = new List<BannerTypeDetailsDto>();
            var storeName = ConstantValues.V2Content.ContentStoreName;

            var verticals = Enum.GetValues(typeof(Vertical)).Cast<Vertical>();

            foreach (var vertical in verticals)
            {
                var indexKey = $"{ConstantValues.V2Content.BannerTypeIndexKey}-{(int)vertical}-{(int)subVertical}";
                Console.WriteLine($"Checking index key: {indexKey}");

                var ids = await _dapr.GetStateAsync<List<string>>(
                    storeName,
                    indexKey,
                    consistencyMode: null,
                    metadata: null,
                    cancellationToken: cancellationToken
                ) ?? new List<string>();

                Console.WriteLine($"Found {ids.Count} item(s) for index key: {indexKey}");

                foreach (var id in ids)
                {
                    Console.WriteLine($"Fetching banner type with ID: {id}");

                    var dto = await _dapr.GetStateAsync<V2BannerTypeDto>(
                        storeName,
                        id,
                        consistencyMode: null,
                        metadata: null,
                        cancellationToken: cancellationToken
                    );

                    if (dto == null)
                    {
                        Console.WriteLine($"BannerTypeDto is null for ID: {id}");
                        continue;
                    }

                    var detail = new BannerTypeDetailsDto
                    {
                        VerticalName = dto.VerticalId.ToString(),
                        SubVerticalName = dto.SubVerticalId.ToString(),
                        PageId = dto.PageId,
                        PageName = await GetPageNameByIdAsync(dto.PageId, cancellationToken),
                        Dimensions = dto.Dimensions,
                        BannerslotId = dto.BannerslotId,
                        BannerTypes = new List<BannerTypeItemDto>()
                    };

                    Console.WriteLine($"BannerTypeDto found: Vertical={detail.VerticalName}, SubVertical={detail.SubVerticalName}, PageId={detail.PageId}");

                    foreach (var bannerTypeId in dto.BannerTypeIds)
                    {
                        Console.WriteLine($"Fetching BannerTypeId: {bannerTypeId}");

                        var bannerTypeDto = await _dapr.GetStateAsync<V2BannerLocationDto>(
                            storeName,
                            bannerTypeId.ToString(),
                            consistencyMode: null,
                            metadata: null,
                            cancellationToken: cancellationToken
                        );

                        if (bannerTypeDto != null)
                        {
                            detail.BannerTypes.Add(new BannerTypeItemDto
                            {
                                BannerTypeId = bannerTypeDto.Id,
                                BannerTypeName = bannerTypeDto.BannerTypeName
                            });

                            Console.WriteLine($"Added BannerType: {bannerTypeDto.BannerTypeName}");
                        }
                        else
                        {
                            Console.WriteLine($"No BannerTypeDto found for ID: {bannerTypeId}");
                        }
                    }

                    result.Add(detail);
                }
            }

            Console.WriteLine($"Total banner types fetched: {result.Count}");
            return result;
        }
        public async Task<List<BannerTypeDetailsDto>> GetBannerTypesByPageIdAsync(Guid pageId, CancellationToken cancellationToken = default)
        {
            var result = new List<BannerTypeDetailsDto>();
            var storeName = ConstantValues.V2Content.ContentStoreName;

            var verticals = Enum.GetValues(typeof(Vertical)).Cast<Vertical>();
            var subVerticals = Enum.GetValues(typeof(SubVertical)).Cast<SubVertical>();

            foreach (var vertical in verticals)
            {
                foreach (var sub in subVerticals)
                {
                    var indexKey = $"{ConstantValues.V2Content.BannerTypeIndexKey}-{(int)vertical}-{(int)sub}";
                    Console.WriteLine($"🔍 Checking index key: {indexKey}");

                    var ids = await _dapr.GetStateAsync<List<string>>(
                        storeName,
                        indexKey,
                        consistencyMode: null,
                        metadata: null,
                        cancellationToken: cancellationToken
                    ) ?? new();

                    Console.WriteLine($"📦 Found {ids.Count} records for key {indexKey}");

                    foreach (var id in ids)
                    {
                        var dto = await _dapr.GetStateAsync<V2BannerTypeDto>(
                            storeName,
                            id,
                            consistencyMode: null,
                            metadata: null,
                            cancellationToken: cancellationToken
                        );

                        if (dto == null) continue;

                        // LOG the values being compared
                        Console.WriteLine($"✅ Checking DTO ID: {dto.Id} | PageId in DTO: {dto.PageId} | Target PageId: {pageId}");

                        // Defensive handling if PageId is a string
                        if (dto.PageId != pageId)
                        {
                            Console.WriteLine($"⛔ PageId doesn't match for DTO ID: {dto.Id}");
                            continue;
                        }

                        var detail = new BannerTypeDetailsDto
                        {
                            VerticalName = dto.VerticalId.ToString(),
                            SubVerticalName = dto.SubVerticalId.ToString(),
                            PageId = dto.PageId,
                            PageName = await GetPageNameByIdAsync(dto.PageId, cancellationToken),
                            Dimensions = dto.Dimensions,
                            BannerslotId = dto.BannerslotId,
                            BannerTypes = new List<BannerTypeItemDto>()
                        };

                        foreach (var bannerTypeId in dto.BannerTypeIds)
                        {
                            var bannerTypeDto = await _dapr.GetStateAsync<V2BannerLocationDto>(
                                storeName,
                                bannerTypeId.ToString(),
                                consistencyMode: null,
                                metadata: null,
                                cancellationToken: cancellationToken
                            );

                            if (bannerTypeDto != null)
                            {
                                detail.BannerTypes.Add(new BannerTypeItemDto
                                {
                                    BannerTypeId = bannerTypeDto.Id,
                                    BannerTypeName = bannerTypeDto.BannerTypeName
                                });
                            }
                        }

                        result.Add(detail);
                    }
                }
            }

            Console.WriteLine($"📤 Returning {result.Count} matched banner type detail(s)");
            return result;
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







    }
}
