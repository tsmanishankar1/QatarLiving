namespace QLN.DataMigration.Services
{
    using Azure.Search.Documents.Indexes.Models;
    using Dapr.Client;
    using Dapr.Client.Autogen.Grpc.v1;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore.Metadata.Internal;
    using Microsoft.Extensions.Logging;
    using QLN.Common.DTO_s;
    using QLN.Common.DTO_s.Subscription;
    using QLN.Common.DTOs;
    using QLN.Common.Infrastructure.Constants;
    using QLN.Common.Infrastructure.DTO_s;
    using QLN.Common.Infrastructure.IService;
    using QLN.Common.Infrastructure.IService.IContentService;
    using QLN.Common.Infrastructure.IService.IProductService;
    using QLN.Common.Infrastructure.IService.IService;
    using QLN.Common.Infrastructure.IService.V2IContent;
    using QLN.Common.Infrastructure.Model;
    using QLN.Common.Infrastructure.Subscriptions;
    using QLN.Common.Infrastructure.Utilities;
    using QLN.DataMigration.Models;
    using System.Text;
    using System.Text.Json;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;

    public class DataOutputService : IDataOutputService
    {
        private readonly ILogger<DataOutputService> _logger;
        private readonly IV2EventService _eventService;
        private readonly IV2NewsService _newsService;
        private readonly IV2CommunityPostService _communityPostService;
        private readonly IClassifiedService _classifiedsService;
        private readonly IV2SubscriptionService _subscriptionService;
        private readonly IServices _servicesService;
        private readonly DaprClient _daprClient;

        public DataOutputService(
            ILogger<DataOutputService> logger,
            IV2EventService eventService,
            IV2NewsService newsService,
            IV2CommunityPostService communityPostService,
            IClassifiedService classifiedService,
            IV2SubscriptionService subscriptionService,
            IServices servicesService,
            DaprClient daprClient
            )
        {
            _logger = logger;
            _eventService = eventService;
            _newsService = newsService;
            _communityPostService = communityPostService;
            _classifiedsService = classifiedService;
            _subscriptionService = subscriptionService;
            _servicesService = servicesService;
            _daprClient = daprClient;
        }

        [Obsolete]
        public async Task SaveCategoriesAsync(ItemsCategories itemsCategories, CancellationToken cancellationToken)
        {
            // needs to be modified to call a Classifieds MS
            foreach (var item in itemsCategories.Models)
            {
                await _daprClient.SaveStateAsync(ConstantValues.StateStoreNames.CommonStore, item.Id.ToString(), item, cancellationToken: cancellationToken);
                _logger.LogInformation($"Saving {item.Name} with ID {item.Id} to state");
            }

            _logger.LogInformation("Completed saving all state");
        }

        public async Task SaveMigrationItemsAsync(List<ItemsCategoryMapper> csvImport, List<DrupalItem> migrationItems, CancellationToken cancellationToken)
        {
            const int CollectablesCategory = 7311;

            // needs to be modified to call a Classifieds MS
            foreach (var item in migrationItems)
            {
                if (long.TryParse(item.AdId, out var id))
                {
                    try
                    {
                        if (item.CategoryParent.Tid != CollectablesCategory)
                        {
                            var entity = new Items
                            {
                                Id = id,
                                Title = item.Title,
                                Description = item.Desc,
                                Price = item.Price,
                                SubVertical = SubVertical.Items,
                                UserName = item.Author.Username,
                                CreatedAt = DateTime.TryParse(item.CreatedDate, out var createdAt) ? createdAt : DateTime.UtcNow,
                                LastRefreshedOn = DateTime.TryParse(item.RefreshedDate, out var lastRefreshed) ? lastRefreshed : DateTime.UtcNow,
                                BuildingNumber = item.BuildingNo,
                                StreetNumber = item.StreetNo,
                                Brand = item.Make?.Name,
                                Model = item.Model?.Name,
                                UserId = item.Author.Uid.ToString(),
                                ContactEmail = item.Email,
                                ContactNumberCountryCode = "+974",// assuming Qatar country code
                                ContactNumber = item.Phone ?? string.Empty,
                                WhatsappNumberCountryCode = "+974",// assuming Qatar country code
                                WhatsAppNumber = item.Whatsapp ?? item.Phone ?? string.Empty,
                                CreatedBy = item.Author.Username,
                                IsFeatured = item.Feature,
                                IsPromoted = item.Promote,
                                UpdatedAt = DateTime.UtcNow,
                                IsActive = !item.Sold,
                                Images = item.Images?.Select(img => new ImageInfo
                                {
                                    Url = img
                                }).ToList() ?? new List<ImageInfo>(),
                                IsRefreshed = lastRefreshed > createdAt,
                                Latitude = item.GeoLocation?.Lat,
                                Longitude = item.GeoLocation?.Lng,
                                Location = item.Location?.Name,
                                Status = item.Published ? AdStatus.Published : AdStatus.Unpublished,
                                zone = item.Zone?.Name ?? string.Empty,
                                PublishedDate = DateTime.TryParse(item.CreatedDate, out var publishedDate) ? publishedDate : DateTime.UtcNow, // not sure if this is required

                            };

                            var categoryMapper = csvImport.FirstOrDefault(x => x.AdId == item.AdId);

                            if (categoryMapper != null)
                            {
                                entity.CategoryId = item.CategoryParent.Tid; // may need to look this up
                                entity.Category = categoryMapper.Category;
                                entity.L1Category = categoryMapper.L1Category;
                                entity.L2Category = categoryMapper.L2Category;
                                entity.AdType = AdTypeEnum.Subscription; // need to see if I can know this
                                //entity.SubscriptionId = categoryMapper.SubscriptionId;
                            }

                            await _classifiedsService.MigrateClassifiedItemsAd(entity, cancellationToken);

                        }
                        else
                        {
                            var entity = new Collectibles
                            {
                                Id = id,
                                Title = item.Title,
                                Description = item.Desc,
                                Price = item.Price,
                                AdType = AdTypeEnum.P2P, // collectables are only P2P
                                SubVertical = SubVertical.Collectibles,
                                UserName = item.Author.Username,
                                CreatedAt = DateTime.TryParse(item.CreatedDate, out var createdAt) ? createdAt : DateTime.UtcNow,
                                BuildingNumber = item.BuildingNo,
                                StreetNumber = item.StreetNo,
                                Brand = item.Make?.Name,
                                Model = item.Model?.Name,
                                UserId = item.Author.Uid.ToString(),
                                ContactEmail = item.Email,
                                ContactNumberCountryCode = "+974",// assuming Qatar country code
                                ContactNumber = item.Phone ?? string.Empty,
                                WhatsappNumberCountryCode = "+974",// assuming Qatar country code
                                WhatsAppNumber = item.Whatsapp ?? item.Phone ?? string.Empty,
                                CreatedBy = item.Author.Username,
                                IsFeatured = item.Feature,
                                IsPromoted = item.Promote,
                                UpdatedAt = DateTime.UtcNow,
                                IsActive = true,
                                Images = item.Images?.Select(img => new ImageInfo
                                {
                                    Url = img
                                }).ToList() ?? new List<ImageInfo>(),
                                Latitude = item.GeoLocation?.Lat,
                                Longitude = item.GeoLocation?.Lng,
                                Location = item.Location?.Name,
                                Status = item.Published ? AdStatus.Published : AdStatus.Unpublished,
                                zone = item.Zone?.Name ?? string.Empty,
                                PublishedDate = DateTime.TryParse(item.CreatedDate, out var publishedDate) ? publishedDate : DateTime.UtcNow, // not sure if this is required

                            };

                            await _classifiedsService.MigrateClassifiedCollectiblesAd(entity, cancellationToken);
                        }

                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Failed to create articles - {ex.Message}");
                        throw new Exception("Unexpected error during article creation", ex);
                    }
                }
            }
            _logger.LogInformation("Completed saving all items to state");
        }

        public async Task SaveMigrationServicesAsync(List<ServicesCategoryMapper> csvImport, List<DrupalItem> migrationItems, CancellationToken cancellationToken, bool isFreeAds = false)
        {
            foreach (var item in migrationItems)
            {
                if (long.TryParse(item.AdId, out var id))
                {
                    try
                    {
                        var entity = new Services
                        {
                            Id = id,
                            Title = item.Title,
                            Description = item.Desc,
                            Price = Convert.ToDecimal(item.Price),
                            UserName = item.Author.Username,
                            CreatedAt = DateTime.TryParse(item.CreatedDate, out var createdAt) ? createdAt : DateTime.UtcNow,
                            LastRefreshedOn = DateTime.TryParse(item.RefreshedDate, out var lastRefreshed) ? lastRefreshed : DateTime.UtcNow,
                            BuildingNumber = item.BuildingNo,
                            StreetNumber = item.StreetNo,
                            EmailAddress = item.Email,
                            PhoneNumberCountryCode = "+974",// assuming Qatar country code
                            PhoneNumber = item.Phone ?? string.Empty,
                            WhatsappNumberCountryCode = "+974",// assuming Qatar country code
                            WhatsappNumber = item.Whatsapp ?? item.Phone ?? string.Empty,
                            CreatedBy = item.Author.Username,
                            IsFeatured = item.Feature,
                            IsPromoted = item.Promote,
                            UpdatedAt = DateTime.UtcNow,
                            IsActive = !item.Sold,
                            PhotoUpload = item.Images?.Select(img => new ImageDto
                            {
                                Url = img
                            }).ToList() ?? new List<ImageDto>(),
                            IsRefreshed = lastRefreshed > createdAt,
                            Lattitude = item.GeoLocation != null ? Convert.ToDecimal(item.GeoLocation.Lat) : 0,
                            Longitude = item.GeoLocation != null ? Convert.ToDecimal(item.GeoLocation.Lng) : 0,
                            Location = item.Location?.Name ?? string.Empty,
                            Status = item.Published ? ServiceStatus.Published : ServiceStatus.Unpublished,
                            ZoneId = item.Zone?.Tid.ToString() ?? string.Empty, // NOTE: not sure if this works
                            PublishedDate = DateTime.TryParse(item.CreatedDate, out var publishedDate) ? publishedDate : DateTime.UtcNow, // not sure if this is required

                        };

                        var categoryMapper = csvImport.FirstOrDefault(x => x.AdId == item.AdId);

                        if (categoryMapper != null)
                        {
                            entity.CategoryId = item.CategoryParent.Tid; // may need to look this up
                            entity.CategoryName = categoryMapper.Category;
                            entity.L1CategoryName = categoryMapper.L1Category;
                            entity.L2CategoryName = categoryMapper.L2Category;
                            entity.AdType = ServiceAdType.Subscription; // need to see if I can know this
                                                                        //entity.SubscriptionId = categoryMapper.SubscriptionId;
                        }

                        if (isFreeAds)
                        {
                            // create a corresponding subscription for this ad and store it in a dictionary
                            // with an index based on username for additional lookups and reuse of the same subscription
                        }

                        await _servicesService.MigrateServiceAd(entity, cancellationToken);

                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Failed to create articles - {ex.Message}");
                        throw new Exception("Unexpected error during article creation", ex);
                    }
                }
            }
            _logger.LogInformation("Completed saving all items to state");
        }

        public async Task SaveContentNewsAsync(List<ArticleItem> items, int categoryId, int subcategoryId, CancellationToken cancellationToken)
        {
            //var articles = new List<V2NewsArticleDTO>();

            foreach (var dto in items)
            {
                var articleCategories = new List<V2ArticleCategory>() { new V2ArticleCategory
                    {
                        CategoryId = categoryId,
                        SubcategoryId = subcategoryId,
                        SlotId = (int)Common.DTO_s.Slot.UnPublished
                    } };

                var article = new V2NewsArticleDTO
                {
                    Id = ProcessingHelpers.StringToGuid(dto.Nid),
                    Title = dto.Title,
                    Content = dto.Description,
                    WriterTag = dto.UserName,
                    Slug = dto.Slug,
                    IsActive = dto.Status == "1",
                    Categories = articleCategories,
                    PublishedDate = DateTime.TryParse(dto.DateCreated, out var publishedDate) ? publishedDate : DateTime.UtcNow,
                    CreatedBy = dto.UserName,
                    UpdatedBy = dto.UserName,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    authorName = dto.UserName,
                    CoverImageUrl = dto.ImageUrl,
                    UserId = dto.UserName
                };

                try
                {
                    await _newsService.MigrateNewsArticleAsync(article, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to create articles - {ex.Message}");
                    throw new Exception("Unexpected error during article creation", ex);
                }
                //articles.Add(article);
            }

        }

        public async Task SaveContentEventsAsync(List<ContentEvent> items, CancellationToken cancellationToken)
        {
            //var events = new List<V2Events>();

            foreach (var dto in items)
            {
                var id = ProcessingHelpers.StringToGuid(dto.Nid);

                var hasStartDate = DateTime.TryParse(dto.EventStart, out var startDate);
                var hasEndDate = DateTime.TryParse(dto.EventEnd, out var endDate);

                var hasStartTime = startDate.Hour != 0; // check if start time is set to midnight
                var hasEndTime = endDate.Hour != 0; // check if end time is set to midnight (not a perfect solution but unlikely event would end here ?)

                var entity = new V2Events
                {
                    Id = id,
                    Slug = dto.Slug,
                    CategoryId = int.TryParse(dto.CategroryId, out var categoryId) ? categoryId : 0,
                    CategoryName = dto.EventCategory,
                    EventTitle = dto.Title,
                    EventType = V2EventType.FreeAcess,
                    Venue = dto.EventVenue,
                    Longitude = dto.EventLat,
                    Latitude = dto.EventLong,
                    EventDescription = dto.Description,
                    CoverImage = dto.ImageUrl,
                    IsFeatured = false,
                    PublishedDate = dto.CreatedAt,
                    IsActive = true,
                    CreatedBy = dto.UserName,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = dto.UserName,
                    Status = Common.DTO_s.EventStatus.Published
                };

                var eventSchedule = new EventSchedule();

                if (hasStartDate)
                {
                    eventSchedule.StartDate = new DateOnly(startDate.Year, startDate.Month, startDate.Day);
                    eventSchedule.TimeSlotType = V2EventTimeType.GeneralTime;
                    eventSchedule.GeneralTextTime = $"{dto.EventStart}";
                }

                if (hasEndDate && startDate != endDate)
                {
                    eventSchedule.EndDate = new DateOnly(endDate.Year, endDate.Month, endDate.Day);
                    eventSchedule.TimeSlotType = V2EventTimeType.GeneralTime;
                    eventSchedule.GeneralTextTime = string.IsNullOrWhiteSpace(eventSchedule.GeneralTextTime) ? $"{dto.EventEnd}" : $" - {dto.EventEnd}";
                }
                else
                {
                    eventSchedule.EndDate = new DateOnly(startDate.Year, startDate.Month, startDate.Day);
                }

                if (eventSchedule.TimeSlotType == V2EventTimeType.GeneralTime)
                {
                    entity.EventSchedule = eventSchedule;
                }

                var timeSlots = new List<TimeSlot>();

                if (hasStartTime)
                {
                    timeSlots.Add(new TimeSlot
                    {
                        DayOfWeek = startDate.DayOfWeek,
                        TextTime = startDate.ToShortTimeString(),
                    });
                }

                if (hasEndTime && startDate != endDate)
                {
                    timeSlots.Add(new TimeSlot
                    {
                        DayOfWeek = endDate.DayOfWeek,
                        TextTime = endDate.ToShortTimeString(),
                    });
                }
                    ;

                if (timeSlots.Count > 0)
                {
                    entity.EventSchedule.TimeSlotType = V2EventTimeType.PerDayTime;
                    entity.EventSchedule.TimeSlots = timeSlots;
                }

                //events.Add(entity);
                try
                {

                    await _eventService.MigrateEvent(entity, cancellationToken);

                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to create events - {ex.Message}");
                    throw new Exception("Unexpected error during events creation", ex);
                }



            }

        }

        public async Task SaveContentCommunityPostsAsync(List<CommunityPost> items, CancellationToken cancellationToken)
        {
            //var posts = new List<V2CommunityPostDto>();

            foreach (var dto in items)
            {
                var id = ProcessingHelpers.StringToGuid(dto.Nid);

                var entity = new V2CommunityPostDto
                {
                    Id = id,
                    Slug = dto.Slug,
                    CategoryId = dto.CategoryId,
                    Category = dto.Category,
                    Title = dto.Title,
                    UpdatedBy = dto.UserName,
                    UpdatedDate = DateTime.UtcNow,
                    Description = dto.Description,
                    ImageUrl = dto.ImageUrl,
                    IsActive = true,
                    UserName = dto.UserName,
                    DateCreated = DateTime.TryParse(dto.DateCreated, out var dateCreated) ? dateCreated : DateTime.UtcNow,
                    LikedUserIds = new List<string>() // creating an empty list so this will be processed into the index
                };
                //posts.Add(entity);
                try
                {

                    await _communityPostService.MigrateCommunityPostAsync(entity, cancellationToken);

                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to create community posts - {ex.Message}");
                    throw new Exception("Unexpected error during article creation", ex);
                }
            }

        }

        public async Task SaveContentCommunityCommentsAsync(Dictionary<string, List<ContentComment>> items, CancellationToken cancellationToken)
        {
            foreach (var dto in items)
            {
                var postGuid = ProcessingHelpers.StringToGuid(dto.Key);

                foreach (var comment in dto.Value)
                {
                    var id = ProcessingHelpers.StringToGuid(comment.CommentId);

                    var entity = new CommunityCommentDto
                    {
                        CommentId = id,
                        CommunityPostId = postGuid,
                        CommentedAt = DateTime.TryParse(comment.CreatedDate, out var commentDate) ? commentDate : DateTime.Now,
                        UserName = comment.Username,
                        UserId = comment.UserId,
                        Content = comment.Subject,
                        UpdatedAt = DateTime.Now,
                        IsActive = true,
                    };
                    try
                    {

                        await _communityPostService.AddCommentToCommunityPostAsync(entity, cancellationToken);

                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Failed to create community comment - {ex.Message}");
                        throw new Exception("Unexpected error during article creation", ex);
                    }
                }
            }
        }

        public async Task SaveContentNewsCommentsAsync(Dictionary<string, List<ContentComment>> items, CancellationToken cancellationToken)
        {
            foreach (var dto in items)
            {
                //var postGuid = ProcessingHelpers.StringToGuid(dto.Key);

                foreach (var comment in dto.Value)
                {
                    var id = ProcessingHelpers.StringToGuid(comment.CommentId);

                    var entity = new V2NewsCommentDto
                    {
                        CommentId = id,
                        Nid = dto.Key,
                        CommentedAt = DateTime.TryParse(comment.CreatedDate, out var commentDate) ? commentDate : DateTime.Now,
                        UserName = comment.Username,
                        Uid = comment.UserId,
                        Comment = comment.Subject,
                        UpdatedAt = DateTime.Now,
                        IsActive = true,
                    };
                    try
                    {

                        await _newsService.SaveNewsCommentAsync(entity, cancellationToken);

                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Failed to create community comment - {ex.Message}");
                        throw new Exception("Unexpected error during article creation", ex);
                    }
                }
            }
        }

        public async Task SaveEventCategoriesAsync(List<Common.Infrastructure.DTO_s.EventCategory> items, CancellationToken cancellationToken)
        {
            foreach (var dto in items)
            {
                try
                {

                    if (int.TryParse(dto.Id, out var id))
                    {
                        var entity = new EventsCategory
                        {
                            Id = id,
                            CategoryName = dto.Name
                        };

                        await _eventService.CreateCategory(entity, cancellationToken);

                        _logger.LogInformation($"Created category {dto.Id} - {dto.Name}");

                    }
                    else
                    {
                        _logger.LogError($"Failed to create category {dto.Id} - {dto.Name}");
                    }

                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to create category {dto.Id} - {ex.Message}");
                    throw new Exception("Unexpected error during article creation", ex);
                }
            }
        }

        public async Task SaveNewsCategoriesAsync(List<NewsCategory> items, CancellationToken cancellationToken)
        {
            foreach (var primaryCategory in items)
            {
                var subCategories = new List<V2NewsSubCategory>();

                foreach (var subCategory in primaryCategory.SubCategories)
                {
                    subCategories.Add(new V2NewsSubCategory
                    {
                        Id = subCategory.Id,
                        SubCategoryName = subCategory.SubCategoryName
                    });
                    _logger.LogInformation($"Created category {subCategory.Id} - {subCategory.SubCategoryName}");
                }

                try
                {
                    var entity = new V2NewsCategory
                    {
                        Id = primaryCategory.Id,
                        CategoryName = primaryCategory.CategoryName,
                        SubCategories = subCategories
                    };

                    await _newsService.AddCategoryAsync(entity, cancellationToken);

                    _logger.LogInformation($"Created category {entity.Id} - {entity.CategoryName}");

                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to create category {primaryCategory.Id} - {ex.Message}");
                    throw new Exception("Unexpected error during article creation", ex);
                }
            }
        }

        public async Task SaveLocationsAsync(List<Location> items, CancellationToken cancellationToken)
        {
            foreach (var location in items)
            {
                var areas = new List<Common.DTO_s.LocationDto.AreaDto>();

                if (location.Areas != null && location.Areas.Count > 0)
                {
                    foreach (var area in location.Areas)
                    {
                        areas.Add(new Common.DTO_s.LocationDto.AreaDto
                        {
                            Id = area.Id,
                            Name = area.Name,
                            Latitude = area.Latitude.ToString(),
                            Longitude = area.Longitude.ToString()
                        });
                        _logger.LogInformation($"Created category {area.Id} - {area.Name}");
                    }
                }

                try
                {
                    var entity = new Common.DTO_s.LocationDto.LocationEventDto
                    {
                        Id = location.Id,
                        Name = location.Name,
                        Latitude = location.Latitude.ToString(),
                        Longitude = location.Longitude.ToString(),
                        Areas = areas
                    };

                    //await _locationService.AddLocationAsync(entity, cancellationToken); // dont have a save method for this as yet

                    _logger.LogInformation($"Created category {entity.Id} - {entity.Name}");

                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to create category {location.Id} - {ex.Message}");
                    throw new Exception("Unexpected error during article creation", ex);
                }
            }
        }

        public async Task SaveLegacyServicesSubscriptionsAsync(List<SubscriptionItem> subscriptions, CancellationToken cancellationToken)
        {
            // this one needs some review in terms of business logic

            foreach (var subscription in subscriptions)
            {
                try
                {
                    if (subscription != null && subscription.Status == "success")
                    {
                        DateTime.TryParse(subscription.StartDate, out var startDate);
                        DateTime.TryParse(subscription.EndDate, out var endDate);

                        var migratedSubscription = new V2SubscriptionDto
                        {
                            Id = ProcessingHelpers.StringToGuid(subscription.SubscriptionId.ToString()), // deterministic GUID so should lways be the same
                            Quota = new SubscriptionQuota
                            {
                                TotalAdsAllowed = subscription.AdsLimitDaily,
                                TotalFeaturesAllowed = int.TryParse(subscription.FeatureLimit, out var featureLimit) ? featureLimit : 0,
                                TotalPromotionsAllowed = 0,
                                DailyRefreshesAllowed = int.TryParse(subscription.RefreshLimitDaily, out var refreshLimitDaily) ? refreshLimitDaily : 0,
                            },
                            //CategoryId = SubscriptionCategory.Services,
                            Currency = "QAR",
                            ProductName = subscription.Product,
                            StartDate = startDate,
                            EndDate = endDate,
                            Price = 0,
                            StatusId = SubscriptionStatus.Active,
                            Vertical = Vertical.Services,
                            lastUpdated = DateTime.UtcNow,
                            UserId = subscription.UserId,
                            SubVertical = SubVertical.Services
                        };

                        //await _subscriptionService.CreateSubscriptionAsync(migratedSubscription);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to save legacy services subscription - {ex.Message}");
                    throw new Exception("Unexpected error during legacy subscription saving", ex);
                }
            }
        }

        public async Task SaveLegacyItemsSubscriptionsAsync(List<SubscriptionItem> subscriptions, CancellationToken cancellationToken)
        {
            // this one needs some review in terms of business logic

            foreach (var subscription in subscriptions)
            {
                try
                {
                    if (subscription != null && subscription.Status == "success")
                    {
                        DateTime.TryParse(subscription.StartDate, out var startDate);
                        DateTime.TryParse(subscription.EndDate, out var endDate);
                        TimeSpan duration = endDate - startDate;
                        if (duration.TotalDays < 0)
                        {
                            duration = TimeSpan.FromDays(30); // Default to 30 days if the duration is negative
                        }

                        var migratedSubscription = new V2SubscriptionDto
                        {
                            Id = ProcessingHelpers.StringToGuid(subscription.SubscriptionId.ToString()), // deterministic GUID so should lways be the same
                            Quota = new SubscriptionQuota
                            {
                                TotalAdsAllowed = subscription.AdsLimitDaily,
                                TotalFeaturesAllowed = int.TryParse(subscription.FeatureLimit, out var featureLimit) ? featureLimit : 0,
                                TotalPromotionsAllowed = 0,
                                DailyRefreshesAllowed = int.TryParse(subscription.RefreshLimitDaily, out var refreshLimitDaily) ? refreshLimitDaily : 0,
                            },
                            //CategoryId = SubscriptionCategory.Services,
                            Currency = "QAR",
                            ProductName = subscription.Product,
                            StartDate = startDate,
                            EndDate = endDate,
                            Price = 0,
                            StatusId = SubscriptionStatus.Active,
                            Vertical = Vertical.Services,
                            lastUpdated = DateTime.UtcNow,
                            UserId = subscription.UserId,
                            SubVertical = SubVertical.Services
                        };

                        //await _subscriptionService.CreateSubscriptionAsync(migratedSubscription);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to save legacy item subscription - {ex.Message}");
                    throw new Exception("Unexpected error during legacy subscription saving", ex);
                }
            }
        }
    }
}
