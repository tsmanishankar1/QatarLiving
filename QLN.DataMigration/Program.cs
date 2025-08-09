using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
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

builder.Services.AddSingleton<IFileStorageBlobService, FileStorageBlobService>();
builder.Services.AddSingleton<IDataOutputService, DataOutputService>();

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
.WithName("MigrateCategories");

app.MapGet("/migrate_items", async (
    [FromServices] IMigrationService migrationService,
    [FromQuery] string environment,
    [FromQuery(Name = "category_id")] int categoryId,
    CancellationToken cancellationToken = default
    ) =>
    {
        return await migrationService.MigrateItems(environment, categoryId, cancellationToken);
    });

app.MapGet("/migrate_articles", async (
    [FromServices] IMigrationService migrationService,
    [FromQuery(Name = "source_category")] string sourceCategory,
    [FromQuery(Name = "destination_category")] int destinationCategory,
    [FromQuery(Name = "destination_sub_category")] int destinationSubCategory,
    [FromQuery(Name = "import_images")] bool importImages = false,
    CancellationToken cancellationToken = default
    ) =>
    {
        return await migrationService.MigrateArticles(sourceCategory, destinationCategory, destinationSubCategory, importImages, cancellationToken);
    });

app.MapGet("/migrate_events", async (
    [FromServices] IMigrationService migrationService,
    [FromQuery(Name = "source_category")] string sourceCategory,
    [FromQuery(Name = "destination_category")] int destinationCategory,
    [FromQuery(Name = "import_images")] bool importImages = false,
    CancellationToken cancellationToken = default
    ) =>
    {
        return await migrationService.MigrateEvents(sourceCategory, destinationCategory, importImages, cancellationToken);
    });

app.MapGet("/migrate_community", async (
    [FromServices] IMigrationService migrationService,
    //[FromQuery(Name = "source_category")] string sourceCategory,
    //[FromQuery(Name = "destination_category")] int destinationCategory,
    [FromQuery(Name = "import_images")] bool importImages = false,
    CancellationToken cancellationToken = default
    ) =>
    {
        return await migrationService.MigrateCommunityPosts(importImages, cancellationToken);
    });


app.Run();
