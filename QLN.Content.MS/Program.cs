using Dapr.Client;
using Microsoft.OpenApi.Models;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using QLN.Common.Infrastructure.CustomEndpoints.V2ContentEndpoints;
using QLN.Common.Infrastructure.IService.IFileStorage;
using QLN.Common.Infrastructure.IService.V2IContent;
using QLN.Common.Infrastructure.Service.FileStorage;
using QLN.Content.MS.Service.NewsInternalService;

var builder = WebApplication.CreateBuilder(args);

//// JWT authentication configuration
//builder.Services.AddAuthentication("Bearer")
//    .AddJwtBearer("Bearer", options =>
//    {
//        options.RequireHttpsMetadata = true;
//        options.TokenValidationParameters = new TokenValidationParameters
//        {
//            ValidateIssuer = true,
//            ValidateAudience = true,
//            ValidateLifetime = true,
//            ValidateIssuerSigningKey = true,
//            ValidIssuer = builder.Configuration["Jwt:Issuer"],
//            ValidAudience = builder.Configuration["Jwt:Audience"],
//            IssuerSigningKey = new SymmetricSecurityKey(
//                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)
//            ),
//            RoleClaimType = ClaimTypes.Role,
//        };
//    });

//builder.Services.AddAuthorization();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
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
builder.Services.AddScoped<IV2contentBannerService, BannerService>();
builder.Services.AddScoped<IFileStorageBlobService, FileStorageBlobService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

//app.UseAuthentication(); 
//app.UseAuthorization();

app.MapControllers();
app.MapGroup("/api/v2").MapContentBannerEndpoints()
    .MapContentNewsEndpoints();

app.Run();
