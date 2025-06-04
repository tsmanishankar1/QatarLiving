using Dapr.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using QLN.Backend.API.ServiceConfiguration;
using QLN.Common.Infrastructure.CustomEndpoints.BannerEndpoints;
using QLN.Common.Infrastructure.CustomEndpoints.ClassifiedEndpoints;
using QLN.Common.Infrastructure.CustomEndpoints.CompanyEndpoints;
using QLN.Common.Infrastructure.CustomEndpoints.ContentEndpoints;
using QLN.Common.Infrastructure.CustomEndpoints.PayToPublishEndpoint;
using QLN.Common.Infrastructure.CustomEndpoints.SubscriptionEndpoints;
using QLN.Common.Infrastructure.CustomEndpoints.User;
using QLN.Common.Infrastructure.DbContext;
using QLN.Common.Infrastructure.Model;
using QLN.Common.Infrastructure.ServiceConfiguration;
using QLN.Common.Infrastructure.TokenProvider;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using QLN.Common.Infrastructure.CustomEndpoints;
using QLN.Common.Infrastructure.CustomEndpoints.LandingEndpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

#region Configure HttpClient with increased timeout for Dapr
builder.Services.AddHttpClient("DaprClient")
    .ConfigureHttpClient(client =>
    {
        client.Timeout = TimeSpan.FromMinutes(5); 
    });

#endregion

#region Swagger configuration
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1.1.1", new OpenApiInfo
    {
        Title = "Qatar Management API",
        Version = "v1.1.1",
        Description = "API documentation for Qatar Management system."
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

builder.Services.Configure<DataProtectionTokenProviderOptions>(opt =>
{
    opt.TokenLifespan = TimeSpan.FromDays(1);
});

#region Identity password options
builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
    options.Password.RequiredUniqueChars = 1;
});
#endregion

#region Database context
builder.Services.AddDbContext<QatarlivingDevContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
#endregion

#region Identity configuration
builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
{
    options.SignIn.RequireConfirmedEmail = true;

    options.Tokens.ProviderMap["EmailVerification"] = new TokenProviderDescriptor(typeof(QLN.Common.Infrastructure.TokenProvider.EmailTokenProvider<ApplicationUser>));
    options.Tokens.ProviderMap["PhoneVerification"] = new TokenProviderDescriptor(typeof(CommonTokenProvider<ApplicationUser>));

    options.Tokens.EmailConfirmationTokenProvider = "EmailVerification";
    options.Tokens.ChangePhoneNumberTokenProvider = "PhoneVerification";
})
.AddEntityFrameworkStores<QatarlivingDevContext>()
.AddDefaultTokenProviders();
#endregion

#region Authentication - New JWT Bearer configuration
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
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
        RoleClaimType = ClaimTypes.Role, 
    };

    options.MapInboundClaims = false;
});
#endregion

builder.Services.AddAuthorization();

builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);

#region Dapr client & actors 
builder.Services.AddSingleton<DaprClient>(_ =>
{
    return new DaprClientBuilder()
        .Build();
});
#endregion
builder.Services.AddActors(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddResponseCaching();

builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
        options.MimeTypes = new[] { "text/css", "application/javascript", "text/html", "application/json" };
    });

builder.Services.AddDaprClient();

builder.Services.ServicesConfiguration(builder.Configuration);
builder.Services.ClassifiedServicesConfiguration(builder.Configuration);
builder.Services.SearchServicesConfiguration(builder.Configuration);
builder.Services.ContentServicesConfiguration(builder.Configuration);
builder.Services.BannerServicesConfiguration(builder.Configuration);
builder.Services.AnalyticsServicesConfiguration(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.CompanyConfiguration(builder.Configuration);
builder.Services.SubscriptionConfiguration(builder.Configuration);
builder.Services.PayToPublishConfiguration(builder.Configuration);
var app = builder.Build();

app.UseResponseCaching();

if (!app.Environment.IsDevelopment())
{
    app.UseResponseCompression();
}

if (builder.Configuration.GetValue<bool>("EnableSwagger"))
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1.1.1/swagger.json", "v1.1.1");
        options.RoutePrefix = "Swagger";
        options.DocumentTitle = "Qatar Management API";
    });
}


var authGroup = app.MapGroup("/auth");
authGroup.MapAuthEndpoints();

var companyGroup = app.MapGroup("/api/companyprofile");
companyGroup.MapCompanyEndpoints()
    .RequireAuthorization();
var classifiedGroup = app.MapGroup("/api/classified");
classifiedGroup.MapClassifiedsEndpoints();

var servicesGroup = app.MapGroup("/api/services");
servicesGroup.MapServicesEndpoints();

var contentGroup = app.MapGroup("/api/content");
contentGroup.MapContentLandingEndpoints();

var bannerGroup = app.MapGroup("/api/banner");
bannerGroup.MapBannerEndpoints();

var analyticGroup = app.MapGroup("/api/analytics");
analyticGroup.MapAnalyticsEndpoints();

app.MapGroup("/api/subscriptions")
   .MapSubscriptionEndpoints();

   app.MapGroup("/api/payments")
    .MapPaymentEndpoints()
    .RequireAuthorization();

app.MapGroup("/api/PayToPublish")
    .MapPayToPublishEndpoints();


app.MapAllBackOfficeEndpoints();
app.MapLandingPageEndpoints();

app.UseHttpsRedirection();
app.UseAuthorization();
app.Run();
