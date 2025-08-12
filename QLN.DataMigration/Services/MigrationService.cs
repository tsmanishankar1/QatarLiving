using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService.IFileStorage;
using QLN.Common.Infrastructure.Utilities;
using QLN.DataMigration.Models;
using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace QLN.DataMigration.Services
{
    public class MigrationService : IMigrationService
    {
        private readonly ILogger<MigrationService> _logger;
        private readonly IDataOutputService _dataOutputService;
        private readonly IDrupalSourceService _drupalSourceService;
        private readonly IFileStorageBlobService _fileStorageBlobService;

        public MigrationService(
            ILogger<MigrationService> logger,
            IDataOutputService dataOutputService,
            IDrupalSourceService drupalSourceService,
            IFileStorageBlobService fileStorageBlobService
            )
        {
            _logger = logger;
            _dataOutputService = dataOutputService;
            _drupalSourceService = drupalSourceService;
            _fileStorageBlobService = fileStorageBlobService;
        }

        public async Task<IResult> MigrateCategories(string environment, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Starting Migrations @ {DateTime.UtcNow}");

            // first fetch all results from source

            var categories = await _drupalSourceService.GetCategoriesAsync(environment, cancellationToken);

            if (categories == null || categories.Makes == null || !categories.Makes.Any())
            {
                // if we have no results return an error
                return Results.Problem("No categories found or deserialized data is invalid.");
            }

            // convert all categories to an object we ewant to store permanently
            // (this is an example, may be replaced with a formal object definition from Kishore/Mujay)

            var itemsCategories = (ItemsCategories)categories;

            _logger.LogInformation($"Completed Data Denormalization @ {DateTime.UtcNow}");

            // then we write the data away to a permanent store (in this case DAPR state)

            await _dataOutputService.SaveCategoriesAsync(itemsCategories, cancellationToken);

            // return that this was successful

            return Results.Ok(new
            {
                Message = $"Migrated {itemsCategories.Models.Count} Categories for {environment} - Completed @ {DateTime.UtcNow}.",
            });
        }

        public async Task<IResult> MigrateItems(string environment, bool importImages, CancellationToken cancellationToken)
        {
            int pageSize = 30;
            int page = 1;

            var startTime = DateTime.Now;

            _logger.LogInformation($"Starting Items Migration @ {startTime}");

            var drupalItems = await _drupalSourceService.GetItemsAsync(environment, pageSize, page, cancellationToken);

            if (drupalItems == null || drupalItems.Items == null || !drupalItems.Items.Any())
            {
                return Results.Problem("No items found or deserialized data is invalid.");
            }

            var migrationItems = new List<DrupalItem>();
            int totalCount = 0;

            // iterate over each drupal item and fetch its data from current storage
            // and upload it into Azure Blob
            foreach (var drupalItem in drupalItems.Items)
            {
                await ProcessMigrationItem(drupalItem, importImages: importImages);
                
                migrationItems.Add(drupalItem);
                totalCount += 1;
            }

            await _dataOutputService.SaveMigrationItemsAsync(migrationItems, cancellationToken); 
            
            var totalItemCount = drupalItems.Total;

            _logger.LogInformation($"Migrated {totalCount} out of {totalItemCount} Items");

            while (totalItemCount > totalCount)
            {
                page += 1;
                _logger.LogInformation($"Fetching data for page {page}");
                drupalItems = await _drupalSourceService.GetItemsAsync(environment, pageSize, page, cancellationToken);
                migrationItems = new List<DrupalItem>();
                if (drupalItems != null && drupalItems.Items.Count > 0)
                {
                    foreach (var drupalItem in drupalItems.Items)
                    {
                        await ProcessMigrationItem(drupalItem, importImages: importImages);

                        migrationItems.Add(drupalItem);
                        totalCount += 1;
                    }

                    await _dataOutputService.SaveMigrationItemsAsync(migrationItems, cancellationToken);

                    _logger.LogInformation($"Migrated {totalCount} out of {totalItemCount} Items");
                }
            }

            _logger.LogInformation($"Completed Items Migration @ {DateTime.UtcNow}");

            return Results.Ok(new
            {
                Message = $"Migrated {totalCount} out of {totalItemCount} Items - Started @ {startTime} - Completed @ {DateTime.UtcNow}.",
            });
        }

        

        public async Task<IResult> MigrateArticles(bool importImages, CancellationToken cancellationToken)
        {
            int pageSize = 200;
            int page = 1;
            string sourceCategory= "20000040"; // News
            int destinationCategory = 101; // News
            int destinationSubCategory = 1001; // News

            var startTime = DateTime.Now;

            _logger.LogInformation($"Starting Items Migration @ {startTime}");

            var drupalItems = await _drupalSourceService.GetNewsFromDrupalAsync(sourceCategory, cancellationToken, page: page, page_size: pageSize);

            if (drupalItems == null || drupalItems.Items == null || !drupalItems.Items.Any())
            {
                return Results.Problem("No items found or deserialized data is invalid.");
            }

            var migrationItems = new List<ArticleItem>();
            int totalCount = 0;

            // iterate over each drupal item and fetch its data from current storage
            // and upload it into Azure Blob
            foreach (var drupalItem in drupalItems.Items)
            {
                await ProcessMigrationArticle(drupalItem, importImages: importImages);

                migrationItems.Add(drupalItem);
                totalCount += 1;
            }

            await _dataOutputService.SaveContentNewsAsync(migrationItems, destinationCategory, destinationSubCategory, cancellationToken);

            int.TryParse(drupalItems.Total, out var totalItemCount);

            _logger.LogInformation($"Migrated {totalCount} out of {totalItemCount} Items");

            while (totalItemCount > totalCount)
            {
                page += 1;
                _logger.LogInformation($"Fetching data for page {page}");
                drupalItems = await _drupalSourceService.GetNewsFromDrupalAsync(sourceCategory, cancellationToken, page: page, page_size: pageSize);
                migrationItems = new List<ArticleItem>();
                if (drupalItems != null && drupalItems.Items.Count > 0)
                {
                    foreach (var drupalItem in drupalItems.Items)
                    {
                        await ProcessMigrationArticle(drupalItem, importImages: importImages);

                        migrationItems.Add(drupalItem);
                        totalCount += 1;
                    }

                    await _dataOutputService.SaveContentNewsAsync(migrationItems, destinationCategory, destinationSubCategory, cancellationToken);

                    _logger.LogInformation($"Migrated {totalCount} out of {totalItemCount} Items"); 
                }
            }

            _logger.LogInformation($"Completed Items Migration @ {DateTime.UtcNow}");

            return Results.Ok(new
            {
                Message = $"Migrated {totalCount} out of {totalItemCount} Items - Started @ {startTime} - Completed @ {DateTime.UtcNow}.",
            });
        }

        public async Task<IResult> MigrateEvents(bool importImages, CancellationToken cancellationToken)
        {
            int pageSize = 200;
            int page = 1;

            var startTime = DateTime.Now;

            _logger.LogInformation($"Starting Items Migration @ {startTime}");

            var drupalItems = await _drupalSourceService.GetEventsFromDrupalAsync(cancellationToken, page: page, page_size: pageSize);

            if (drupalItems == null || drupalItems.Items == null || !drupalItems.Items.Any())
            {
                return Results.Problem("No items found or deserialized data is invalid.");
            }

            var migrationItems = new List<ContentEvent>();
            int totalCount = 0;

            // iterate over each drupal item and fetch its data from current storage
            // and upload it into Azure Blob
            foreach (var drupalItem in drupalItems.Items)
            {
                await ProcessMigrationEvent(drupalItem, importImages: importImages);

                migrationItems.Add(drupalItem);
                totalCount += 1;
            }

            await _dataOutputService.SaveContentEventsAsync(migrationItems, cancellationToken);

            //int.TryParse(drupalItems.Total, out var totalItemCount);
            int totalItemCount = drupalItems.Total;

            _logger.LogInformation($"Migrated {totalCount} out of {totalItemCount} Items");

            while (totalItemCount > totalCount)
            {
                page += 1;
                _logger.LogInformation($"Fetching data for page {page}");
                drupalItems = await _drupalSourceService.GetEventsFromDrupalAsync(cancellationToken, page: page, page_size: pageSize);
                migrationItems = new List<ContentEvent>();

                if (drupalItems != null && drupalItems.Items.Count > 0)
                {
                    foreach (var drupalItem in drupalItems.Items)
                    {
                        await ProcessMigrationEvent(drupalItem, importImages: importImages);

                        migrationItems.Add(drupalItem);
                        totalCount += 1;
                    }

                    await _dataOutputService.SaveContentEventsAsync(migrationItems, cancellationToken);

                    _logger.LogInformation($"Migrated {totalCount} out of {totalItemCount} Items");
                }
            }

            _logger.LogInformation($"Completed Items Migration @ {DateTime.UtcNow}");

            return Results.Ok(new
            {
                Message = $"Migrated {totalCount} out of {totalItemCount} Items - Started @ {startTime} - Completed @ {DateTime.UtcNow}.",
            });
        }

        public async Task<IResult> MigrateCommunityPosts(
            //string sourceCategory, 
            //int destinationCategory, 
            bool importImages, 
            CancellationToken cancellationToken
            )
        {
            int pageSize = 200;
            int page = 1;

            var startTime = DateTime.Now;

            _logger.LogInformation($"Starting Items Migration @ {startTime}");

            var drupalItems = await _drupalSourceService.GetCommunitiesFromDrupalAsync(cancellationToken, page: page, page_size: pageSize);

            if (drupalItems == null || drupalItems.Items == null || !drupalItems.Items.Any())
            {
                return Results.Problem("No items found or deserialized data is invalid.");
            }

            var migrationItems = new List<CommunityPost>();
            int totalCount = 0;

            // iterate over each drupal item and fetch its data from current storage
            // and upload it into Azure Blob
            foreach (var drupalItem in drupalItems.Items)
            {
                await ProcessMigrationCommunity(drupalItem, importImages: importImages);

                migrationItems.Add(drupalItem);
                totalCount += 1;
            }

            await _dataOutputService.SaveContentCommunityPostsAsync(migrationItems, cancellationToken);

            int.TryParse(drupalItems.Total, out var totalItemCount);
            //int totalItemCount = drupalItems.Total;

            _logger.LogInformation($"Migrated {totalCount} out of {totalItemCount} Items");

            while (totalItemCount > totalCount)
            {
                page += 1;
                _logger.LogInformation($"Fetching data for page {page}");
                drupalItems = await _drupalSourceService.GetCommunitiesFromDrupalAsync(cancellationToken, page: page, page_size: pageSize);
                migrationItems = new List<CommunityPost>();
                if (drupalItems != null && drupalItems.Items.Count > 0)
                {
                    foreach (var drupalItem in drupalItems.Items)
                    {
                        await ProcessMigrationCommunity(drupalItem, importImages: importImages);

                        migrationItems.Add(drupalItem);
                        totalCount += 1;
                    }

                    await _dataOutputService.SaveContentCommunityPostsAsync(migrationItems, cancellationToken);

                    _logger.LogInformation($"Migrated {totalCount} out of {totalItemCount} Items");
                }
            }

            _logger.LogInformation($"Completed Items Migration @ {DateTime.UtcNow}");

            return Results.Ok(new
            {
                Message = $"Migrated {totalCount} out of {totalItemCount} Items - Started @ {startTime} - Completed @ {DateTime.UtcNow}.",
            });
        }

        private async Task ProcessMigrationItem(DrupalItem drupalItem, bool importImages)
        {
            if (importImages)
            {

                var uploadedBlobKeys = new List<string>();

                foreach (var item in drupalItem.Images)
                {
                    // Check if the item is a valid URL
                    if (Uri.TryCreate(item, UriKind.Absolute, out var uriResult) &&
                        (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
                    {
                        try
                        {
                            using var httpClient = new HttpClient();
                            var fileBytes = await httpClient.GetByteArrayAsync(item);

                            _logger.LogInformation($"Downloaded {Path.GetFileName(uriResult.LocalPath)} @ {DateTime.UtcNow}");

                            // Generate a custom file name from the URL
                            var customName = uriResult.AbsolutePath;

                            // Upload to blob storage
                            var url = await _fileStorageBlobService.SaveFile(fileBytes, customName, "migration-images");
                            //var url = $"https://replacement.url{customName}";

                            _logger.LogInformation($"Uploaded {customName} @ {DateTime.UtcNow}");

                            uploadedBlobKeys.Add(url);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogInformation($"Failed to process image URL '{item}': {ex.Message}");
                        }
                    }
                    else
                    {
                        _logger.LogInformation($"Skipped non-URL item: {item}");
                    }
                }

                drupalItem.Images = uploadedBlobKeys;

                _logger.LogInformation($"Processed {drupalItem.Images.Count} Images for {drupalItem.Slug}");
            }

            drupalItem.Slug = drupalItem.Slug?.Split('/')?.LastOrDefault() ?? ProcessingHelpers.GenerateSlug(drupalItem.Title);
        }
        private async Task ProcessMigrationArticle(ArticleItem drupalItem, bool importImages)
        {
            // Check if the item is a valid URL
            if (importImages && Uri.TryCreate(drupalItem.ImageUrl, UriKind.Absolute, out var uriResult) &&
                    (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
            {
                try
                {
                    using var httpClient = new HttpClient();
                    var fileBytes = await httpClient.GetByteArrayAsync(drupalItem.ImageUrl);

                    _logger.LogInformation($"Downloaded {Path.GetFileName(uriResult.LocalPath)} @ {DateTime.UtcNow}");

                    // Generate a custom file name from the URL
                    var customName = uriResult.AbsolutePath;

                    // Upload to blob storage
                    var url = await _fileStorageBlobService.SaveFile(fileBytes, customName, "migration-images");
                    //var url = $"https://replacement.url{customName}";

                    _logger.LogInformation($"Uploaded {customName} @ {DateTime.UtcNow}");

                    drupalItem.ImageUrl = url;
                }
                catch (Exception ex)
                {
                    _logger.LogInformation($"Failed to process image URL '{drupalItem.ImageUrl}': {ex.Message}");
                }

                _logger.LogInformation($"Processed Image for {drupalItem.Slug}");
            }
            else
            {
                _logger.LogDebug($"Skipped migrating: {drupalItem.ImageUrl}");
            }

            //drupalItem.Slug = ProcessingHelpers.GenerateSlug(drupalItem.Title);
            
            drupalItem.Slug = drupalItem.Slug?.Split('/')?.LastOrDefault() ?? ProcessingHelpers.GenerateSlug(drupalItem.Title);

            _logger.LogInformation($"Processed {drupalItem.Slug}");
        }

        private async Task ProcessMigrationEvent(ContentEvent drupalItem, bool importImages)
        {
            // Check if the item is a valid URL
            if (importImages && Uri.TryCreate(drupalItem.ImageUrl, UriKind.Absolute, out var uriResult) &&
                    (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
            {
                try
                {
                    using var httpClient = new HttpClient();
                    var fileBytes = await httpClient.GetByteArrayAsync(drupalItem.ImageUrl);

                    _logger.LogInformation($"Downloaded {Path.GetFileName(uriResult.LocalPath)} @ {DateTime.UtcNow}");

                    // Generate a custom file name from the URL
                    var customName = uriResult.AbsolutePath;

                    // Upload to blob storage
                    var url = await _fileStorageBlobService.SaveFile(fileBytes, customName, "migration-images");
                    //var url = $"https://replacement.url{customName}";

                    _logger.LogInformation($"Uploaded {customName} @ {DateTime.UtcNow}");

                    drupalItem.ImageUrl = url;
                }
                catch (Exception ex)
                {
                    _logger.LogInformation($"Failed to process image URL '{drupalItem.ImageUrl}': {ex.Message}");
                }

                _logger.LogInformation($"Processed Image for {drupalItem.Slug}");
            }
            else
            {
                _logger.LogInformation($"Skipped migrating: {drupalItem.ImageUrl}");
            }

            //drupalItem.Slug = ProcessingHelpers.GenerateSlug(drupalItem.Title);

            drupalItem.Slug = drupalItem.Slug?.Split('/')?.LastOrDefault() ?? ProcessingHelpers.GenerateSlug(drupalItem.Title);

            _logger.LogInformation($"Processed {drupalItem.Slug}");

        }

        private async Task ProcessMigrationCommunity(CommunityPost drupalItem, bool importImages)
        {
            // Check if the item is a valid URL
            if (importImages && Uri.TryCreate(drupalItem.ImageUrl, UriKind.Absolute, out var uriResult) &&
                    (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
            {
                try
                {
                    using var httpClient = new HttpClient();
                    var fileBytes = await httpClient.GetByteArrayAsync(drupalItem.ImageUrl);

                    _logger.LogInformation($"Downloaded {Path.GetFileName(uriResult.LocalPath)} @ {DateTime.UtcNow}");

                    // Generate a custom file name from the URL
                    var customName = uriResult.AbsolutePath;

                    // Upload to blob storage
                    var url = await _fileStorageBlobService.SaveFile(fileBytes, customName, "migration-images");
                    //var url = $"https://replacement.url{customName}";

                    _logger.LogInformation($"Uploaded {customName} @ {DateTime.UtcNow}");

                    drupalItem.ImageUrl = url;
                }
                catch (Exception ex)
                {
                    _logger.LogInformation($"Failed to process image URL '{drupalItem.ImageUrl}': {ex.Message}");
                }

                _logger.LogInformation($"Processed Image for {drupalItem.Slug}");
            }
            else
            {
                _logger.LogInformation($"Skipped migrating: {drupalItem.ImageUrl}");
            }

            //drupalItem.Slug = ProcessingHelpers.GenerateSlug(drupalItem.Title);

            drupalItem.Slug = drupalItem.Slug?.Split('/')?.LastOrDefault() ?? ProcessingHelpers.GenerateSlug(drupalItem.Title);

            _logger.LogInformation($"Processed {drupalItem.Slug}");

        }

        public async Task<IResult> MigrateEventCategories(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Starting Migrations @ {DateTime.UtcNow}");

            // first fetch all results from source

            var categories = await _drupalSourceService.GetCategoriesFromDrupalAsync(cancellationToken);

            if (categories == null || categories.EventCategories == null || !categories.EventCategories.Any())
            {
                // if we have no results return an error
                return Results.Problem("No categories found or deserialized data is invalid.");
            }

            _logger.LogInformation($"Completed Data Denormalization @ {DateTime.UtcNow}");

            await _dataOutputService.SaveEventCategoriesAsync(categories.EventCategories, cancellationToken);

            // return that this was successful

            return Results.Ok(new
            {
                Message = $"Migrated {categories.EventCategories.Count} Categories - Completed @ {DateTime.UtcNow}.",
            });
        }

        public async Task<IResult> MigrateNewsCategories(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Starting Migrations @ {DateTime.UtcNow}");
            List<NewsCategory> categories = Constants.NewsCategories();

            _logger.LogInformation($"Completed Data Denormalization @ {DateTime.UtcNow}");

            await _dataOutputService.SaveNewsCategoriesAsync(categories, cancellationToken);

            // return that this was successful

            return Results.Ok(new
            {
                Message = $"Migrated {categories.Count} Primary Categories & {categories.SelectMany(x => x.SubCategories).ToList().Count} Subcategories - Completed @ {DateTime.UtcNow}."
            });
        }

        public async Task<IResult> MigrateLocations(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Starting Migrations @ {DateTime.UtcNow}");

            var categories = await _drupalSourceService.GetCategoriesFromDrupalAsync(cancellationToken);

            if (categories == null || categories.Locations == null || !categories.Locations.Any())
            {
                // if we have no results return an error
                return Results.Problem("No categories found or deserialized data is invalid.");
            }

            _logger.LogInformation($"Completed Data Denormalization @ {DateTime.UtcNow}");

            await _dataOutputService.SaveLocationsAsync(categories.Locations, cancellationToken);

            // return that this was successful

            return Results.Ok(new
            {
                Message = $"Migrated {categories.Locations.Count} Locations & {categories.Locations.SelectMany(x => x.Areas ?? new List<Area>()).ToList().Count} Areas - Completed @ {DateTime.UtcNow}."
            });
        }
    }
}
