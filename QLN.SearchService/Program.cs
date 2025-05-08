using Azure.Search.Documents;
using Azure;
using QLN.SearchService.IService;
using QLN.SearchService.Service;
using QLN.SearchService.IndexModels;
using QLN.SearchService.CustomEndponts;
using QLN.SearchService.IRepository;
using QLN.SearchService.Repository;
using QLN.SearchService.InitializerService;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.Configure<AzureSearchSettings>(
    builder.Configuration.GetSection("AzureSearch"));
builder.Services.AddSingleton(serviceProvider =>
{
    var config = builder.Configuration.GetSection("AzureSearch").Get<AzureSearchSettings>();
    var endpoint = new Uri(config.Endpoint);
    var credential = new AzureKeyCredential(config.ApiKey);
    return new SearchClient(endpoint, config.IndexName, credential);
});
builder.Services.AddScoped<ISearchService, SearchService>();
builder.Services.AddScoped<ISearchRepository, SearchRepository>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<SearchIndexInitializer>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
var initializer = app.Services.GetRequiredService<SearchIndexInitializer>();
await initializer.EnsureIndexExistsAsync();

var classifiedGroup = app.MapGroup("/api/classifieds")
                         .WithTags("Classifieds")
                         .WithOpenApi();

classifiedGroup.MapClassifiedSearchEndpoints();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
