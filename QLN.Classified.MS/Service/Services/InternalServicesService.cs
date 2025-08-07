using Dapr.Client;
using Microsoft.EntityFrameworkCore;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Auditlog;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.CustomException;
using QLN.Common.Infrastructure.IService.IService;
using QLN.Common.Infrastructure.Model;
using QLN.Common.Infrastructure.QLDbContext;
using QLN.Common.Infrastructure.Subscriptions;
using QLN.Common.Infrastructure.Utilities;
using System.Text.RegularExpressions;
using static QLN.Common.DTO_s.NotificationDto;


namespace QLN.Classified.MS.Service.Services
{
    public class InternalServicesService : IServices
    {
        public readonly QLClassifiedContext _dbContext;
        public readonly DaprClient _dapr;
        public readonly AuditLogger _auditLogger;
        public InternalServicesService(DaprClient dapr, AuditLogger auditLogger, QLClassifiedContext dbContext)
        {
            _dapr = dapr;
            _auditLogger = auditLogger;
            _dbContext = dbContext;
        }
        public async Task<List<CategoryDto>> GetAllCategories(CancellationToken cancellationToken = default)
        {
            var allCategories = await _dbContext.Categories
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var rootCategories = allCategories
                .Where(c => c.ParentId == null)
                .Select(c => MapCategoryRecursive(c, allCategories))
                .ToList();

            return rootCategories;
        }
        private CategoryDto MapCategoryRecursive(Category category, List<Category> allCategories)
        {
            return new CategoryDto
            {
                Id = category.Id,
                CategoryName = category.CategoryName,
                Vertical = category.Vertical.ToString(),
                SubVertical = category.SubVertical?.ToString() ?? string.Empty,
                ParentId = category.ParentId,
                Fields = allCategories
                    .Where(child => child.ParentId == category.Id)
                    .Select(child => MapFieldRecursive(child, allCategories))
                    .ToList()
            };
        }
        private FieldDto MapFieldRecursive(Category field, List<Category> allCategories)
        {
            return new FieldDto
            {
                Id = field.Id,
                CategoryName = field.CategoryName,
                Type = field.Type,
                Options = field.Options,
                Fields = allCategories
                    .Where(c => c.ParentId == field.Id)
                    .Select(c => MapFieldRecursive(c, allCategories))
                    .ToList()
            };
        }
        public async Task<CategoryDto?> GetCategoryById(long id, CancellationToken cancellationToken = default)
        {
            var allCategories = await _dbContext.Categories
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var category = allCategories.FirstOrDefault(c => c.Id == id);
            if (category == null)
                return null;

            return MapCategoryRecursive(category, allCategories);
        }
        public async Task<string> UpdateCategory(CategoryDto dto, CancellationToken cancellationToken = default)
        {
            var category = await _dbContext.Categories
                .FirstOrDefaultAsync(c => c.Id == dto.Id, cancellationToken);

            if (category == null)
                return "Category not found";

            category.CategoryName = dto.CategoryName;
            category.Vertical = Enum.Parse<Vertical>(dto.Vertical);
            category.SubVertical = Enum.TryParse<SubVertical>(dto.SubVertical, out var sub) ? sub : null;

            var allFieldDtos = FlattenFields(dto.Fields);

            foreach (var fieldDto in allFieldDtos)
            {
                var existingField = await _dbContext.Categories
                    .FirstOrDefaultAsync(c => c.Id == fieldDto.Id, cancellationToken);

                if (existingField != null)
                {
                    existingField.CategoryName = fieldDto.CategoryName;
                    existingField.Type = fieldDto.Type;
                    existingField.Options = fieldDto.Options;
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            return "Category Updated Successfully";
        }
        private List<FieldDto> FlattenFields(List<FieldDto>? fields)
        {
            var list = new List<FieldDto>();
            if (fields == null) return list;

            foreach (var field in fields)
            {
                list.Add(field);
                if (field.Fields != null)
                    list.AddRange(FlattenFields(field.Fields));
            }

            return list;
        }
        public async Task<string> CreateCategory(CategoryDto dto, CancellationToken cancellationToken)
        {
            try
            {
                if (!Enum.TryParse<Vertical>(dto.Vertical, true, out var verticalEnum))
                    return "Invalid vertical value";

                SubVertical? subVerticalEnum = null;
                if (!string.IsNullOrWhiteSpace(dto.SubVertical))
                {
                    if (Enum.TryParse<SubVertical>(dto.SubVertical, true, out var parsedSubVertical))
                        subVerticalEnum = parsedSubVertical;
                    else
                        return "Invalid sub-vertical value";
                }

                Category? mainCategory;

                if (dto.ParentId.HasValue)
                {
                    mainCategory = await _dbContext.Categories
                        .FirstOrDefaultAsync(c => c.Id == dto.ParentId.Value, cancellationToken);

                    if (mainCategory == null)
                        return $"Parent category with ID {dto.ParentId.Value} not found.";
                }
                else
                {
                    mainCategory = new Category
                    {
                        CategoryName = dto.CategoryName,
                        Vertical = verticalEnum,
                        SubVertical = subVerticalEnum,
                        ParentId = null
                    };

                    _dbContext.Categories.Add(mainCategory);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
                
                if (dto.Fields != null && dto.Fields.Any())
                {
                    foreach (var fieldDto in dto.Fields)
                    {
                        await SaveFieldRecursive(fieldDto, mainCategory.Id, verticalEnum, subVerticalEnum, cancellationToken);
                    }
                }

                await _dbContext.SaveChangesAsync(cancellationToken);
                return "Category created successfully";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }
        private async Task SaveFieldRecursive(FieldDto fieldDto, long parentId, Vertical vertical, SubVertical? subVertical, CancellationToken cancellationToken)
        {
            var category = new Category
            {
                CategoryName = fieldDto.CategoryName,
                Type = fieldDto.Type,
                Options = fieldDto.Options,
                ParentId = parentId,
                Vertical = vertical,
                SubVertical = subVertical
            };

            _dbContext.Categories.Add(category);
            await _dbContext.SaveChangesAsync(cancellationToken);

            if (fieldDto.Fields != null && fieldDto.Fields.Any())
            {
                foreach (var childField in fieldDto.Fields)
                {
                    await SaveFieldRecursive(childField, category.Id, vertical, subVertical, cancellationToken);
                }
            }
        }

        private Category MapField(FieldDto dto, Category parent, Vertical verticalEnum, SubVertical? subVerticalEnum)
        {
            var category = new Category
            {
                CategoryName = dto.CategoryName,
                Type = dto.Type,
                Options = dto.Options,
                ParentCategory = parent,
                Vertical = verticalEnum,
                SubVertical = subVerticalEnum
            };

            if (dto.Fields != null && dto.Fields.Any())
            {
                foreach (var childDto in dto.Fields)
                {
                    var childCategory = MapField(childDto, category, verticalEnum, subVerticalEnum);
                    category.CategoryFields.Add(childCategory);
                }
            }

            return category;
        }
        
        public async Task<string> CreateServiceAd(string uid, string userName, ServiceDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                string? categoryName = null;
                string? l1CategoryName = null;
                string? l2CategoryName = null;

                var mainCategory = await _dbContext.Categories
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == dto.CategoryId && c.ParentId == null, cancellationToken);

                if (mainCategory == null)
                {
                    throw new ArgumentException($"Invalid CategoryId: {dto.CategoryId}. No matching main category found.");
                }
                dto.CategoryName = mainCategory.CategoryName;

                var l1Category = await _dbContext.Categories
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == dto.L1CategoryId && c.ParentId == mainCategory.Id, cancellationToken);

                if (l1Category == null)
                {
                    throw new ArgumentException($"Invalid L1CategoryId: {dto.L1CategoryId}. Not found under main category '{mainCategory.CategoryName}'.");
                }
                dto.L1CategoryName = l1Category.CategoryName;

                var l2Category = await _dbContext.Categories
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == dto.L2CategoryId && c.ParentId == l1Category.Id, cancellationToken);

                if (l2Category == null)
                {
                    throw new ArgumentException($"Invalid L2CategoryId: {dto.L2CategoryId}. Not found under L1 category '{l1Category.CategoryName}'.");
                }
                dto.L2CategoryName = l2Category.CategoryName;

                var hasActiveAd = await _dbContext.Services
                    .AnyAsync(s =>
                        s.CreatedBy == uid &&
                        s.L2CategoryId == dto.L2CategoryId &&
                        s.IsActive &&
                        s.Status == ServiceStatus.Published,
                        cancellationToken);

                if (hasActiveAd)
                {
                    throw new ConflictException("You already have an active ad in this category. Please unpublish or remove it before posting another.");
                }
                dto.Status = GetAdStatus(dto.L1CategoryName, dto.AdType);

                var entity = new QLN.Common.Infrastructure.Model.Services
                {
                    CategoryId = dto.CategoryId,
                    L1CategoryId = dto.L1CategoryId,
                    L2CategoryId = dto.L2CategoryId,
                    CategoryName = dto.CategoryName,
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
                    Availability = dto.Availability,
                    Duration = dto.Duration,
                    Reservation = dto.Reservation,
                    SubscriptionId = null,
                    ZoneId = dto.ZoneId,
                    Longitude = dto.Longitude,
                    Lattitude = dto.Lattitude,
                    PhotoUpload = dto.PhotoUpload,
                    UserName = userName,
                    Status = dto.Status,
                    PublishedDate = dto.AdType == ServiceAdType.Subscription ? DateTime.UtcNow : null,
                    IsActive = true,
                    CreatedBy = uid,
                    CreatedAt = DateTime.UtcNow,
                    AdType = dto.AdType,
                };

                ValidateCommon(entity);

                await _dbContext.Services.AddAsync(entity, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
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
                return "Service Ad Created Successfully";
            }
            catch (ConflictException)
            {
                throw;
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
        private ServiceStatus GetAdStatus(string l1CategoryName, ServiceAdType adType)
        {
            if (adType == ServiceAdType.Subscription)
                return ServiceStatus.Published;

            if (string.Equals(l1CategoryName, "Therapeutic Services", StringComparison.OrdinalIgnoreCase))
                return ServiceStatus.PendingApproval;

            if (adType == ServiceAdType.PayToPublish)
                return ServiceStatus.PendingApproval;

            throw new ArgumentException("Invalid ServiceAdType.");
        }
        private static void ValidateCommon(QLN.Common.Infrastructure.Model.Services dto)
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

            if (dto.CategoryId <= 0 || dto.L1CategoryId <= 0 || dto.L2CategoryId <= 0)
                throw new ArgumentException("All category IDs must be provided and greater than zero.");

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
        public async Task<string> UpdateServiceAd(string userId, QLN.Common.Infrastructure.Model.Services dto, CancellationToken cancellationToken = default)
        {
            try
            {
                if (dto.Id == 0)
                    throw new ArgumentException("Service Ad ID is required for update.");

                var existing = await _dbContext.Services
                    .FirstOrDefaultAsync(s => s.Id == dto.Id && s.IsActive, cancellationToken);

                if (existing == null)
                    throw new ArgumentException("Service Ad not found for update.");

                string? categoryName = null;
                string? l1CategoryName = null;
                string? l2CategoryName = null;

                var mainCategory = await _dbContext.Categories
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == dto.CategoryId && c.ParentId == null, cancellationToken);

                if (mainCategory == null)
                {
                    throw new ArgumentException($"Invalid CategoryId: {dto.CategoryId}. No matching main category found.");
                }
                dto.CategoryName = mainCategory.CategoryName;

                var l1Category = await _dbContext.Categories
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == dto.L1CategoryId && c.ParentId == mainCategory.Id, cancellationToken);

                if (l1Category == null)
                {
                    throw new ArgumentException($"Invalid L1CategoryId: {dto.L1CategoryId}. Not found under main category '{mainCategory.CategoryName}'.");
                }
                dto.L1CategoryName = l1Category.CategoryName;

                var l2Category = await _dbContext.Categories
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == dto.L2CategoryId && c.ParentId == l1Category.Id, cancellationToken);

                if (l2Category == null)
                {
                    throw new ArgumentException($"Invalid L2CategoryId: {dto.L2CategoryId}. Not found under L1 category '{l1Category.CategoryName}'.");
                }
                dto.L2CategoryName = l2Category.CategoryName;

                var hasActiveAd = await _dbContext.Services.AnyAsync(ad =>
                    ad.CreatedBy == userId &&
                    ad.L2CategoryId == dto.L2CategoryId &&
                    ad.IsActive &&
                    ad.Status == ServiceStatus.Published &&
                    ad.Id != dto.Id,
                    cancellationToken);

                if (hasActiveAd)
                    throw new ConflictException("You already have an active ad in this category. Please unpublish or remove it before posting another.");
                dto.Status = GetAdStatus(dto.L1CategoryName, dto.AdType);

                ValidateCommon(dto);

                dto.UpdatedAt = DateTime.UtcNow;
                dto.UpdatedBy = userId;
                AdUpdateHelper.ApplySelectiveUpdates(existing, dto);

                _dbContext.Services.Update(existing);
                await _dbContext.SaveChangesAsync(cancellationToken);

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

                return "Service Ad updated successfully.";
            }
            catch (ConflictException)
            {
                throw;
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
        private async Task<CommonIndexRequest> IndexServiceToAzureSearch(QLN.Common.Infrastructure.Model.Services dto, CancellationToken cancellationToken)
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
                LastRefreshedOn = dto.LastRefreshedOn,
                IsRefreshed = dto.IsRefreshed,
                PublishedDate = dto.PublishedDate,
                ExpiryDate = dto.ExpiryDate,
                Availability = dto.Availability,
                Duration = dto.Duration,
                Reservation = dto.Reservation,
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
        public async Task<QLN.Common.Infrastructure.Model.Services?> GetServiceAdById(long id, CancellationToken cancellationToken = default)
        {
            var ad = await _dbContext.Services
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id && s.IsActive, cancellationToken);

            return ad;
        }
        public async Task<string> DeleteServiceAdById(string userId, long id, CancellationToken cancellationToken = default)
        {
            var ad = await _dbContext.Services
                .FirstOrDefaultAsync(s => s.Id == id && s.IsActive, cancellationToken);

            if (ad == null)
                throw new InvalidDataException("Active Service Ad not found.");
            ad.IsActive = false;
            ad.UpdatedAt = DateTime.UtcNow;
            ad.UpdatedBy = userId;

            await _dbContext.SaveChangesAsync(cancellationToken);

            var upsertRequest = await IndexServiceToAzureSearch(ad, cancellationToken);
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

            return "Service Ad soft-deleted successfully.";
        }
        public async Task<ServicesPagedResponse<QLN.Common.Infrastructure.Model.Services>> GetAllServicesWithPagination(BasePaginationQuery? dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var query = _dbContext.Services
                    .Where(s => s.IsActive);

                if (!string.IsNullOrWhiteSpace(dto?.Title))
                {
                    string searchTerm = dto.Title.Trim().ToLower();
                    query = query.Where(s => s.Title.ToLower().Contains(searchTerm));
                }
                if (dto?.Filters != null)
                {
                    foreach (var filter in dto.Filters)
                    {
                        string key = filter.Key.ToLower();
                        string value = filter.Value.Trim();

                        if (key == "categoryName")
                        {
                            query = query.Where(s => s.CategoryName == value);
                        }
                        else if (key == "l1categoryName")
                        {
                            query = query.Where(s => s.L1CategoryName == value);
                        }
                        else if (key == "l2categoryName")
                        {
                            query = query.Where(s => s.L2CategoryName == value);
                        }
                        else if (key == "price")
                        {
                            var parts = value.Split('-', StringSplitOptions.RemoveEmptyEntries);

                            if (parts.Length == 2)
                            {
                                bool minParsed = decimal.TryParse(parts[0], out var minPrice);
                                bool maxParsed = decimal.TryParse(parts[1], out var maxPrice);

                                if (minParsed && maxParsed)
                                {
                                    query = query.Where(s => s.Price >= minPrice && s.Price <= maxPrice);
                                }
                                else if (minParsed)
                                {
                                    query = query.Where(s => s.Price >= minPrice);
                                }
                                else if (maxParsed)
                                {
                                    query = query.Where(s => s.Price <= maxPrice);
                                }
                            }
                            else if (parts.Length == 1 && decimal.TryParse(parts[0], out var exactPrice))
                            {
                                query = query.Where(s => s.Price == exactPrice);
                            }
                        }
                        else if(key == "adType")
                        {
                            if (Enum.TryParse<ServiceAdType>(value, true, out var adType))
                            {
                                query = query.Where(s => s.AdType == adType);
                            }
                        }
                        else if (key == "status")
                        {
                            if (Enum.TryParse<ServiceStatus>(value, true, out var status))
                            {
                                query = query.Where(s => s.Status == status);
                            }
                        }
                        else if (key == "location")
                        {
                            query = query.Where(s => s.Location.ToLower().Contains(value.ToLower()));
                        }
                        else if (key == "createdby")
                        {
                            query = query.Where(s => s.CreatedBy == value);
                        }
                    }
                }
                query = dto?.SortBy?.ToLower() switch
                {
                    "asc" => query.OrderBy(s => s.CreatedAt),
                    "desc" => query.OrderByDescending(s => s.CreatedAt),
                    _ => query.OrderByDescending(s => s.CreatedAt)
                };
                var pageNumber = dto?.PageNumber ?? 1;
                var perPage = dto?.PerPage ?? 10;

                var totalCount = await query.CountAsync(cancellationToken);

                var skip = (pageNumber - 1) * perPage;

                var pagedItems = await query
                    .Skip(skip)
                    .Take(perPage)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                return new ServicesPagedResponse<QLN.Common.Infrastructure.Model.Services>
                {
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PerPage = perPage,
                    Items = pagedItems
                };
            }
            catch (InvalidDataException ex)
            {
                throw new InvalidDataException($"Error fetching all services: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while fetching all services.", ex);
            }
        }
        public async Task<QLN.Common.Infrastructure.Model.Services> PromoteService(PromoteServiceRequest request, CancellationToken ct)
        {
            var serviceAd = await _dbContext.Services
                .FirstOrDefaultAsync(s => s.Id == request.ServiceId && s.IsActive, ct);

            if (serviceAd == null)
                throw new KeyNotFoundException("Service Ad not found.");

            serviceAd.IsPromoted = request.IsPromoted;
            serviceAd.PromotedExpiryDate = request.IsPromoted ? DateTime.UtcNow.AddDays(7) : null;
            serviceAd.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(ct);
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
        public async Task<QLN.Common.Infrastructure.Model.Services> FeatureService(FeatureServiceRequest request, CancellationToken ct)
        {
            var serviceAd = await _dbContext.Services
                .FirstOrDefaultAsync(s => s.Id == request.ServiceId && s.IsActive, ct);

            if (serviceAd == null)
                throw new KeyNotFoundException("Service Ad not found.");

            serviceAd.IsFeatured = request.IsFeature;
            serviceAd.FeaturedExpiryDate = request.IsFeature ? DateTime.UtcNow.AddDays(7) : null;
            serviceAd.UpdatedAt = DateTime.UtcNow;

            _dbContext.Services.Update(serviceAd);
            await _dbContext.SaveChangesAsync(ct);

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
        public async Task<QLN.Common.Infrastructure.Model.Services> RefreshService(RefreshServiceRequest request, CancellationToken ct)
        {
            var serviceAd = await _dbContext.Services
                .FirstOrDefaultAsync(s => s.Id == request.ServiceId && s.IsActive, ct);

            if (serviceAd == null)
                throw new KeyNotFoundException("Service Ad not found.");
            serviceAd.LastRefreshedOn = request.IsRefreshed ? DateTime.UtcNow.AddDays(7) : null;
            serviceAd.IsRefreshed = serviceAd.LastRefreshedOn.HasValue && serviceAd.LastRefreshedOn.Value > DateTime.UtcNow;
            serviceAd.UpdatedAt = DateTime.UtcNow;

            _dbContext.Services.Update(serviceAd);
            await _dbContext.SaveChangesAsync(ct);

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
        public async Task<QLN.Common.Infrastructure.Model.Services> PublishService(long id, CancellationToken ct)
        {
            var serviceAd = await _dbContext.Services.FirstOrDefaultAsync(s => s.Id == id && s.IsActive, ct);
            if (serviceAd == null)
                throw new KeyNotFoundException("Service Ad not found.");

            var conflictExists = await _dbContext.Services.AnyAsync(s =>
                s.Id != id &&
                s.CreatedBy == serviceAd.CreatedBy &&
                s.L2CategoryId == serviceAd.L2CategoryId &&
                s.IsActive &&
                s.Status == ServiceStatus.Published, ct);

            if (conflictExists)
            {
                throw new ConflictException("You already have an active ad in this category. Please unpublish or remove it before posting another.");
            }

            if (serviceAd.Status == ServiceStatus.Published)
                throw new InvalidDataException("Service is already published.");

            if (serviceAd.Status != ServiceStatus.Unpublished)
                throw new InvalidDataException("Only unpublished services can be published.");

            serviceAd.Status = ServiceStatus.Published;
            serviceAd.PublishedDate = DateTime.UtcNow;
            serviceAd.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(ct);

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
        public async Task<List<QLN.Common.Infrastructure.Model.Services>> ModerateBulkService(BulkModerationRequest request, CancellationToken ct)
        {
            var ads = await _dbContext.Services
                .Where(s => request.AdIds.Contains(s.Id))
                .ToListAsync(ct);

            var updatedAds = new List<QLN.Common.Infrastructure.Model.Services>();

            foreach (var ad in ads)
            {
                bool shouldUpdate = false;

                switch (request.Action)
                {
                    case BulkModerationAction.Approve:
                        if (ad.Status == ServiceStatus.PendingApproval)
                        {
                            await EnsureNoActiveAdConflict(ad, ct);
                            ad.Status = ServiceStatus.Published;
                            ad.PublishedDate = DateTime.UtcNow;
                            shouldUpdate = true;
                        }
                        break;

                    case BulkModerationAction.Publish:
                        if (ad.Status == ServiceStatus.Unpublished)
                        {
                            await EnsureNoActiveAdConflict(ad, ct);
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

                    updatedAds.Add(ad);
                }
            }
            await _dbContext.SaveChangesAsync(ct);
            return updatedAds;
        }
        private async Task EnsureNoActiveAdConflict(QLN.Common.Infrastructure.Model.Services currentAd, CancellationToken ct)
        {
            var conflict = await _dbContext.Services.AnyAsync(s =>
                s.Id != currentAd.Id &&
                s.CreatedBy == currentAd.CreatedBy &&
                s.L2CategoryId == currentAd.L2CategoryId &&
                s.IsActive &&
                s.Status == ServiceStatus.Published, ct);

            if (conflict)
            {
                throw new ConflictException($"Ad '{currentAd.Title}' cannot be published. An active ad already exists in the same category by this user.");
            }
        }
    }
}
