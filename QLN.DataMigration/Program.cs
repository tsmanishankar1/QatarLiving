using Microsoft.AspNetCore.Mvc;
using QLN.Common.Infrastructure.Constants;
using QLN.DataMigration.Models;
using QLN.DataMigration.Services;
using System.Net.Http.Headers;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddAuthorization();

builder.Services.AddDaprClient();

builder.Services.AddScoped<IMigrationService, MigrationService>();
builder.Services.AddHttpClient<IDrupalSourceServices, DrupalSourceServices>(); // this doesnt need a base address as it will be set in the service implementation

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

    List<DrupalCategory> categories = drupalItems.Items
        .SelectMany(i => i.Category)
        .Distinct(new DrupalCategoryTidComparer())
        .ToList();

    //var migrationItems = (MigrationItems)drupalItems;

    //await migrationService.SaveMigrationItemsAsync(migrationItems);

    return Results.Ok(new
    {
        Message = $"Items migration for {environment} completed @ {DateTime.UtcNow}.",
        Categories = categories
    });
});


app.Run();

public class DrupalCategoryTidComparer : IEqualityComparer<DrupalCategory>
{
    public bool Equals(DrupalCategory? x, DrupalCategory? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;
        return x.Tid == y.Tid;
    }

    public int GetHashCode(DrupalCategory obj)
    {
        return obj.Tid.GetHashCode();
    }
}