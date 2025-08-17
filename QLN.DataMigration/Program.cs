using CsvHelper;
using FirebaseAdmin.Messaging;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi.Models;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.IService;
using QLN.Common.Infrastructure.IService.IContentService;
using QLN.Common.Infrastructure.IService.IFileStorage;
using QLN.Common.Infrastructure.IService.IService;
using QLN.Common.Infrastructure.IService.V2IContent;
using QLN.Common.Infrastructure.Service.FileStorage;
using QLN.Common.Infrastructure.Utilities;
using QLN.DataMigration.Models;
using QLN.DataMigration.Services;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using static System.Net.Mime.MediaTypeNames;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAntiforgery();

// Add services to the container.
builder.Services.AddAuthorization();

builder.Services.AddDaprClient();

builder.Services.AddHttpClient("DaprClient")
    .ConfigureHttpClient(client =>
    {
        client.Timeout = TimeSpan.FromMinutes(20);
    });

builder.Services.AddSingleton<IFileStorageBlobService, FileStorageBlobService>();
builder.Services.AddSingleton<IDataOutputService, DataOutputService>();
builder.Services.AddSingleton<IV2CommunityPostService, CommunityPostService>();
builder.Services.AddSingleton<IV2NewsService, NewsService>();
builder.Services.AddSingleton<IV2EventService, EventsService>();
builder.Services.AddSingleton<IClassifiedService, ClassifiedsService>();
builder.Services.AddSingleton<IServices, ServicesService>();
builder.Services.AddSingleton<IExternalSubscriptionService, SubscriptionService>();

var drupalUrl = builder.Configuration.GetSection("BaseUrl")["LegacyDrupal"] ?? throw new ArgumentNullException("LegacyDrupal");
if (Uri.TryCreate(drupalUrl, UriKind.Absolute, out var drupalBaseUrl))
{
    builder.Services.AddHttpClient<IDrupalSourceService, DrupalSourceService>(option =>
    {
        option.BaseAddress = drupalBaseUrl;
    });
}
builder.Services.AddSingleton<IMigrationService, MigrationService>();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
//builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1.1.1", new OpenApiInfo
    {
        Title = "Qatar Migration API",
        Version = "v1.1.1",
        Description = "API documentation for Qatar Migration."
    });
});

builder.Services.AddRequestTimeouts(options =>
{
    options.DefaultPolicy = new RequestTimeoutPolicy { Timeout = TimeSpan.FromMinutes(20) }; // Default timeout
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    //app.MapOpenApi();

    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1.1.1/swagger.json", "v1.1.1");
        options.RoutePrefix = "Swagger";
        options.DocumentTitle = "Qatar Migrations API";
    });

}

app.UseHttpsRedirection();

app.UseAntiforgery();

//app.UseAuthorization();

// migrate categories

app.MapGet("/migrate_mobile_devices", async (
    [FromServices] IMigrationService migrationService,
    [FromQuery] string environment,
    CancellationToken cancellationToken = default
    ) =>
{
    return await migrationService.MigrateMobileDevices(environment, cancellationToken);
})
.WithSummary("Migrate Mobile Devices - Not to be used");

app.MapGet("/migrate_items", async (
    [FromServices] IMigrationService migrationService,
    [FromQuery] string environment,
    [FromQuery(Name = "import_images")] bool importImages,
    CancellationToken cancellationToken = default
    ) =>
    {
        return await migrationService.MigrateItems(environment, importImages, cancellationToken);
    })
    .WithSummary("Migrate Items - not ready yet");

app.MapGet("/migrate_item_subscriptions", async (
    [FromServices] IMigrationService migrationService,
    CancellationToken cancellationToken = default
    ) =>
{
    return await migrationService.MigrateLegacyItemsSubscriptions(cancellationToken);
})
    .WithSummary("Migrate Item Subscriptions");

app.MapGet("/migrate_services_subscriptions", async (
    [FromServices] IMigrationService migrationService,
    CancellationToken cancellationToken = default
    ) =>
{
    return await migrationService.MigrateLegacyServicesSubscriptions(cancellationToken);
})
    .WithSummary("Migrate Services Subscriptions");


app.MapGet("/migrate_event_categories", async (
    [FromServices] IMigrationService migrationService,
    CancellationToken cancellationToken = default
    ) =>
{
    return await migrationService.MigrateEventCategories(cancellationToken);
})
    .WithSummary("Migrate Event Categories - Run 1st");

app.MapGet("/migrate_news_categories", async (
    [FromServices] IMigrationService migrationService,
    CancellationToken cancellationToken = default
    ) =>
{
    return await migrationService.MigrateNewsCategories(cancellationToken);
})
    .WithSummary("Migrate News Categories - Run 1st");

app.MapGet("/migrate_articles", async (
    [FromServices] IMigrationService migrationService,
    //[FromQuery(Name = "source_category")] string sourceCategory,
    //[FromQuery(Name = "destination_category")] int destinationCategory,
    //[FromQuery(Name = "destination_sub_category")] int destinationSubCategory,
    [FromQuery(Name = "import_images")] bool importImages = false,
    CancellationToken cancellationToken = default
    ) =>
    {
        return await migrationService.MigrateArticles( importImages, cancellationToken);
    })
    .WithSummary("Migrate News Articles - Run 2nd")
    .WithRequestTimeout(TimeSpan.FromMinutes(20));

app.MapGet("/migrate_events", async (
    [FromServices] IMigrationService migrationService,
    //[FromQuery(Name = "category")] int category,
    //[FromQuery(Name = "source_category")] string sourceCategory,
    //[FromQuery(Name = "destination_category")] int destinationCategory,
    [FromQuery(Name = "import_images")] bool importImages = false,
    CancellationToken cancellationToken = default
    ) =>
    {
        return await migrationService.MigrateEvents(importImages, cancellationToken);
    })
    .WithSummary("Migrate Events - Run 2nd")
    .WithRequestTimeout(TimeSpan.FromMinutes(20));

app.MapGet("/migrate_community", async (
    [FromServices] IMigrationService migrationService,
    //[FromQuery(Name = "source_category")] string sourceCategory,
    //[FromQuery(Name = "destination_category")] int destinationCategory,
    [FromQuery(Name = "import_images")] bool importImages = false,
    CancellationToken cancellationToken = default
    ) =>
    {
        return await migrationService.MigrateCommunityPosts(importImages, cancellationToken);
    })
    .WithSummary("Migrate Community Posts - Run 2nd")
    .WithRequestTimeout(TimeSpan.FromMinutes(20));


app.MapGet("/migrate_locations", async (
    [FromServices] IMigrationService migrationService,
    CancellationToken cancellationToken = default
    ) =>
{
    return await migrationService.MigrateLocations(cancellationToken);
})
    .WithSummary("Migrate Locations - Not yet required");

app.MapPost("/import_csv_categories", async (IFormFile file) =>
{
    if (file == null || file.Length == 0)
        return Results.BadRequest("No file uploaded.");

    var totalCount = 0;
    var records = new List<CsvCategoryImportRow>();

    using (var stream = file.OpenReadStream())
    using (var reader = new StreamReader(stream))
    using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
    {
        csv.Read();
        csv.ReadHeader();
        while (csv.Read())
        {
            var record = csv.GetRecord<CsvCategoryImportRow>();
            if (record.L2Category == "NULL") record.L2Category = null;
            Console.WriteLine($"ad_id: {record.AdId}, category: {record.Category}, l1_category: {record.L1Category}, l2_category: {record.L2Category}");
            totalCount++;
            // Do something with the record.
        }
    }
    return Results.Ok(new
    {
        Message = "CSV file imported successfully.",
        TotalCount = totalCount
    });
})
.WithSummary("Import CSV file with ad_id, category, l1_category, l2_category")
.DisableAntiforgery();




app.Run();
