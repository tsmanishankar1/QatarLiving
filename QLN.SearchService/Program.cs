using Azure;
using Azure.Search.Documents.Indexes;
using Microsoft.Extensions.Options;
using QLN.SearchService;
using QLN.SearchService.CustomEndpoints;
using QLN.SearchService.IndexModels;
using QLN.SearchService.IRepository;
using QLN.SearchService.IService;
using QLN.SearchService.Repository;
using QLN.SearchService.Service;

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

builder.Services.AddScoped<ISearchRepository, SearchRepository>();
builder.Services.AddScoped<ISearchService, SearchService>();
builder.Services.AddSingleton<ISearchIndexInitializer, SearchIndexInitializer>();

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

app.MapCommonIndexingEndpoints();

app.UseHttpsRedirection();
app.Run();
