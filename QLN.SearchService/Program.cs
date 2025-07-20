using Azure;
using Azure.Search.Documents.Indexes;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;
using QLN.Common.Infrastructure.CustomEndpoints;
using QLN.SearchService;
using QLN.SearchService.CustomEndpoints;
using QLN.SearchService.IndexModels;
using QLN.SearchService.ServiceConfiguration;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDaprClient();
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
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

builder.Services.SearchConfiguration(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
app.UseCloudEvents();
app.MapSubscribeHandler();
var common = app.MapGroup("/api/indexes");
common.MapCommonIndexingEndpoints();
app.MapGroup("/api/analytics")
   .MapAnalyticsEndpoints();
app.MapIndexSubscriberEndpoints();
app.UseHttpsRedirection();
app.Run();
