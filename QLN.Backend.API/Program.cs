using Azure.Core.Serialization;
using Dapr.Client;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using QLN.Backend.API.ServiceConfiguration;
using QLN.Common.DTO_s;
using QLN.Common.DTO_s.Payments;
using QLN.Common.Infrastructure.Auditlog;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.CustomEndpoints;
using QLN.Common.Infrastructure.CustomEndpoints.AddonEndpoint;
using QLN.Common.Infrastructure.CustomEndpoints.BannerEndpoints;
using QLN.Common.Infrastructure.CustomEndpoints.ClassifiedEndpoints;
using QLN.Common.Infrastructure.CustomEndpoints.CompanyEndpoints;
using QLN.Common.Infrastructure.CustomEndpoints.ContentEndpoints;
using QLN.Common.Infrastructure.CustomEndpoints.D365Endpoints;
using QLN.Common.Infrastructure.CustomEndpoints.FatoraEndpoints;
using QLN.Common.Infrastructure.CustomEndpoints.FileUploadService;
using QLN.Common.Infrastructure.CustomEndpoints.PayToPublishEndpoint;
using QLN.Common.Infrastructure.CustomEndpoints.ProductEndpoints;
using QLN.Common.Infrastructure.CustomEndpoints.ServiceBOEndpoint;
using QLN.Common.Infrastructure.CustomEndpoints.ServiceEndpoints;
using QLN.Common.Infrastructure.CustomEndpoints.SubscriptionEndpoints;
using QLN.Common.Infrastructure.CustomEndpoints.User;
using QLN.Common.Infrastructure.CustomEndpoints.V2ClassifiedBOEndPoints;
using QLN.Common.Infrastructure.CustomEndpoints.V2ContentEndpoints;
using QLN.Common.Infrastructure.CustomEndpoints.V2ContentEventEndpoints;
using QLN.Common.Infrastructure.CustomEndpoints.Wishlist;
using QLN.Common.Infrastructure.IService.IAuth;
using QLN.Common.Infrastructure.Model;
using QLN.Common.Infrastructure.QLDbContext;
using QLN.Common.Infrastructure.ServiceConfiguration;
using QLN.Common.Infrastructure.TokenProvider;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
var builder = WebApplication.CreateBuilder(args);

#region Kestrel For Dev Testing via dapr.yaml
// disabling as this should check if I am running local, 
// not IsDevelopment as our dev environment uses Is
//if (builder.Environment.IsDevelopment())
//{
//    builder.WebHost.ConfigureKestrel(options =>
//   {
//        options.ListenAnyIP(5200); // HTTP
//        options.ListenAnyIP(7161, listenOptions =>
//        {
//          listenOptions.UseHttps(); // HTTPS
//        });
//    });
//}
#endregion

//builder.Services.AddControllers(); // disabling as we dont use controllers
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
var connStr = builder.Configuration.GetConnectionString("DefaultConnection")!;
var dsb = new NpgsqlDataSourceBuilder(connStr);
dsb.EnableDynamicJson();      
var dataSource = dsb.Build();
builder.Services.AddDbContext<QLApplicationContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddDbContext<QLPaymentsContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddDbContext<QLClassifiedContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddDbContext<QLCompanyContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddDbContext<QLSubscriptionContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddDbContext<QLLogContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddDbContext<QLNotificationContext>(options =>
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
.AddEntityFrameworkStores<QLApplicationContext>()
.AddDefaultTokenProviders();

WebApplicationBuilder builder1 = builder;
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
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        RoleClaimType = ClaimTypes.Role,
        NameClaimType = ClaimTypes.Name
    };

    options.MapInboundClaims = false; options.Events = new JwtBearerEvents
    {
        OnTokenValidated = ctx =>
        {
            if (ctx.SecurityToken is JwtSecurityToken jwt &&
                jwt.Payload.TryGetValue("user", out var userObj) && userObj is not null)
            {
                try
                {
                    var el = userObj is JsonElement je ? je : JsonDocument.Parse(userObj.ToString()!).RootElement;
                    var id = new ClaimsIdentity("QLClaims");

                    string? GetString(string name)
                        => el.TryGetProperty(name, out var p) && p.ValueKind is JsonValueKind.String
                           ? p.GetString() : null;

                    void AddIf(string type, string? val)
                    {
                        if (!string.IsNullOrWhiteSpace(val)) id.AddClaim(new Claim(type, val));
                    }

                    var uid = GetString("uid");
                    AddIf("ql:uid", uid);
                    if (!string.IsNullOrWhiteSpace(uid))
                        id.AddClaim(new Claim(ClaimTypes.NameIdentifier, uid));

                    AddIf(ClaimTypes.Name, GetString("name") ?? GetString("alias"));
                    AddIf(ClaimTypes.Email, GetString("email"));
                    AddIf("ql:phone", GetString("phone"));
                    AddIf("ql:alias", GetString("alias"));
                    AddIf("ql:qlnext_user_id", GetString("qlnext_user_id"));

                    if (el.TryGetProperty("roles", out var roles) && roles.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var r in roles.EnumerateArray())
                        {
                            var rv = r.GetString();
                            if (!string.IsNullOrWhiteSpace(rv))
                                id.AddClaim(new Claim(ClaimTypes.Role, rv));
                        }
                    }

                    ctx.Principal!.AddIdentity(id);
                }
                catch {  }
            }
            return Task.CompletedTask;
        }
    };

});

#endregion

//builder.Services.AddAuthorization();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireActiveBusinessAccount", policy =>
        policy.RequireAssertion(context =>
        {
            var userClaim = context.User.Claims.FirstOrDefault(c => c.Type == "user");
            if (userClaim == null) return false;

            try
            {
                var userJson = JsonDocument.Parse(userClaim.Value);
                var root = userJson.RootElement;

                // Check if roles array contains "business_account"
                if (!root.TryGetProperty("roles", out var rolesProp) ||
                    !rolesProp.EnumerateArray().Any(role => role.GetString() == "business_account"))
                {
                    return false;
                }

                // Once Subscription info is in we can verify completely against it
                //// Check if subscription exists and is not expired
                //if (!root.TryGetProperty("subscription", out var subscriptionProp))
                //    return false;

                //if (!subscriptionProp.TryGetProperty("expire_date", out var expireDateProp))
                //    return false;

                //if (!long.TryParse(expireDateProp.GetString(), out var expireUnix))
                //    return false;

                //var nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                //if (expireUnix <= nowUnix)
                //    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }));

    options.AddPolicy("RequireNoBusinessAccount", policy =>
        policy.RequireAssertion(context =>
        {
            var userClaim = context.User.Claims.FirstOrDefault(c => c.Type == "user");
            if (userClaim == null) return false;

            try
            {
                var userJson = JsonDocument.Parse(userClaim.Value);
                var root = userJson.RootElement;

                // Fail if roles array contains "business_account"
                if (root.TryGetProperty("roles", out var rolesProp) &&
                    rolesProp.EnumerateArray().Any(role => role.GetString() == "business_account"))
                {
                    return false;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }));
});



builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);

#region Dapr client & actors 
builder.Services.AddSingleton<DaprClient>(_ =>
{
    return new DaprClientBuilder()
        .Build();
});

builder.Services.AddDaprClient();
#endregion

builder.Services.AddActors(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.ConfigureHttpJsonOptions(opts =>
{
    opts.SerializerOptions.Converters
        .Add(new MicrosoftSpatialGeoJsonConverter());
    opts.SerializerOptions.Converters.Add(new AttributesJsonConverter());
});
builder.Services.AddResponseCaching();
builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
        options.MimeTypes = new[] { "text/css", "application/javascript", "text/html", "application/json" };
    });


builder.Services.FileServiceConfiguration(builder.Configuration);
builder.Services.ServicesConfiguration(builder.Configuration);
builder.Services.ServiceConfiguration(builder.Configuration);
builder.Services.ClassifiedServicesConfiguration(builder.Configuration);
builder.Services.ClassifiedLandingBo(builder.Configuration);
builder.Services.SearchServicesConfiguration(builder.Configuration);
builder.Services.ContentServicesConfiguration(builder.Configuration);
builder.Services.AnalyticsServicesConfiguration(builder.Configuration);
builder.Services.BannerServicesConfiguration(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.CompanyConfiguration(builder.Configuration);
builder.Services.EventConfiguration(builder.Configuration);
builder.Services.EventFOConfiguration(builder.Configuration);
builder.Services.NewsConfiguration(builder.Configuration);
builder.Services.ReportsConfiguration(builder.Configuration);
builder.Services.DailyBoConfiguration(builder.Configuration);
builder.Services.CommunityConfiguration(builder.Configuration);
builder.Services.CommunityPostConfiguration(builder.Configuration);
builder.Services.AddonConfiguration(builder.Configuration);
builder.Services.SubscriptionConfiguration(builder.Configuration);
builder.Services.PayToPublishConfiguration(builder.Configuration);
builder.Services.PayToFeatureConfiguration(builder.Configuration);
builder.Services.AddonConfiguration(builder.Configuration);
builder.Services.V2BannerConfiguration(builder.Configuration);
builder.Services.DrupalAuthConfiguration(builder.Configuration);
builder.Services.DrupalUserServicesConfiguration(builder.Configuration);
builder.Services.AddScoped<AuditLogger>();
builder.Services.PaymentsConfiguration(builder.Configuration);
builder.Services.ProductsConfiguration(builder.Configuration);
builder.Services.ClassifiedBoStoresConfiguration(builder.Configuration);
builder.Services.ServicesBo(builder.Configuration);

// Putting this here as it could be used from the Backend API - but also in Classifieds Service
builder.Services.ImplioConfiguration(builder.Configuration);

builder.Services.AddSingleton<D365Config>(provider =>
{
    var config = new D365Config();
    builder.Configuration.GetSection("D365").Bind(config);
    return config;
});

var app = builder.Build();
#region DAPR Subscriptions

app.UseCloudEvents();

app.MapSubscribeHandler();

#endregion

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
app.UseAuthentication();
app.UseHttpsRedirection();
app.UseAuthorization();


var authGroup = app.MapGroup("/auth");
authGroup.MapAuthEndpoints();
var filesGroup = app.MapGroup("/files");
filesGroup.MapFileUploadEndpoint();
var paymentGroup = app.MapGroup("/api/pay");
paymentGroup.MapD365Endpoints();
var wishlistgroup = app.MapGroup("/api/wishlist");
wishlistgroup.MapWishlist();
var companyProfileGroup = app.MapGroup("/api/companyprofile");
companyProfileGroup.MapCompanyProfile()
    .RequireAuthorization();
var classifiedGroup = app.MapGroup("/api/classified");
classifiedGroup.MapClassifiedsEndpoints();
var eventGroup = app.MapGroup("/api/v2/event");
eventGroup.MapEventEndpoints()
    .RequireAuthorization();
var foEventGroup = app.MapGroup("/api/v2/fo/event");
foEventGroup.MapFOEventEndpoints();
var ServiceGroup = app.MapGroup("/api/service");
ServiceGroup.MapAllServiceConfiguration()
    .RequireAuthorization();
var reportsGroup = app.MapGroup("/api/v2/report");
reportsGroup.MapReportsEndpoints();
var contentGroup = app.MapGroup("/api/content");
contentGroup.MapContentLandingEndpoints();
var analyticGroup = app.MapGroup("/api/analytics");
analyticGroup.MapAnalyticsEndpoints();

app.MapGroup("/api/v2/subscriptions")
    .MapV2SubscriptionEndpoints()
    .MapV2AdminEndpoints()
    .RequireAuthorization();

var fatoraGroup = app.MapGroup("/api/pay");
fatoraGroup.MapFaturaEndpoints();

var newsGroup = app.MapGroup("/api/v2/news");
newsGroup.MapNewsEndpoints();

var dailyGroup = app.MapGroup("/api/v2/dailyliving");
dailyGroup.MapDailyEndpoints();

var locationGroup = app.MapGroup("/api/v2/location");
locationGroup.MapLocationsEndpoints();
var communityPostGroup = app.MapGroup("/api/v2/community");
communityPostGroup.MapCommunityPostEndpoints();
 var bannerGroup = app.MapGroup("/api/banner");
bannerGroup.MapBannerEndpoints();
var bannerPostGroup  = app.MapGroup("/api/v2/banner");
bannerPostGroup.MapBannerPostEndpoints();
var ClassifiedBo = app.MapGroup("/api/v2/classifiedbo");
ClassifiedBo.MapClassifiedboEndpoints()
    .RequireAuthorization();

var ServicesBo = app.MapGroup("/api/servicebo");
ServicesBo.MapAllServiceBoConfiguration();

var Product = app.MapGroup("/api/products");
Product.MapProductEndpoints()
    .RequireAuthorization();

app.MapGet("/testauth", (HttpContext context) =>
{
    var user = context.User;
    if (user == null || !user.Identity.IsAuthenticated)
    {
        return Results.Unauthorized();
    }
    var claims = user.Claims.Select(c => new { c.Type, c.Value }).ToList();
    return Results.Ok(new { Message = "Authenticated", Claims = claims });
})
    .WithName("TestAuth")
    .WithTags("AAAAuthentication")
    .WithDescription("Test authentication endpoint to verify JWT token claims.")
    .RequireAuthorization("RequireActiveBusinessAccount");

app.MapPost("/testauth", (HttpContext context) =>
{
    var user = context.User;
    if (user == null || !user.Identity.IsAuthenticated)
    {
        return Results.Unauthorized();
    }
    var claims = user.Claims.Select(c => new { c.Type, c.Value }).ToList();
    return Results.Ok(new { Message = "Authenticated", Claims = claims });
})
    .WithName("TestPostAuth")
    .WithTags("AAAAuthentication")
    .WithDescription("Test authentication endpoint to verify JWT token claims.")
    .RequireAuthorization("RequireNoBusinessAccount");

app.MapPost("/drupallogin", async (
    HttpContext context,
    IDrupalAuthService drupalAuthService,
    LoginRequest loginRequest,
    CancellationToken cancellationToken
    ) =>
{
    var drupalLogin = await drupalAuthService.LoginAsync(loginRequest.UsernameOrEmailOrPhone, loginRequest.Password, cancellationToken);

    return Results.Ok(drupalLogin);
})
    .WithName("TestDrupalLogin")
    .WithTags("AAAAuthentication")
    .WithDescription("Test login to Drupal");

app.Run();