
using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using QLN.Common.Infrastructure.Constants;
using QLN.DataMigration.Models;
using System.Net.Http.Headers;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddAuthorization();

builder.Services.AddDaprClient();

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
    HttpContext httpContext, 
    [FromServices] ILogger logger,
    [FromServices] DaprClient dapr,
    [FromQuery] string environment) =>
{
    using var httpClient = new HttpClient();

    var formData = new List<KeyValuePair<string, string>>
    {
        new KeyValuePair<string, string>("env", environment)
    };
    var content = new FormUrlEncodedContent(formData);

    content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

    var response = await httpClient.PostAsync(Constants.CategoriesEndpoint, content);

    if (!response.IsSuccessStatusCode)
    {
        return Results.Problem($"Failed to migrate categories. Status: {response.StatusCode}");
    }

    logger.LogInformation($"Got Response from migration endpoint {Constants.CategoriesEndpoint}");

    var json = await response.Content.ReadAsStringAsync();

    DrupalItemsCategories? categories = null;

    try
    {
        categories = JsonSerializer.Deserialize<DrupalItemsCategories>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        logger.LogInformation("Completed Deserialization");
    }
    catch (Exception ex)
    {
        return Results.Problem($"Deserialization error: {ex.Message}");
    }

    if (categories == null || categories.Makes == null || !categories.Makes.Any())
    {
        return Results.Problem("No categories found or deserialized data is invalid.");
    }

    var itemsCategories = (ItemsCategories)categories;

    logger.LogInformation("Completed Data Denormalization");

    foreach (var item in itemsCategories.Models)
    {
        await dapr.SaveStateAsync(ConstantValues.StateStoreNames.CommonStore, item.Id.ToString(), item);
        logger.LogInformation($"Saving {item.Name} with ID {item.Id} to state");
    }

    logger.LogInformation("Completed saving all state");

    return Results.Ok(new
    {
        Message = $"Categories migration for {environment} completed.",
    });
})
.WithName("MigrateCategories");


app.Run();
