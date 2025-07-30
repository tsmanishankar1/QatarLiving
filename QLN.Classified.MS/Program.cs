using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using QLN.Classified.MS.DBContext;
using QLN.Classifieds.MS.ServiceConfiguration;
using QLN.Common.Infrastructure.CustomEndpoints;
using QLN.Common.Infrastructure.CustomEndpoints.ClassifiedEndpoints;
using QLN.Common.Infrastructure.CustomEndpoints.ServiceBOEndpoint;
using QLN.Common.Infrastructure.CustomEndpoints.ServiceEndpoints;
using QLN.Common.Infrastructure.CustomEndpoints.V2ClassifiedBOEndPoints;
using QLN.Common.Infrastructure.IService.V2IClassifiedBoService;

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
#region Database context
builder.Services.AddDbContext<ClassifiedDevContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
#endregion
builder.Services.AddAuthorization();
builder.Services.AddDaprClient();
builder.Services.ClassifiedInternalServicesConfiguration(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

app.MapGroup("/api/classifieds")
   .MapClassifiedEndpoints();
var ServiceGroup = app.MapGroup("/api/service");
ServiceGroup.MapAllServiceConfiguration();
var servicesGroup = app.MapGroup("/api/services");
servicesGroup.MapServicesEndpoints();



var ClassifiedBo = app.MapGroup("/api/v2/classifiedbo");
ClassifiedBo.MapClassifiedboEndpoints();

var ServicesBo = app.MapGroup("/api/servicebo");
ServicesBo.MapAllServiceBoConfiguration();



app.Run();
