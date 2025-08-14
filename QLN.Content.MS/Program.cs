using Dapr;
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
using static QLN.Common.Infrastructure.Constants.ConstantValues;

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

app.UseCloudEvents();
app.MapSubscribeHandler();

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

app.MapPost("/api/v2/news/bulkMigrate", 
    [Topic(PubSubName, PubSubTopics.ArticlesMigration)] 
        async Task<Results<
             Ok<string>,
             BadRequest<ProblemDetails>,
             ProblemHttpResult>>
         (
             V2NewsArticleDTO article,
             IV2NewsService service,
             CancellationToken cancellationToken
         ) =>
{
    try
    {

        var result = await service.MigrateNewsArticleAsync(article, cancellationToken);

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
.WithName("BulkMigrateNews")
.WithTags("News")
.WithSummary("Bulk Migrate News Article")
.WithDescription("Bulk Migrate News Article")
.Produces<string>(StatusCodes.Status200OK)
.Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
.Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

app.MapPost("/api/v2/events/bulkMigrate",
    [Topic(PubSubName, PubSubTopics.EventsMigration)] 
            async Task<Results<
            Ok<string>,
            ForbidHttpResult,
            BadRequest<ProblemDetails>,
            ProblemHttpResult>>
            (
            V2Events dto,
            IV2EventService service,
            HttpContext httpContext,
            CancellationToken cancellationToken
            ) =>
 {
     try
     {
         var result = await service.MigrateEvent(dto, cancellationToken);
         return TypedResults.Ok(result);
     }
     catch (Exception ex)
     {
         return TypedResults.Problem("Internal Server Error", ex.Message);
     }
 }).ExcludeFromDescription()
.WithName("BulkMigrateEvents")
.WithTags("Event")
.WithSummary("Bulk Migrate Events")
.WithDescription("Bulk Migrate Events")
.Produces<string>(StatusCodes.Status200OK)
.Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
.Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

app.MapPost("/api/v2/community/bulkMigrate",
    [Topic(PubSubName, PubSubTopics.PostsMigration)] 
                async Task<Results<
                Ok<string>,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
            (
                V2CommunityPostDto post,
                IV2CommunityPostService service,
                CancellationToken ct
            ) =>
 {
     try
     {

         var result = await service.MigrateCommunityPostAsync(post, ct);
         return TypedResults.Ok(result);
     }
     catch (Exception ex)
     {
         return TypedResults.Problem("Internal Server Error", ex.Message);
     }
 }).ExcludeFromDescription()
.WithName("BulkMigrateCommunityPosts")
.WithTags("V2Community")
.WithSummary("Bulk Migrate Community Posts")
.WithDescription("Bulk Migrate Community Posts")
.Produces<string>(StatusCodes.Status200OK)
.Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
.Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

app.MapPost("/api/v2/community/comments/bulkMigrate",
    [Topic(PubSubName, PubSubTopics.CommentsMigration)]
async Task<Results<
                Ok<string>,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
            (
                CommunityCommentDto comment,
                IV2CommunityPostService service,
                CancellationToken ct
            ) =>
    {
        try
        {

            await service.AddCommentToCommunityPostAsync(comment, ct);
            return TypedResults.Ok($"Comment {comment.CommentId} Added");
        }
        catch (Exception ex)
        {
            return TypedResults.Problem("Internal Server Error", ex.Message);
        }
    }).ExcludeFromDescription()
.WithName("BulkMigrateCommunityComments")
.WithTags("V2Community")
.WithSummary("Bulk Migrate Community Comments")
.WithDescription("Bulk Migrate Community Comments")
.Produces<string>(StatusCodes.Status200OK)
.Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
.Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

app.UseHttpsRedirection();
app.Run();
