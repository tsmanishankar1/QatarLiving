using Microsoft.OpenApi.Models;
using QLN.Common.Infrastructure.CustomEndpoints.V2ContentEndpoints;
using QLN.Common.Infrastructure.IService.IFileStorage;
using QLN.Common.Infrastructure.IService.V2IContent;
using QLN.Common.Infrastructure.Service.FileStorage;
//using QLN.Content.MS.Service.NewsInternalService;
using QLN.Common.Infrastructure.CustomEndpoints.V2ContentEventEndpoints;
using QLN.Common.Infrastructure.IService.IContentService;
using QLN.Content.MS.Service.EventInternalService;
using QLN.Content.MS.Service.NewsInternalService;
using QLN.Content.MS.Service.CommunityInternalService;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddDaprClient();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IV2EventService, V2InternalEventService>();
builder.Services.AddScoped<IV2NewsService, V2InternalNewsService>();
builder.Services.AddScoped<IV2CommunityPostService,V2InternalCommunityPostService>();
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

builder.Services.AddDaprClient();
builder.Services.AddScoped<IV2NewsService, V2InternalNewsService>();
builder.Services.AddScoped<IV2EventService, V2InternalEventService>();
builder.Services.AddScoped<IFileStorageBlobService, FileStorageBlobService>();
builder.Services.AddScoped<V2IContentLocation, V2InternalLocationService>();
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
var eventGroup = app.MapGroup("/api/v2/event");
eventGroup.MapEventEndpoints();

var newsGroup = app.MapGroup("/api/v2/news");
newsGroup.MapNewsEndpoints();

var CommunityGroup = app.MapGroup("/api/v2/location");
CommunityGroup.MapLocationsEndpoints();

var communityPostGroup = app.MapGroup("/api/v2/community");
communityPostGroup.MapCommunityEndpoints();
// app.MapControllers(); / disabling to trigger a build, but we dont use controllers anyhow
app.UseHttpsRedirection();
app.Run();
