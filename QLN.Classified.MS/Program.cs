using Dapr;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Npgsql;
using QLN.Classifieds.MS.ServiceConfiguration;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Auditlog;
using QLN.Common.Infrastructure.CustomEndpoints.ClassifiedEndpoints;
using QLN.Common.Infrastructure.CustomEndpoints.ServiceBOEndpoint;
using QLN.Common.Infrastructure.CustomEndpoints.ServiceEndpoints;
using QLN.Common.Infrastructure.CustomEndpoints.V2ClassifiedBOEndPoints;
using QLN.Common.Infrastructure.IService;
using QLN.Common.Infrastructure.IService.V2IContent;
using QLN.Common.Infrastructure.Model;
using QLN.Common.Infrastructure.QLDbContext;
using static QLN.Common.Infrastructure.Constants.ConstantValues;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opts => {
    opts.SwaggerDoc("v1", new OpenApiInfo { Title = "QLN.Classified.MS", Version = "v1" });
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

builder.Services.AddAuthorization();
builder.Services.AddDaprClient();
builder.Services.ClassifiedInternalServicesConfiguration(builder.Configuration);
builder.Services.AddScoped<AuditLogger>();

#region DbContext
var dataSourceBuilder = new NpgsqlDataSourceBuilder(builder.Configuration.GetConnectionString("DefaultConnection"));
dataSourceBuilder.EnableDynamicJson();
var dataSource = dataSourceBuilder.Build();
builder.Services.AddDbContext<QLClassifiedContext>(options =>
    options.UseNpgsql(dataSource));
builder.Services.AddDbContext<QLSubscriptionContext>(options =>
    options.UseNpgsql(dataSource));
builder.Services.AddDbContext<QLApplicationContext>(options =>
    options.UseNpgsql(dataSource));
builder.Services.AddDbContext<QLPaymentsContext>(options =>
    options.UseNpgsql(dataSource));
builder.Services.AddDbContext<QLCompanyContext>(options =>
    options.UseNpgsql(dataSource));
builder.Services.AddDbContext<QLPaymentsContext>(options =>
    options.UseNpgsql(dataSource));
builder.Services.AddDbContext<QLSubscriptionContext>(options =>
    options.UseNpgsql(dataSource));
builder.Services.AddDbContext<QLLogContext>(options =>
    options.UseNpgsql(dataSource));
#endregion
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseCloudEvents();
app.MapSubscribeHandler();

app.UseHttpsRedirection();

app.MapGroup("/api/classifieds")
   .MapClassifiedEndpoints();
app.MapGroup("/api/classifieds")
   .MapClassifiedFOStoresEndpoints();
var ServiceGroup = app.MapGroup("/api/service");
ServiceGroup.MapAllServiceConfiguration();
var ClassifiedBo = app.MapGroup("/api/v2/classifiedbo");
ClassifiedBo.MapClassifiedboEndpoints();
var ServicesBo = app.MapGroup("/api/servicebo");
ServicesBo.MapAllServiceBoConfiguration();

app.MapPost("/api/classifieds/items/bulkMigrate",
    [Topic(PubSubName, PubSubTopics.ItemsMigration)]
async Task<Results<
                Ok<string>,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
            (
                Items item,
                IClassifiedService service,
                CancellationToken ct
            ) =>
    {
        try
        {

            var result = await service.MigrateClassifiedItemsAd(item, ct);
            return TypedResults.Ok(result);
        }
        catch (Exception ex)
        {
            return TypedResults.Problem("Internal Server Error", ex.Message);
        }
    }).ExcludeFromDescription()
.WithName("BulkMigrateClassifiedsItems")
.WithTags("Classified")
.WithSummary("Bulk Migrate Classifieds Ads")
.WithDescription("Bulk Migrate Classifieds Ads")
.Produces<string>(StatusCodes.Status200OK)
.Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
.Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

app.MapPost("/api/classifieds/collectables/bulkMigrate",
    [Topic(PubSubName, PubSubTopics.CollectablesMigration)]
async Task<Results<
                Ok<string>,
                BadRequest<ProblemDetails>,
                ProblemHttpResult>>
            (
                Collectibles collectable,
                IClassifiedService service,
                CancellationToken ct
            ) =>
    {
        try
        {

            var result = await service.MigrateClassifiedCollectiblesAd(collectable, ct);
            return TypedResults.Ok(result);
        }
        catch (Exception ex)
        {
            return TypedResults.Problem("Internal Server Error", ex.Message);
        }
    }).ExcludeFromDescription()
.WithName("BulkMigrateCollectables")
.WithTags("Classified")
.WithSummary("Bulk Migrate Collectables")
.WithDescription("Bulk Migrate Collectables")
.Produces<string>(StatusCodes.Status200OK)
.Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
.Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

app.Run();
