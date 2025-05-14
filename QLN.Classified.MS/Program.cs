using System.Text;
using Dapr.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using QLN.Common.Infrastructure.CustomEndpoints.BannerEndPoints;
using QLN.Common.Infrastructure.IService.BannerService;
using QLN.Classified.MS.Service;
using QLN.Common.Swagger;
using QLN.Classified.MS.Service.BannerService;
using QLN.Common.Infrastructure.CustomEndpoints.ClassifiedEndpoints;

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
    opts.OperationFilter<SwaggerFileUploadFilter>();
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
  .AddJwtBearer(opt => {
      opt.RequireHttpsMetadata = true;
      opt.TokenValidationParameters = new TokenValidationParameters
      {
          ValidateIssuer = true,
          ValidateAudience = true,
          ValidateLifetime = true,
          ValidateIssuerSigningKey = true,
          ValidIssuer = builder.Configuration["Jwt:Issuer"],
          ValidAudience = builder.Configuration["Jwt:Audience"],
          IssuerSigningKey = new SymmetricSecurityKey(
           Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
      };
      opt.MapInboundClaims = false;
      opt.TokenValidationParameters.RoleClaimType = "role";
      opt.TokenValidationParameters.NameClaimType = "name";
  });
builder.Services.AddAuthorization();

builder.Services.AddDaprClient();

builder.Services.AddScoped<IBannerService, BannerService>();
builder.Services.AddScoped<IClassifiedService, ClassifiedService>();

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
app.UseAuthentication();
app.UseAuthorization();

app.MapGroup("/api/classified")
   .MapClassifiedLandingEndpoints();

app.MapGroup("/api/{vertical}")
   .MapClassifiedEndpoints();

app.MapControllers();

app.Run();
