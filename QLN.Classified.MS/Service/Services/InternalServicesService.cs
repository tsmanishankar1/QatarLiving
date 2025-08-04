using Dapr.Actors.Client;
using Dapr.Actors;
using Dapr.Client;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Auditlog;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.IService.IService;
using QLN.Common.Infrastructure.IService.ISubscriptionService;
using System.Text.Json;
using System.Text.RegularExpressions;
using static QLN.Common.DTO_s.NotificationDto;
using QLN.Subscriptions.Actor.ActorClass;

namespace QLN.Classified.MS.Service.Services
{
    public class InternalServicesService : IServices
    {
        public readonly DaprClient _dapr;
        public readonly AuditLogger _auditLogger;
        public InternalServicesService(DaprClient dapr, AuditLogger auditLogger)
        {
            _dapr = dapr;
            _auditLogger = auditLogger;
        }
        public async Task<string> CreateCategory(ServicesCategory dto, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(dto.Category))
                throw new InvalidDataException("MainCategory is required.");

            if (dto.L1Categories == null || dto.L1Categories.Count == 0)
                throw new InvalidDataException("At least one L1 Category is required.");

            dto.Id = Guid.NewGuid(); 

            foreach (var l1 in dto.L1Categories)
            {
                l1.Id = Guid.NewGuid();

                if (string.IsNullOrWhiteSpace(l1.Name))
                    throw new InvalidDataException("Each L1 Category must have a Name.");

                if (l1.L2Categories == null || l1.L2Categories.Count == 0)
                    throw new InvalidDataException($"L1 Category '{l1.Name}' must have at least one L2 Category.");

                foreach (var l2 in l1.L2Categories)
                {
                    l2.Id = Guid.NewGuid();

                    if (string.IsNullOrWhiteSpace(l2.Name))
                        throw new InvalidDataException("Each L2 Category must have a Name.");
                }
            }

            var key = dto.Id.ToString();

            await _dapr.SaveStateAsync(ConstantValues.Services.StoreName, key, dto, cancellationToken: cancellationToken);

            var keys = await _dapr.GetStateAsync<List<string>>(ConstantValues.Services.StoreName, ConstantValues.Services.IndexKey, cancellationToken : cancellationToken) ?? new();

            if (!keys.Contains(key))
            {
                keys.Add(key);
                await _dapr.SaveStateAsync(ConstantValues.Services.StoreName, ConstantValues.Services.IndexKey, keys, cancellationToken : cancellationToken);
            }

            return "Category Created Successfully";
        }
        public async Task<string> UpdateCategory(ServicesCategory dto, CancellationToken cancellationToken = default)
        {
            var key = dto.Id?.ToString();
            if (string.IsNullOrWhiteSpace(key))
                throw new InvalidDataException("Invalid category ID.");

            var existing = await _dapr.GetStateAsync<ServicesCategory>(
                ConstantValues.Services.StoreName,
                key,
                cancellationToken: cancellationToken
            );

            if (existing == null)
                throw new InvalidDataException("Category not found for update.");

            foreach (var l1 in dto.L1Categories)
            {
                if (l1.Id == Guid.Empty)
                    l1.Id = Guid.NewGuid();

                foreach (var l2 in l1.L2Categories)
                {
                    if (l2.Id == Guid.Empty)
                        l2.Id = Guid.NewGuid();
                }
            }

            await _dapr.SaveStateAsync(ConstantValues.Services.StoreName, key, dto, cancellationToken : cancellationToken);

            return "Category updated successfully.";
        }
        public async Task<List<ServicesCategory>> GetAllCategories(CancellationToken cancellationToken = default)
        {
            var keys = await _dapr.GetStateAsync<List<string>>(
                ConstantValues.Services.StoreName,
                ConstantValues.Services.IndexKey,
                cancellationToken: cancellationToken
            ) ?? new();

            if (keys == null || keys.Count == 0)
                return new List<ServicesCategory>();

            var bulkItems = await _dapr.GetBulkStateAsync(
                ConstantValues.Services.StoreName,
                keys,
                parallelism: null,
                cancellationToken: cancellationToken
            );

            var result = bulkItems
                .Where(item => !string.IsNullOrWhiteSpace(item.Value))
                .Select(item =>
                {
                    try
                    {
                        return JsonSerializer.Deserialize<ServicesCategory>(
                            item.Value,
                            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
                        );
                    }
                    catch
                    {
                        return null;
                    }
                })
                .Where(x => x != null)
                .ToList();

            return result!;
        }
        public async Task<ServicesCategory?> GetCategoryById(Guid id, CancellationToken cancellationToken = default)
        {
            var key = id.ToString();

            var indexKeys = await _dapr.GetStateAsync<List<string>>(
                ConstantValues.Services.StoreName,
                ConstantValues.Services.IndexKey,
                cancellationToken: cancellationToken
            ) ?? new();
            if (!indexKeys.Contains(key))
            {
                return null;
            }
            var category = await _dapr.GetStateAsync<ServicesCategory>(
                ConstantValues.Services.StoreName,
                key,
                cancellationToken: cancellationToken
            );

            return category;
        }
        public async Task<string> CreateServiceAd(string uid, string userName, ServiceDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                string? categoryName = null;
                string? l1CategoryName = null;
                string? l2CategoryName = null;
                var mainCategory = await _dapr.GetStateAsync<ServicesCategory>(
                    ConstantValues.Services.StoreName,
                    dto.CategoryId.ToString(),
                    cancellationToken: cancellationToken);

                if (mainCategory != null)
                {
                    dto.CategoryName = mainCategory.Category;
                    var l1Category = mainCategory.L1Categories?.FirstOrDefault(l1 => l1.Id == dto.L1CategoryId);
                    if (l1Category != null)
                    {
                        dto.L1CategoryName = l1Category.Name;
                        l1CategoryName = l1Category.Name;
                        var l2Category = l1Category.L2Categories?.FirstOrDefault(l2 => l2.Id == dto.L2CategoryId);
                        if (l2Category != null)
                        {
                            dto.L2CategoryName = l2Category.Name;
                            l2CategoryName = l2Category.Name;
                        }
                    }
                }

                var allAdKeys = await _dapr.GetStateAsync<List<string>>(
                   ConstantValues.Services.StoreName,
                   ConstantValues.Services.ServicesIndexKey,
                   cancellationToken: cancellationToken
                ) ?? new();

                foreach (var adKey in allAdKeys)
                {
                    var existingAd = await _dapr.GetStateAsync<ServicesModel>(
                        ConstantValues.Services.StoreName,
                        adKey,
                        cancellationToken: cancellationToken
                    );

                    if (existingAd != null &&
                        existingAd.CreatedBy == uid &&
                        existingAd.L2CategoryId == dto.L2CategoryId &&
                        existingAd.IsActive &&
                        existingAd.Status == ServiceStatus.Published)
                    {
                        throw new ArgumentException("You already have an active ad in this category. Please unpublish or remove it before posting another.");
                    }
                }

                var entity = new ServicesModel
                {
                    AdType = dto.AdType,
                    Id = Guid.NewGuid(),
                    CategoryId = dto.CategoryId,
                    L1CategoryId = dto.L1CategoryId,
                    L2CategoryId = dto.L2CategoryId,
                    CategoryName = mainCategory.Category,
                    L1CategoryName = dto.L1CategoryName,
                    L2CategoryName = dto.L2CategoryName,
                    IsPriceOnRequest = dto.IsPriceOnRequest,
                    Price = dto.Price,
                    Title = dto.Title,
                    Description = dto.Description,
                    PhoneNumberCountryCode = dto.PhoneNumberCountryCode,
                    PhoneNumber = dto.PhoneNumber,
                    WhatsappNumberCountryCode = dto.WhatsappNumberCountryCode,
                    WhatsappNumber = dto.WhatsappNumber,
                    EmailAddress = dto.EmailAddress,
                    Location = dto.Location,
                    LocationId = dto.LocationId,
                    StreetNumber = dto.StreetNumber,
                    BuildingNumber = dto.BuildingNumber,
                    LicenseCertificate = dto.LicenseCertificate,
                    Comments = dto.Comments,
                    SubscriptionId = uid,
                    ZoneId = dto.ZoneId,
                    Longitude = dto.Longitude,
                    Lattitude = dto.Lattitude,
                    PhotoUpload = dto.PhotoUpload,
                    UserName = userName,
                    Status = dto.Status,
                    IsActive = true,
                    CreatedBy = uid,
                    CreatedAt = DateTime.UtcNow
                };
                ValidateCommon(entity);

                var key = entity.Id.ToString();
                await _dapr.SaveStateAsync(
                    ConstantValues.Services.StoreName,
                    key,
                    entity,
                    cancellationToken: cancellationToken
                );
                var upsertRequest = await IndexServiceToAzureSearch(entity, cancellationToken);

                if (upsertRequest != null)
                {
                    var message = new IndexMessage
                    {
                        Action = "Upsert",
                        Vertical = ConstantValues.IndexNames.ServicesIndex,
                        UpsertRequest = upsertRequest
                    };

                    await _dapr.PublishEventAsync(
                        pubsubName: ConstantValues.PubSubName,
                        topicName: ConstantValues.PubSubTopics.IndexUpdates,
                    data: message,
                        cancellationToken: cancellationToken
                    );
                }
                var keys = await _dapr.GetStateAsync<List<string>>(
                    ConstantValues.Services.StoreName,
                    ConstantValues.Services.ServicesIndexKey,
                    cancellationToken: cancellationToken
                ) ?? new();

                if (!keys.Contains(key))
                {
                    keys.Add(key);
                    await _dapr.SaveStateAsync(
                        ConstantValues.Services.StoreName,
                        ConstantValues.Services.ServicesIndexKey,
                        keys,
                        cancellationToken: cancellationToken
                    );
                }
                await _auditLogger.CreateAuditLog(
                    id: Guid.NewGuid(),
                    module: "Service",
                    httpMethod: "POST",
                    apiEndpoint: $"/api/service/createbyuserid?uid={uid}&userName={userName}",
                    successMessage: "Service Ad Created Successfully",
                    createdBy: uid,
                    payload: entity,
                    cancellationToken: cancellationToken
                );
                return "Service Ad Created Successfully";
            }
            catch (ArgumentException ex)
            {
                throw new InvalidDataException(ex.Message, ex);
            }
            catch (Exception ex)
            {
                throw new Exception("Error creating service ad", ex);
            }
        }
        private static void ValidateCommon(ServicesModel dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Title))
                throw new ArgumentException("Title is required.");
            if (dto.Title.Length < 5 || dto.Title.Length > 70)
                throw new ArgumentException("Title must be between 5 and 70 characters.");

            if (string.IsNullOrWhiteSpace(dto.Description))
                throw new ArgumentException("Description is required.");
            if (dto.Description.Length < 20)
                throw new ArgumentException("Description must be at least 20 characters.");

            if (string.IsNullOrWhiteSpace(dto.PhoneNumberCountryCode) || string.IsNullOrWhiteSpace(dto.PhoneNumber))
                throw new ArgumentException("Phone number with country code is required.");
            if (string.IsNullOrWhiteSpace(dto.WhatsappNumberCountryCode) || string.IsNullOrWhiteSpace(dto.WhatsappNumber))
                throw new ArgumentException("WhatsApp number with country code is required.");

            if (dto.CategoryId == Guid.Empty || dto.L1CategoryId == Guid.Empty || dto.L2CategoryId == Guid.Empty)
                throw new ArgumentException("All category IDs must be provided.");

            var phoneRegex = new Regex(@"^\d{6,15}$");

            if (!phoneRegex.IsMatch(dto.PhoneNumber))
                throw new ArgumentException("Invalid phone number format.");

            if (!string.IsNullOrWhiteSpace(dto.WhatsappNumber) && !phoneRegex.IsMatch(dto.WhatsappNumber))
                throw new ArgumentException("Invalid WhatsApp number format.");

            if (!string.IsNullOrWhiteSpace(dto.EmailAddress) && !IsValidEmail(dto.EmailAddress))
                throw new ArgumentException("Invalid email format.");

            if (!string.IsNullOrWhiteSpace(dto.L1CategoryName) &&
                    dto.L1CategoryName.Trim().Equals("therapeutic services", StringComparison.OrdinalIgnoreCase) &&
                    string.IsNullOrWhiteSpace(dto.LicenseCertificate))
            {
                throw new ArgumentException("License certificate is required for therapeutic services.");
            }
        }
        public async Task<string> UpdateServiceAd(string userId, ServicesModel dto, CancellationToken cancellationToken = default)
        {
            try
            {
                if (dto.Id == Guid.Empty)
                    throw new ArgumentException("Service Ad ID is required for update.");

                var key = dto.Id.ToString();
                var existing = await _dapr.GetStateAsync<ServicesModel>(
                    ConstantValues.Services.StoreName,
                    key,
                    cancellationToken: cancellationToken
                );
                if (existing == null)
                    throw new ArgumentException("Service Ad not found for update.");
                var mainCategory = await _dapr.GetStateAsync<ServicesCategory>(
                  ConstantValues.Services.StoreName,
                  dto.CategoryId.ToString(),
                  cancellationToken: cancellationToken);
                string? categoryName = null;
                string? l1CategoryName = null;
                string? l2CategoryName = null;

                if (mainCategory != null)
                {
                    categoryName = mainCategory.Category;
                    var l1Category = mainCategory.L1Categories.FirstOrDefault(l1 => l1.Id == dto.L1CategoryId);
                    if (l1Category != null)
                    {
                        l1CategoryName = l1Category.Name;

                        var l2Category = l1Category.L2Categories.FirstOrDefault(l2 => l2.Id == dto.L2CategoryId);
                        if (l2Category != null)
                        {
                            l2CategoryName = l2Category.Name;
                        }
                    }
                }
                var allAdKeys = await _dapr.GetStateAsync<List<string>>(
                  ConstantValues.Services.StoreName,
                  ConstantValues.Services.ServicesIndexKey,
                  cancellationToken: cancellationToken
               ) ?? new();
                foreach (var adKey in allAdKeys)
                {
                    var existingAd = await _dapr.GetStateAsync<ServicesModel>(
                        ConstantValues.Services.StoreName,
                        adKey,
                        cancellationToken: cancellationToken
                    );

                    if (existingAd != null &&
                        existingAd.CreatedBy == userId &&
                        existingAd.L2CategoryId == dto.L2CategoryId &&
                        existingAd.IsActive &&
                        existingAd.Status == ServiceStatus.Published)
                    {
                        throw new ArgumentException("You already have an active ad in this category. Please unpublish or remove it before posting another.");
                    }
                }
                ValidateCommon(dto);

                var entity = new ServicesModel
                {
                    Id = existing.Id,
                    CategoryId = dto.CategoryId,
                    L1CategoryId = dto.L1CategoryId,
                    L2CategoryId = dto.L2CategoryId,
                    CategoryName = categoryName,
                    L1CategoryName = l1CategoryName,
                    L2CategoryName = l2CategoryName,
                    IsPriceOnRequest = dto.IsPriceOnRequest,
                    Price = dto.Price,
                    Title = dto.Title,
                    Description = dto.Description,
                    PhoneNumberCountryCode = dto.PhoneNumberCountryCode,
                    PhoneNumber = dto.PhoneNumber,
                    WhatsappNumberCountryCode = dto.WhatsappNumberCountryCode,
                    WhatsappNumber = dto.WhatsappNumber,
                    EmailAddress = dto.EmailAddress,
                    Location = dto.Location,
                    LocationId = dto.LocationId,
                    StreetNumber = dto.StreetNumber,
                    BuildingNumber = dto.BuildingNumber,
                    LicenseCertificate = dto.LicenseCertificate,
                    Comments = dto.Comments,
                    SubscriptionId = dto.SubscriptionId,
                    ZoneId = dto.ZoneId,
                    Longitude = dto.Longitude,
                    Lattitude = dto.Lattitude,
                    PhotoUpload = dto.PhotoUpload,
                    AdType = dto.AdType,
                    IsFeatured = dto.IsFeatured,
                    IsPromoted = dto.IsPromoted,
                    IsRefreshed = dto.IsRefreshed,
                    RefreshExpiryDate = dto.RefreshExpiryDate,
                    FeaturedExpiryDate = dto.FeaturedExpiryDate,
                    PromotedExpiryDate = dto.PromotedExpiryDate,
                    ExpiryDate = dto.ExpiryDate,
                    UserName = dto.UserName,
                    PublishedDate = dto.PublishedDate,
                    Status = dto.Status,
                    IsActive = dto.IsActive,
                    CreatedBy = existing.CreatedBy,
                    CreatedAt = existing.CreatedAt,
                    UpdatedBy = userId,
                    UpdatedAt = DateTime.UtcNow
                };

                await _dapr.SaveStateAsync(
                    ConstantValues.Services.StoreName,
                    key,
                    entity,
                    cancellationToken: cancellationToken
                );
                var upsertRequest = await IndexServiceToAzureSearch(entity, cancellationToken);

                if (upsertRequest != null)
                {
                    var message = new IndexMessage
                    {
                        Action = "Upsert",
                        Vertical = ConstantValues.IndexNames.ServicesIndex,
                        UpsertRequest = upsertRequest
                    };

                    await _dapr.PublishEventAsync(
                        pubsubName: ConstantValues.PubSubName,
                        topicName: ConstantValues.PubSubTopics.IndexUpdates,
                        data: message,
                        cancellationToken: cancellationToken
                    );
                }
                await _dapr.PublishEventAsync("pubsub", "notifications-email", new NotificationRequest
                {
                    Destinations = new List<string> { "email" },
                    Recipients = new List<RecipientDto>
                    {
                        new RecipientDto
                        {
                            Name = dto.UserName,
                            Email = dto.EmailAddress
                        }
                    },
                    Subject = $"Service Ad '{dto.Title}' was updated",
                    Plaintext = $"Hello,\n\nYour ad titled '{dto.Title}' has been updated.\n\nStatus: {dto.Status}\n\nThanks,\nQL Team",
                    Html = $"{dto.Title} has been updated."
                }, cancellationToken);

                await _auditLogger.UpdateAuditLog(
                   id: Guid.NewGuid(),
                   module: "Service",
                   httpMethod: "PUT",
                   apiEndpoint: $"/api/service/updatebyid?id={dto.Id}",
                   successMessage: "Service Ad Updated Successfully",
                   updatedBy: userId,
                   payload: dto,
                   cancellationToken: cancellationToken
               );

                return "Service Ad updated successfully.";
            }
            catch (ArgumentException ex)
            {
                throw new InvalidDataException(ex.Message, ex);
            }
            catch (Exception ex)
            {
                throw new Exception("Error updating service ad", ex);
            }
        }
        private async Task<CommonIndexRequest> IndexServiceToAzureSearch(ServicesModel dto, CancellationToken cancellationToken)
        {
          
            var indexDoc = new ServicesIndex
            {
                Id = dto.Id.ToString(),
                CategoryId = dto.CategoryId.ToString(),
                L1CategoryId = dto.L1CategoryId.ToString(),
                L2CategoryId = dto.L2CategoryId.ToString(),
                CategoryName = dto.CategoryName,
                L1CategoryName = dto.L1CategoryName,
                L2CategoryName = dto.L2CategoryName,
                Price = (double)dto.Price,
                IsPriceOnRequest = dto.IsPriceOnRequest,
                Title = dto.Title,
                Description = dto.Description,
                PhoneNumberCountryCode = dto.PhoneNumberCountryCode,
                PhoneNumber = dto.PhoneNumber,
                WhatsappNumberCountryCode = dto.WhatsappNumberCountryCode,
                WhatsappNumber = dto.WhatsappNumber,
                EmailAddress = dto.EmailAddress,
                Location = dto.Location,
                LocationId = dto.LocationId,
                StreetNumber = dto.StreetNumber,
                BuildingNumber = dto.BuildingNumber,
                LicenseCertificate = dto.LicenseCertificate,
                ZoneId = dto.ZoneId,
                SubscriptionId = dto.SubscriptionId,
                Comments = dto.Comments,
                Longitude = (double)dto.Longitude,
                Lattitude = (double)dto.Lattitude,
                AdType = dto.AdType.ToString(),
                IsFeatured = dto.IsFeatured,
                IsPromoted = dto.IsPromoted,
                Status = dto.Status.ToString(),
                FeaturedExpiryDate = dto.FeaturedExpiryDate,
                PromotedExpiryDate = dto.PromotedExpiryDate,
                RefreshExpiryDate = dto.RefreshExpiryDate,
                IsRefreshed = dto.IsRefreshed,
                PublishedDate = dto.PublishedDate,
                ExpiryDate = dto.ExpiryDate,
                UserName = dto.UserName,
                IsActive = dto.IsActive,
                CreatedBy = dto.CreatedBy,
                CreatedAt = dto.CreatedAt,
                UpdatedAt = dto.UpdatedAt,
                UpdatedBy = dto.UpdatedBy,
                Images = dto.PhotoUpload.Select(i => new ImageInfo
                {
                    Url = i.Url,
                    Order = i.Order
                }).ToList()
            };
            var indexRequest = new CommonIndexRequest
            {
                IndexName = ConstantValues.IndexNames.ServicesIndex,
                ServicesItem = indexDoc
            };
            return indexRequest;

        }

        private static bool IsValidEmail(string email)
        {
            return !string.IsNullOrWhiteSpace(email) &&
                   Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase);
        }
        public async Task<List<ServicesModel>> GetAllServiceAds(CancellationToken cancellationToken = default)
        {
            var keys = await _dapr.GetStateAsync<List<string>>(
                ConstantValues.Services.StoreName,
                ConstantValues.Services.ServicesIndexKey,
                cancellationToken: cancellationToken
            ) ?? new();

            if (keys.Count == 0)
                return new List<ServicesModel>();

            var items = await _dapr.GetBulkStateAsync(
                ConstantValues.Services.StoreName,
                keys,
                parallelism: null,
                cancellationToken: cancellationToken
            );

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };

            return items
                .Where(i => !string.IsNullOrWhiteSpace(i.Value))
                .Select(i =>
                {
                    return JsonSerializer.Deserialize<ServicesModel>(i.Value, options);
                })
                .Where(x => x != null && x.Id != Guid.Empty && !string.IsNullOrWhiteSpace(x.Title) && x.IsActive)!
                .ToList();
        }
        public async Task<ServicesModel?> GetServiceAdById(Guid id, CancellationToken cancellationToken = default)
        {
            var key = id.ToString();

            var indexKeys = await _dapr.GetStateAsync<List<string>>(
                ConstantValues.Services.StoreName,
                ConstantValues.Services.ServicesIndexKey,
                cancellationToken: cancellationToken
            ) ?? new();

            if (!indexKeys.Contains(key))
                return null; 

            var ad = await _dapr.GetStateAsync<ServicesModel>(
                ConstantValues.Services.StoreName,
                key,
                cancellationToken: cancellationToken
            );

            return ad?.IsActive == true ? ad : null;
        }
        public async Task<string> DeleteServiceAdById(string userId, Guid id, CancellationToken cancellationToken = default)
        {
            var key = id.ToString();

            var existing = await _dapr.GetStateAsync<ServicesModel>(
                ConstantValues.Services.StoreName,
                key,
                cancellationToken: cancellationToken
            );

            if (existing == null || !existing.IsActive)
                throw new InvalidDataException("Active Service Ad not found.");

            existing.IsActive = false;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedBy = userId;

            await _dapr.SaveStateAsync(
                ConstantValues.Services.StoreName,
                key,
                existing,
                cancellationToken: cancellationToken
            );
            var upsertRequest = await IndexServiceToAzureSearch(existing, cancellationToken);

            if (upsertRequest != null)
            {
                var message = new IndexMessage
                {
                    Action = "Upsert",
                    Vertical = ConstantValues.IndexNames.ServicesIndex,
                    UpsertRequest = upsertRequest
                };

                await _dapr.PublishEventAsync(
                    pubsubName: ConstantValues.PubSubName,
                    topicName: ConstantValues.PubSubTopics.IndexUpdates,
                    data: message,
                    cancellationToken: cancellationToken
                );
            }
            await _auditLogger.UpdateAuditLog(
                id: Guid.NewGuid(),
                module: "Service",
                httpMethod: "DELETE",
                apiEndpoint: $"/api/service/deletebyid?id={id}",
                successMessage: "Service Ad Deleted Successfully",
                updatedBy: userId,
                payload: null, 
                cancellationToken: cancellationToken
            );

            return "Service Ad soft-deleted successfully.";
        }
        public async Task<ServicesPagedResponse<ServicesModel>> GetServicesByStatusWithPagination(ServiceStatusQuery dto, CancellationToken cancellationToken = default)
        {
            var indexKeys = await _dapr.GetStateAsync<List<string>>(
                ConstantValues.Services.StoreName,
                ConstantValues.Services.ServicesIndexKey,
                cancellationToken: cancellationToken
            ) ?? new();

            if (indexKeys.Count == 0)
            {
                return new ServicesPagedResponse<ServicesModel>
                {
                    TotalCount = 0,
                    PageNumber = dto.PageNumber,
                    PerPage = dto.PerPage,
                    Items = new()
                };
            }

            var ads = await _dapr.GetBulkStateAsync(
                ConstantValues.Services.StoreName,
                indexKeys,
                parallelism: 10,
                cancellationToken: cancellationToken
            );

            var filtered = ads
                .Where(e => !string.IsNullOrWhiteSpace(e.Value))
                .Select(e => JsonSerializer.Deserialize<ServicesModel>(e.Value!, _jsonOptions))
                .Where(e => e != null && e.Status == dto.Status && e.IsActive)
                .ToList();

            var totalCount = filtered.Count;
            var skip = (dto.PageNumber - 1) * dto.PerPage;

            var pagedItems = filtered
                .Skip((int)skip)
                .Take((int)dto.PerPage)
                .ToList();

            return new ServicesPagedResponse<ServicesModel>
            {
                TotalCount = totalCount,
                PageNumber = dto.PageNumber,
                PerPage = dto.PerPage,
                Items = pagedItems
            };
        }
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };
        public async Task<ServicesModel> PromoteService(PromoteServiceRequest request, CancellationToken ct)
        {
            var serviceAd = await _dapr.GetStateAsync<ServicesModel>(
                ConstantValues.Services.StoreName,
                request.ServiceId.ToString(),
                cancellationToken: ct
            );
            serviceAd.IsPromoted = request.IsPromoted;
            serviceAd.PromotedExpiryDate = request.IsPromoted ? DateTime.UtcNow.AddDays(7) : null;
            serviceAd.UpdatedAt = DateTime.UtcNow;

            await _dapr.SaveStateAsync(
                ConstantValues.Services.StoreName,
                request.ServiceId.ToString(),
                serviceAd,
                cancellationToken: ct
            );
            var upsertRequest = await IndexServiceToAzureSearch(serviceAd, ct);

            if (upsertRequest != null)
            {
                var message = new IndexMessage
                {
                    Action = "Upsert",
                    Vertical = ConstantValues.IndexNames.ServicesIndex,
                    UpsertRequest = upsertRequest
                };

                await _dapr.PublishEventAsync(
                    pubsubName: ConstantValues.PubSubName,
                    topicName: ConstantValues.PubSubTopics.IndexUpdates,
                data: message,
                    cancellationToken: ct
                );
            }
            return serviceAd;
        }
        public async Task<ServicesModel> FeatureService(FeatureServiceRequest request, CancellationToken ct)
        {
            var serviceAd = await _dapr.GetStateAsync<ServicesModel>(
                ConstantValues.Services.StoreName,
                request.ServiceId.ToString(),
                cancellationToken: ct
            );

            serviceAd.IsFeatured = request.IsFeature;
            serviceAd.FeaturedExpiryDate = request.IsFeature ? DateTime.UtcNow.AddDays(7) : null;
            serviceAd.UpdatedAt = DateTime.UtcNow;

            await _dapr.SaveStateAsync(
                ConstantValues.Services.StoreName,
                request.ServiceId.ToString(),
                serviceAd,
                cancellationToken: ct
            );
            var upsertRequest = await IndexServiceToAzureSearch(serviceAd, ct);

            if (upsertRequest != null)
            {
                var message = new IndexMessage
                {
                    Action = "Upsert",
                    Vertical = ConstantValues.IndexNames.ServicesIndex,
                    UpsertRequest = upsertRequest
                };

                await _dapr.PublishEventAsync(
                    pubsubName: ConstantValues.PubSubName,
                    topicName: ConstantValues.PubSubTopics.IndexUpdates,
                data: message,
                    cancellationToken: ct
                );
            }
            return serviceAd;
        }
        public async Task<ServicesModel> RefreshService(RefreshServiceRequest request, CancellationToken ct)
        {
            var serviceAd = await _dapr.GetStateAsync<ServicesModel>(
                ConstantValues.Services.StoreName,
                request.ServiceId.ToString(),
                cancellationToken: ct
            );

            serviceAd.IsRefreshed = request.IsRefreshed;
            serviceAd.RefreshExpiryDate = request.IsRefreshed ? DateTime.UtcNow.AddDays(7) : null;
            serviceAd.UpdatedAt = DateTime.UtcNow;

            await _dapr.SaveStateAsync(
                ConstantValues.Services.StoreName,
                request.ServiceId.ToString(),
                serviceAd,
                cancellationToken: ct
            );
            var upsertRequest = await IndexServiceToAzureSearch(serviceAd, ct);

            if (upsertRequest != null)
            {
                var message = new IndexMessage
                {
                    Action = "Upsert",
                    Vertical = ConstantValues.IndexNames.ServicesIndex,
                    UpsertRequest = upsertRequest
                };

                await _dapr.PublishEventAsync(
                    pubsubName: ConstantValues.PubSubName,
                    topicName: ConstantValues.PubSubTopics.IndexUpdates,
                data: message,
                    cancellationToken: ct
                );
            }
            return serviceAd;
        }
        public async Task<ServicesModel> PublishService(Guid id, CancellationToken ct)
        {
            var serviceAd = await _dapr.GetStateAsync<ServicesModel>(
                ConstantValues.Services.StoreName,
                id.ToString(),
                cancellationToken: ct
            );
            var allAdKeys = await _dapr.GetStateAsync<List<string>>(
               ConstantValues.Services.StoreName,
               ConstantValues.Services.ServicesIndexKey,
               cancellationToken: ct
            ) ?? new();

            foreach (var adKey in allAdKeys)
            {
                var existingAd = await _dapr.GetStateAsync<ServicesModel>(
                    ConstantValues.Services.StoreName,
                    adKey,
                    cancellationToken: ct
                );

                if (existingAd != null &&
                    existingAd.CreatedBy == serviceAd.CreatedBy &&
                    existingAd.L2CategoryId == serviceAd.L2CategoryId &&
                    existingAd.IsActive &&
                    existingAd.Status == ServiceStatus.Published)
                {
                    throw new InvalidDataException("You already have an active ad in this category. Please unpublish or remove it before posting another.");
                }
            }
            if (serviceAd == null)
                throw new InvalidDataException("Service Ad not found.");

            if (serviceAd.Status == ServiceStatus.Published)
                throw new InvalidDataException("Service is already published.");

            if (serviceAd.Status != ServiceStatus.Unpublished)
                throw new InvalidDataException("Unpublished Service only be published.");

            serviceAd.Status = ServiceStatus.Published;
            serviceAd.PublishedDate = DateTime.UtcNow;
            serviceAd.UpdatedAt = DateTime.UtcNow;

            await _dapr.SaveStateAsync(
                ConstantValues.Services.StoreName,
                id.ToString(),
                serviceAd,
                cancellationToken: ct
            );
            var upsertRequest = await IndexServiceToAzureSearch(serviceAd, ct);

            if (upsertRequest != null)
            {
                var message = new IndexMessage
                {
                    Action = "Upsert",
                    Vertical = ConstantValues.IndexNames.ServicesIndex,
                    UpsertRequest = upsertRequest
                };

                await _dapr.PublishEventAsync(
                    pubsubName: ConstantValues.PubSubName,
                    topicName: ConstantValues.PubSubTopics.IndexUpdates,
                data: message,
                    cancellationToken: ct
                );
            }
            return serviceAd;
        }
        public async Task<List<ServicesModel>> ModerateBulkService(BulkModerationRequest request, CancellationToken ct)
        {
            var indexKeys = await _dapr.GetStateAsync<List<string>>(
                ConstantValues.Services.StoreName,
                ConstantValues.Services.ServicesIndexKey,
                cancellationToken: ct
            ) ?? new();

            var updated = new List<ServicesModel>();

            foreach (var id in request.AdIds)
            {
                if (!indexKeys.Contains(id.ToString()))
                    continue;

                var ad = await _dapr.GetStateAsync<ServicesModel>(
                    ConstantValues.Services.StoreName,
                    id.ToString(),
                    cancellationToken: ct
                );

                if (ad is null)
                    continue;

                bool shouldUpdate = false;

                switch (request.Action)
                {
                    case BulkModerationAction.Approve:
                        if (ad.Status == ServiceStatus.PendingApproval)
                        {
                            ad.Status = ServiceStatus.Published;
                            ad.PublishedDate = DateTime.UtcNow;
                            shouldUpdate = true;
                        }
                        break;

                    case BulkModerationAction.Publish:
                        if (ad.Status == ServiceStatus.Unpublished)
                        {
                            ad.Status = ServiceStatus.Published;
                            ad.PublishedDate = DateTime.UtcNow;
                            shouldUpdate = true;
                        }
                        break;

                    case BulkModerationAction.Unpublish:
                        if (ad.Status == ServiceStatus.Published)
                        {
                            ad.Status = ServiceStatus.Unpublished;
                            shouldUpdate = true;
                        }
                        break;
                    case BulkModerationAction.UnPromote:
                        if (ad.Status == ServiceStatus.Promote)
                        {
                            ad.Status = ServiceStatus.UnPromote;
                            shouldUpdate = true;
                        }
                        break;
                        case BulkModerationAction.UnFeature:
                        if (ad.Status == ServiceStatus.Feature)
                        {
                            ad.Status = ServiceStatus.UnFeature;
                            shouldUpdate = true;
                        }
                        break;

                    case BulkModerationAction.Remove:
                        ad.Status = ServiceStatus.Rejected;
                        ad.UpdatedBy = request.UpdatedBy;
                        shouldUpdate = true;
                        break;

                    default:
                        throw new InvalidOperationException("Invalid action");
                }

                if (shouldUpdate)
                {
                    ad.UpdatedAt = DateTime.UtcNow;
                    ad.UpdatedBy = request.UpdatedBy;
                    await _dapr.SaveStateAsync(ConstantValues.Services.StoreName, id.ToString(), ad, cancellationToken: ct);
                    var upsertRequest = await IndexServiceToAzureSearch(ad, ct);

                    if (upsertRequest != null)
                    {
                        var message = new IndexMessage
                        {
                            Action = "Upsert",
                            Vertical = ConstantValues.IndexNames.ServicesIndex,
                            UpsertRequest = upsertRequest
                        };

                        await _dapr.PublishEventAsync(
                            pubsubName: ConstantValues.PubSubName,
                            topicName: ConstantValues.PubSubTopics.IndexUpdates,
                        data: message,
                            cancellationToken: ct
                        );
                    }
                    updated.Add(ad);
                }
            }

            return updated;
        }
        public async Task<ServicesStatusCountsDto> GetServiceStatusCountsAsync(string userId, CancellationToken cancellationToken = default)
        {
            var indexKeys = await _dapr.GetStateAsync<List<string>>(
                ConstantValues.Services.StoreName,
                ConstantValues.Services.ServicesIndexKey,
                cancellationToken: cancellationToken
            ) ?? new();

            if (indexKeys.Count == 0)
            {
                return new ServicesStatusCountsDto
                {
                    PublishedCount = 0,
                    PromotedCount = 0,
                    FeaturedCount = 0,
                    PromoteTotal = 0,
                    FeaturedTotal = 0,
                    PublishedTotal = 0
                };
            }

            
            var userQuotaActor = GetUserQuotaActorProxy(userId);
            var allQuotas = await userQuotaActor.GetQuotasAsync(cancellationToken);

           
            var activeQuotas = allQuotas
                .Where(q => q.EndDate >= DateTime.UtcNow &&
                            q.VerticalName?.Equals("services", StringComparison.OrdinalIgnoreCase) == true)
                .ToList();

            var promoteBudget = activeQuotas.Sum(q => q.TotalPromoteBudget ?? 0);
            var featuredBudget = activeQuotas.Sum(q => q.TotalFeatureBudget ?? 0);
            var adBudget = activeQuotas.Sum(q => q.TotalAdBudget ?? 0);

           
            var quotaIds = activeQuotas.Select(q => q.Id).ToHashSet(); 

            var ads = await _dapr.GetBulkStateAsync(
                ConstantValues.Services.StoreName,
                indexKeys,
                parallelism: 10,
                cancellationToken: cancellationToken
            );

            var serviceList = ads
                .Where(e => !string.IsNullOrWhiteSpace(e.Value))
                .Select(e => JsonSerializer.Deserialize<ServicesModel>(e.Value!, _jsonOptions))
                .Where(e =>
                    e != null &&
                    e.IsActive &&
                    e.CreatedBy == userId
                
                )
                .ToList();

            return new ServicesStatusCountsDto
            {
                PublishedCount = serviceList.Count(x => x.Status == ServiceStatus.Published),
                PromotedCount = serviceList.Count(x => x.IsPromoted),
                FeaturedCount = serviceList.Count(x => x.IsFeatured),
                PromoteTotal = promoteBudget,
                FeaturedTotal = featuredBudget,
                PublishedTotal = adBudget
            };
        }
        private IUserQuotaActor GetUserQuotaActorProxy(string userId)
        {
            Console.WriteLine($"[GetUserQuotaActorProxy] Creating actor proxy for user: {userId}");
            var actorId = new ActorId(userId);
            return ActorProxy.Create<IUserQuotaActor>(actorId, nameof(UserQuotaActor));
        }
        public async Task<ServicesStatusCountsDto> GetSubverticalStatusCountsAsync(string userId, string subVerticalName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("UserId is required.");
            if (string.IsNullOrWhiteSpace(subVerticalName))
                throw new ArgumentException("SubVerticalName is required.");

            string storeName;
            string indexKey;

            var lowerSubvertical = subVerticalName.ToLower();
            switch (lowerSubvertical)
            {
                case "classifieds":
                case "items":
                    storeName = ConstantValues.StateStoreNames.UnifiedStore;
                    indexKey = ConstantValues.IndexNames.ClassifiedsItemsIndex;
                    break;

                case "preloved":
                    storeName = ConstantValues.StateStoreNames.UnifiedStore;
                    indexKey = ConstantValues.IndexNames.ClassifiedsPrelovedIndex;
                    break;

                case "collectibles":
                    storeName = ConstantValues.StateStoreNames.UnifiedStore;
                    indexKey = ConstantValues.IndexNames.ClassifiedsCollectiblesIndex;
                    break;

                case "deals":
                    storeName = ConstantValues.StateStoreNames.UnifiedStore;
                    indexKey = ConstantValues.IndexNames.ClassifiedsDealsIndex;
                    break;

                default:
                    throw new ArgumentException($"Unsupported subvertical: {subVerticalName}");
            }

            var indexKeys = await _dapr.GetStateAsync<List<string>>(storeName, indexKey, metadata: null, cancellationToken: cancellationToken) ?? new();
            if (indexKeys.Count == 0)
                return new ServicesStatusCountsDto();

            var userQuotaActor = GetUserQuotaActorProxy(userId);
            var allQuotas = await userQuotaActor.GetQuotasAsync(cancellationToken);

            var activeQuotas = allQuotas
                .Where(q => q.EndDate >= DateTime.UtcNow &&
                            q.SubVerticalName?.Equals(subVerticalName, StringComparison.OrdinalIgnoreCase) == true)
                .ToList();

            var promoteBudget = activeQuotas.Sum(q => q.TotalPromoteBudget ?? 0);
            var featuredBudget = activeQuotas.Sum(q => q.TotalFeatureBudget ?? 0);
            var adBudget = activeQuotas.Sum(q => q.TotalAdBudget ?? 0);

            var ads = await _dapr.GetBulkStateAsync(storeName, indexKeys, parallelism: 10, cancellationToken: cancellationToken);

            int publishedCount = 0, promotedCount = 0, featuredCount = 0;

            switch (lowerSubvertical)
            {
                case "classifieds":
                case "items":
                    foreach (var adState in ads)
                    {
                        if (string.IsNullOrWhiteSpace(adState.Value)) continue;

                        try
                        {
                            var ad = JsonSerializer.Deserialize<ClassifiedsItems>(adState.Value!, _jsonOptions);
                            if (ad == null || !ad.IsActive || ad.CreatedBy != userId || !ad.SubVertical.Equals(subVerticalName, StringComparison.OrdinalIgnoreCase))
                                continue;

                            if (ad.Status == AdStatus.Published)
                                publishedCount++;
                            if (ad.IsPromoted)
                                promotedCount++;
                            if (ad.IsFeatured)
                                featuredCount++;
                        }
                        catch { continue; }
                    }
                    break;

                case "preloved":
                    foreach (var adState in ads)
                    {
                        if (string.IsNullOrWhiteSpace(adState.Value)) continue;

                        try
                        {
                            var ad = JsonSerializer.Deserialize<ClassifiedsPreloved>(adState.Value!, _jsonOptions);
                            if (ad == null || !ad.IsActive || ad.CreatedBy != userId || !ad.SubVertical.Equals(subVerticalName, StringComparison.OrdinalIgnoreCase))
                                continue;

                            if (ad.Status == AdStatus.Published)
                                publishedCount++;
                            if (ad.IsPromoted)
                                promotedCount++;
                            if (ad.IsFeatured)
                                featuredCount++;
                        }
                        catch { continue; }
                    }
                    break;

                case "collectibles":
                    foreach (var adState in ads)
                    {
                        if (string.IsNullOrWhiteSpace(adState.Value)) continue;

                        try
                        {
                            var ad = JsonSerializer.Deserialize<ClassifiedsCollectibles>(adState.Value!, _jsonOptions);
                            if (ad == null || !ad.IsActive || ad.CreatedBy != userId || !ad.SubVertical.Equals(subVerticalName, StringComparison.OrdinalIgnoreCase))
                                continue;

                            if (ad.Status == AdStatus.Published)
                                publishedCount++;
                            if (ad.IsPromoted)
                                promotedCount++;
                            if (ad.IsFeatured)
                                featuredCount++;
                        }
                        catch { continue; }
                    }
                    break;

                case "deals":
                    foreach (var adState in ads)
                    {
                        if (string.IsNullOrWhiteSpace(adState.Value)) continue;

                        try
                        {
                            var ad = JsonSerializer.Deserialize<ClassifiedDeals>(adState.Value!, _jsonOptions);
                            if (ad == null  || ad.UserId != userId || !ad.SubVertical.Equals(subVerticalName, StringComparison.OrdinalIgnoreCase))
                                continue;

                            if (ad.Status == AdStatus.Published)
                                publishedCount++;
                            if ((bool)ad.IsPromoted)
                                promotedCount++;
                            if ((bool)ad.IsFeatured)
                                featuredCount++;
                        }
                        catch { continue; }
                    }
                    break;
            }

            return new ServicesStatusCountsDto
            {
                PublishedCount = publishedCount,
                PromotedCount = promotedCount,
                FeaturedCount = featuredCount,
                PromoteTotal = promoteBudget,
                FeaturedTotal = featuredBudget,
                PublishedTotal = adBudget
            };
        }








    }
}
