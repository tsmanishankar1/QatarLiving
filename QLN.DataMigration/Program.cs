using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.IService.IContentService;
using QLN.Common.Infrastructure.IService.IFileStorage;
using QLN.Common.Infrastructure.IService.V2IContent;
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

builder.Services.AddSingleton<IFileStorageBlobService, FileStorageBlobService>();
builder.Services.AddSingleton<IDataOutputService, DataOutputService>();
builder.Services.AddSingleton<IV2CommunityPostService, CommunityPostService>();
builder.Services.AddSingleton<IV2NewsService, NewsService>();
builder.Services.AddSingleton<IV2EventService, EventsService>();

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
builder.Services.AddOpenApi();


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

// migrate categories

app.MapGet("/migrate_categories", async (
    [FromServices] IMigrationService migrationService,
    [FromQuery] string environment,
    CancellationToken cancellationToken = default
    ) =>
{
    return await migrationService.MigrateCategories(environment, cancellationToken);
})
.WithName("Migrate Categories - Not to be used");

app.MapGet("/migrate_items", async (
    [FromServices] IMigrationService migrationService,
    [FromQuery] string environment,
    [FromQuery(Name = "category_id")] int categoryId,
    CancellationToken cancellationToken = default
    ) =>
    {
        return await migrationService.MigrateItems(environment, categoryId, cancellationToken);
    })
    .WithName("Migrate Items - not ready yet");


app.MapGet("/migrate_event_categories", async (
    [FromServices] IMigrationService migrationService,
    CancellationToken cancellationToken = default
    ) =>
{
    return await migrationService.MigrateEventCategories(cancellationToken);
})
    .WithName("Migrate Event Categories - Run 1st");

app.MapGet("/migrate_news_categories", async (
    [FromServices] IMigrationService migrationService,
    CancellationToken cancellationToken = default
    ) =>
{
    return await migrationService.MigrateNewsCategories(cancellationToken);
})
    .WithName("Migrate News Categories - Run 1st");

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
    .WithName("Migrate News Articles - Run 2nd");

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
    .WithName("Migrate Events - Run 2nd");

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
    .WithName("Migrate Community Posts - Run 2nd");


app.MapGet("/migrate_locations", async (
    [FromServices] IMigrationService migrationService,
    CancellationToken cancellationToken = default
    ) =>
{
    return await migrationService.MigrateLocations(cancellationToken);
})
    .WithName("Migrate Locations - Not yet required");

app.Run();
