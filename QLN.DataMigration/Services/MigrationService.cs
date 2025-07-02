using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QLN.Common.Infrastructure.IService.IFileStorage;
using QLN.DataMigration.Models;
using System;
using System.Diagnostics;

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

        public async Task<IResult> MigrateCategories(string environment)
        {
            _logger.LogInformation($"Starting Migrations @ {DateTime.UtcNow}");

            // first fetch all results from source

            var categories = await _drupalSourceService.GetCategoriesAsync(environment);

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

            await _dataOutputService.SaveCategoriesAsync(itemsCategories);

            // return that this was successful

            return Results.Ok(new
            {
                Message = $"Migrated {itemsCategories.Models.Count} Categories for {environment} - Completed @ {DateTime.UtcNow}.",
            });
        }

        public async Task<IResult> MigrateItems(string environment, int categoryId)
        {

            string sortField = "price";
            string sortOrder = "desc";
            string? keywords = "";
            int pageSize = 30;
            int page = 1;

            _logger.LogInformation($"Starting Items Migration @ {DateTime.UtcNow}");

            var drupalItems = await _drupalSourceService.GetItemsAsync(environment, categoryId, sortField, sortOrder, keywords, pageSize, page);

            if (drupalItems == null || drupalItems.Items == null || !drupalItems.Items.Any())
            {
                return Results.Problem("No items found or deserialized data is invalid.");
            }

            // the following collections would allow us to capturre existing
            // "linked references" for the flattened data structures

            List<DrupalCategory> categoryParents = drupalItems.Items
                .Select(i => i.CategoryParent)
                .Distinct(new DrupalCategoryTidComparer())
                .ToList();

            List<DrupalCategory> categories = drupalItems.Items
                .SelectMany(i => i.Category)
                .Distinct(new DrupalCategoryTidComparer())
                .ToList();

            List<DrupalCategory> linkedCategories = drupalItems.Items
                .SelectMany(i => i.LinkedCategories)
                .Distinct(new DrupalCategoryTidComparer())
                .ToList();

            // Review this 
            List<DrupalCategory> consolidatedCategories =
            [
                .. categoryParents,
                .. categories,
                .. linkedCategories,
            ];

            consolidatedCategories = consolidatedCategories
            .Distinct(new DrupalCategoryTidComparer())
            .ToList();

            // The above categories should maybe be merged into one large Categories listing ?

            List<DrupalLocation?> locations = drupalItems.Items
                .Select(i => i.Location)
                .Distinct(new DrupalLocationTidComparer())
                .ToList();

            List<DrupalZone?> zones = drupalItems.Items
                .Select(i => i.Zone)
                .Distinct(new DrupalZoneTidComparer())
                .ToList();

            List<DrupalOffer?> offers = drupalItems.Items
                .Select(i => i.Offer)
                .Distinct(new DrupalOfferTidComparer())
                .ToList();

            // Upload the files to blob storage
            // Create a distinct list of images to fetch from existing URLs

            var migrationItems = new List<MigrationItem>();

            // iterate over each drupal item and fetch its data from current storage
            // and upload it into Azure Blob
            foreach (var drupalItem in drupalItems.Items)
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

                migrationItems.Add((MigrationItem)drupalItem);
            }

            // this would create the items we actually want to migrate - I have
            // implemented a data normalization process to "flatten" the data,
            // this more closely represents what a columner structure would look
            // like with entity

            await _dataOutputService.SaveMigrationItemsAsync(migrationItems);

            _logger.LogInformation($"Completed Items Migration @ {DateTime.UtcNow}");

            return Results.Ok(new
            {
                Message = $"Migrated {migrationItems.Count} Items for {environment} - Completed @ {DateTime.UtcNow}.",
                Items = migrationItems,
                Categories = consolidatedCategories,
                Locations = locations,
                Zones = zones,
                Offers = offers
            });
        }
    }
}
