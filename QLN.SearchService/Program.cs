using Azure;
using Azure.Search.Documents.Indexes;
using Microsoft.Extensions.Options;
using QLN.Common.Infrastructure.CustomEndpoints;
using QLN.SearchService;
using QLN.SearchService.IndexModels;
using QLN.SearchService.ServiceConfiguration;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<AzureSearchSettings>(
    builder.Configuration.GetSection("AzureSearch"));

builder.Services.AddSingleton(sp =>
{
    var s = sp.GetRequiredService<IOptions<AzureSearchSettings>>().Value;
    return new SearchIndexClient(
        new Uri(s.Endpoint),
        new AzureKeyCredential(s.ApiKey)
    );
});
builder.Services.SearchConfiguration(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

var app = builder.Build();

using var scope = app.Services.CreateScope();
await scope.ServiceProvider
           .GetRequiredService<ISearchIndexInitializer>()
           .InitializeAsync();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var common = app.MapGroup("/api/{vertical}");
common.MapCommonIndexingEndpoints();
app.MapGroup("/api/analytics")
   .MapAnalyticsEndpoints();

app.UseHttpsRedirection();
app.Run();
