using Microsoft.OpenApi.Models;
using QLN.Common.Infrastructure.CustomEndpoints.V2ContentEndpoints;
using QLN.Common.Infrastructure.IService.IFileStorage;
using QLN.Common.Infrastructure.IService.V2IContent;
using QLN.Common.Infrastructure.Service.FileStorage;
using QLN.Content.MS.Service.NewsInternalService;
using QLN.Content.MS.Service;
using QLN.Common.Infrastructure.CustomEndpoints.V2ContentEventEndpoints;
using QLN.Common.Infrastructure.IService.IContentService;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddDaprClient();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IV2EventService, V2InternalEventService>();
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
builder.Services.AddScoped<IV2ContentNews, NewsInternalService>();
builder.Services.AddScoped<IV2EventService, V2InternalEventService>();
builder.Services.AddScoped<IV2contentBannerService, V2InternalBannerService>();
builder.Services.AddScoped<IFileStorageBlobService, FileStorageBlobService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
var eventGroup = app.MapGroup("v2/api/event");
eventGroup.MapEventEndpoints();
app.MapGroup("/api/v2").MapContentBannerEndpoints()
    .MapContentNewsEndpoints();
app.MapControllers();
app.UseHttpsRedirection();
app.Run();
