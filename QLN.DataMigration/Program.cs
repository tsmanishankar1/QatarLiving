using Microsoft.AspNetCore.Mvc;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.IService.IFileStorage;
using QLN.Common.Infrastructure.Service.FileStorage;
using QLN.Common.Infrastructure.Utilities;
using QLN.DataMigration.Models;
using QLN.DataMigration.Services;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using static System.Net.Mime.MediaTypeNames;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddAuthorization();

builder.Services.AddDaprClient();

builder.Services.AddScoped<IMigrationService, MigrationService>();
builder.Services.AddHttpClient<IDrupalSourceServices, DrupalSourceServices>(); // this doesnt need a base address as it will be set in the service implementation

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddSingleton<IFileStorageBlobService, FileStorageBlobService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "v1");
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();


app.MapGet("/migrate_categories", async (
    [FromServices] IMigrationService migrationService,
    [FromServices] IDrupalSourceServices drupalSourceServices,
    [FromQuery] string environment) =>
{
    Console.WriteLine($"Starting Migrations @ {DateTime.UtcNow}");

    var categories = await drupalSourceServices.GetCategoriesAsync(environment);

    if (categories == null || categories.Makes == null || !categories.Makes.Any())
    {
        return Results.Problem("No categories found or deserialized data is invalid.");
    }

    var itemsCategories = (ItemsCategories)categories;

    Console.WriteLine($"Completed Data Denormalization  @ {DateTime.UtcNow}");

    await migrationService.SaveCategoriesAsync(itemsCategories);

    return Results.Ok(new
    {
        Message = $"Categories migration for {environment} completed @ {DateTime.UtcNow}.",
    });
})
.WithName("MigrateCategories");

app.MapGet("/migrate_items", async (
    [FromServices] IMigrationService migrationService,
    [FromServices] IDrupalSourceServices drupalSourceServices,
    [FromServices] IFileStorageBlobService fileStorageBlobService,
    [FromQuery] string environment,
    [FromQuery(Name = "category_id")] int categoryId,
    [FromQuery(Name = "sort_field")] string sortField,
    [FromQuery(Name = "sort_order")] string sortOrder,
    [FromQuery(Name = "keywords")] string keywords,
    [FromQuery(Name = "page_size")] int pageSize,
    [FromQuery(Name = "page")] int page
    ) =>
{
    Console.WriteLine($"Starting Items Migration @ {DateTime.UtcNow}");

    var drupalItems = await drupalSourceServices.GetItemsAsync(environment, categoryId, sortField, sortOrder, keywords, pageSize, page);

    if (drupalItems == null || drupalItems.Items == null || !drupalItems.Items.Any())
    {
        return Results.Problem("No items found or deserialized data is invalid.");
    }

    Console.WriteLine($"Completed Items Migration @ {DateTime.UtcNow}");

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
    
    // to an image upload function from 

    List<string> sourceFiles = drupalItems.Items
        .SelectMany(i => i.Images)
        .Distinct()
        .ToList();

    var uploadedBlobKeys = new List<string>();

    foreach (var item in sourceFiles)
    {
        // Check if the item is a valid URL
        if (Uri.TryCreate(item, UriKind.Absolute, out var uriResult) &&
            (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
        {
            try
            {
                using var httpClient = new HttpClient();
                var fileBytes = await httpClient.GetByteArrayAsync(item);

                // Generate a custom file name from the URL
                var customName = Path.GetFileName(uriResult.LocalPath);

                // Upload to blob storage
                var url = await fileStorageBlobService.SaveFile(fileBytes, customName, "migration-images");

                Console.WriteLine($"Uploaded {customName} @ {DateTime.UtcNow}");

                uploadedBlobKeys.Add(url);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to process image URL '{item}': {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine($"Skipped non-URL item: {item}");
        }
    }

    // this would create the items we actually want to migrate - I have
    // implemented a data normalization process to "flatten" the data,
    // this more closely represents what a columner structure would look
    // like with entity

    var migrationItems = (MigrationItems)drupalItems;

    await migrationService.SaveMigrationItemsAsync(migrationItems);

    return Results.Ok(new
    {
        Message = $"Items migration for {environment} completed @ {DateTime.UtcNow}.",
        Categories = consolidatedCategories,
        Locations = locations,
        Zones = zones,
        Offers = offers,
        UploadedBlobs = uploadedBlobKeys
    });
});


app.Run();
