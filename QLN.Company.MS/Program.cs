using Microsoft.OpenApi.Models;
using QLN.Common.Infrastructure.CustomEndpoints.CompanyEndpoints;
using QLN.Common.Infrastructure.IService.ICompanyService;
using QLN.Common.Swagger;
using QLN.Company.MS.Service;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddDaprClient();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opts => {
    opts.SwaggerDoc("v1", new OpenApiInfo { Title = "QLN.Company.MS", Version = "v1" });
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
    opts.OperationFilter<SwaggerFileUploadFilter>();
});

builder.Services.AddScoped<ICompanyService, InternalCompanyService>();
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
var companyGroup = app.MapGroup("/companyprofile");
companyGroup.MapCompanyEndpoints();
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
