using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.CustomEndpoints.V2ContentEndpoints;
using QLN.Common.Infrastructure.CustomEndpoints.V2ContentEventEndpoints;
using QLN.Common.Infrastructure.IService.IContentService;
using QLN.Common.Infrastructure.IService.IFileStorage;
using QLN.Common.Infrastructure.IService.ISearchService;
using QLN.Common.Infrastructure.IService.V2IContent;
using QLN.Common.Infrastructure.Service.FileStorage;
using QLN.Content.MS.Service.BannerInternalService;
using QLN.Content.MS.Service.CommunityInternalService;
using QLN.Content.MS.Service.DailyInternalService;
using QLN.Content.MS.Service.EventInternalService;
using QLN.Content.MS.Service.NewsInternalService;
using QLN.Content.MS.Service.ReportInternalService;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddDaprClient();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IFileStorageBlobService, FileStorageBlobService>();
builder.Services.AddScoped<IV2EventService, V2InternalEventService>();
builder.Services.AddScoped<IV2FOEventService, V2InternalFOEventService>();
builder.Services.AddScoped<IV2NewsService, V2InternalNewsService>();
builder.Services.AddScoped<IV2ReportsService, V2InternalReportsService>();
builder.Services.AddScoped<IV2ContentDailyService, DailyInternalService>();
builder.Services.AddScoped<V2IContentLocation, V2InternalLocationService>();
builder.Services.AddScoped<IV2ReportsService, V2InternalReportsService>();
builder.Services.AddScoped<IV2BannerService, V2BannerInternalService>();
builder.Services.AddScoped<IV2CommunityPostService, V2InternalCommunityPostService>();
builder.Services.AddSwaggerGen(opts =>
{
    opts.SwaggerDoc("v1", new OpenApiInfo { Title = "QLN.Content.MS", Version = "v1" });
    opts.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT auth"
    });
    opts.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        }]
        = new string[] { }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
var eventGroup = app.MapGroup("/api/v2/event");
eventGroup.MapEventEndpoints();
var foEventGroup = app.MapGroup("/api/v2/fo/event");
foEventGroup.MapFOEventEndpoints();
var reportgroup = app.MapGroup("/api/v2/report");
reportgroup.MapReportsEndpoints();
var newsGroup = app.MapGroup("/api/v2/news");
newsGroup.MapNewsEndpoints();
var dailyGroup = app.MapGroup("/api/v2/dailyliving");
dailyGroup.MapDailyEndpoints();
var CommunityGroup = app.MapGroup("/api/v2/location");
CommunityGroup.MapLocationsEndpoints();
var communityPostGroup = app.MapGroup("/api/v2/community");
communityPostGroup.MapCommunityPostEndpoints();

var bannerPostGroup = app.MapGroup("/api/v2/banner");
bannerPostGroup.MapBannerPostEndpoints();

app.MapPost("/api/v2/news/bulkMigrate", async Task<Results<
             Ok<string>,
             BadRequest<ProblemDetails>,
             ProblemHttpResult>>
         (
             List<V2NewsArticleDTO> articles,
             IV2NewsService service,
             CancellationToken cancellationToken
         ) =>
{
    try
    {

        var result = await service.BulkMigrateNewsArticleAsync(articles, cancellationToken);

        return TypedResults.Ok(result);
    }
    catch (InvalidDataException ex)
    {
        return TypedResults.BadRequest(new ProblemDetails
        {
            Title = "Invalid Data",
            Detail = ex.Message,
            Status = StatusCodes.Status400BadRequest
        });
    }
    catch (Exception ex)
    {
        return TypedResults.Problem("Internal Server Error", ex.Message);
    }
}).ExcludeFromDescription()
.WithName("BulkMigrate")
.WithTags("News")
.WithSummary("Bulk Migrate News Articles")
.WithDescription("Bulk Migrate News Articles")
.Produces<string>(StatusCodes.Status200OK)
.Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
.Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

app.UseHttpsRedirection();
app.Run();
