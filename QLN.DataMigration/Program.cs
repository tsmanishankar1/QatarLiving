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

builder.Services.AddSingleton<IFileStorageBlobService, FileStorageBlobService>();
builder.Services.AddSingleton<IDataOutputService, DataOutputService>();
builder.Services.AddHttpClient<IDrupalSourceService, DrupalSourceService>(); // this doesnt need a base address as it will be set in the service implementation
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
    [FromQuery] string environment) =>
{
    return await migrationService.MigrateCategories(environment);
})
.WithName("MigrateCategories");

app.MapGet("/migrate_items", async (
    [FromServices] IMigrationService migrationService,
    [FromQuery] string environment,
    [FromQuery(Name = "category_id")] int categoryId
    ) =>
{
    return await migrationService.MigrateItems(environment, categoryId);
});


app.Run();
