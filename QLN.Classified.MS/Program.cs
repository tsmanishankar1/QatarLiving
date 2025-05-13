using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using QLN.Common.Infrastructure.CustomEndpoints.BannerEndPoints;
using QLN.Common.Infrastructure.ServiceConfiguration;
using QLN.Common.Swagger;
using System.Text;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

#region swagger configuration
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1.1.1", new OpenApiInfo
    {
        Title = "QLN.Classified.MS API",
        Version = "v1.1.1",
        Description = "API documentation for QatarQLN.Classified.MS API."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' followed by your JWT token."
    });

    options.OperationFilter<SwaggerFileUploadFilter>();

    options.MapType<IFormFile>(() => new OpenApiSchema
    {
        Type = "string",
        Format = "binary"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});
#endregion


#region authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = true;

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };

    options.MapInboundClaims = false;

    options.TokenValidationParameters.RoleClaimType = "role";
    options.TokenValidationParameters.NameClaimType = "name";

});

#endregion

builder.Services.AddAuthorization();

builder.Services.AddDaprClient(clientBuilder =>
{
    clientBuilder
      .UseHttpEndpoint("http://localhost:63231")
      .UseGrpcEndpoint("http://localhost:50003");
});

builder.Services.ClassifiedServicesConfiguration(builder.Configuration);


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1.1.1/swagger.json", "v1.1.1");
        options.RoutePrefix = "Swagger";
        options.DocumentTitle = "QLN.Classified.MS API";
    });
}


var bannerGroup = app.MapGroup("/api/banner");
bannerGroup.MapBannerEndpoints();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
