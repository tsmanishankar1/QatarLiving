using Azure.Core.Serialization;
using Dapr.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using QLN.Backend.API.ServiceConfiguration;
using QLN.Common.Infrastructure.CustomEndpoints;
using QLN.Common.Infrastructure.CustomEndpoints.BannerEndpoints;
using QLN.Common.Infrastructure.CustomEndpoints.ClassifiedEndpoints;
using QLN.Common.Infrastructure.CustomEndpoints.CompanyEndpoints;
using QLN.Common.Infrastructure.CustomEndpoints.ContentEndpoints;
using QLN.Common.Infrastructure.CustomEndpoints.LandingEndpoints;
using QLN.Common.Infrastructure.CustomEndpoints.PayToPublishEndpoint;
using QLN.Common.Infrastructure.CustomEndpoints.SubscriptionEndpoints;
using QLN.Common.Infrastructure.CustomEndpoints.User;
using QLN.Common.Infrastructure.DbContext;
using QLN.Common.Infrastructure.Model;
using QLN.Common.Infrastructure.ServiceConfiguration;
using QLN.Common.Infrastructure.TokenProvider;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using QLN.Common.Infrastructure.CustomEndpoints;
using QLN.Common.Infrastructure.CustomEndpoints.LandingEndpoints;
using QLN.Common.Infrastructure.CustomEndpoints.V2ContentEventEndpoints;

using Azure.Core.Serialization;
using QLN.Common.Infrastructure.CustomEndpoints.V2ContentEndpoints;

using QLN.Common.Infrastructure.CustomEndpoints.Wishlist;
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

    //options.Events = new JwtBearerEvents
    //{
    //    OnMessageReceived = context =>
    //    {
    //        // Expect JWT in Authorization header as Bearer token
    //        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
    //        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
    //        {
    //            context.Token = authHeader.Substring("Bearer ".Length).Trim();
    //        }
    //        return Task.CompletedTask;
    //    },
    //    OnTokenValidated = async context =>
    //    {
            
    //        var jwt = context.SecurityToken as JwtSecurityToken;
    //        var tokenString = context.Request.Headers["Authorization"].FirstOrDefault()?.Substring("Bearer ".Length).Trim();

    //        if (!string.IsNullOrEmpty(tokenString))
    //        {
    //            var principal = ValidateTokenFromDrupal(tokenString);
    //            context.Principal = principal;
    //        }
    //        await Task.CompletedTask;
    //    }
    //};


    // Add this section before or inside AddJwtBearer to ignore signature validation
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = false,
        RequireSignedTokens = false,
        ValidateIssuerSigningKey = false, // Do not validate the signature
        // Bypass signature validation for demo purposes
        // Instead of creating a new JwtSecurityToken (which does not validate the signature),
        // just parse the token string and return the JwtSecurityToken instance.
        // This will allow the token to pass signature validation if all other checks are disabled.
        SignatureValidator = (token, paramater) =>
        {
            return new JwtSecurityToken(token);
        },
        //SignatureValidator = (token, parameters) =>
        //{
        //    var principal = ValidateTokenFromDrupal(token);
        //    return principal?.Identity is ClaimsIdentity identity && identity.IsAuthenticated
        //        ? CreateJwtSecurityTokenFromPrincipal(principal)
        //        : CreateJwtSecurityTokenFromPrincipal(principal); // Return null if validation fails
        //},
        //ValidIssuer = builder.Configuration["Jwt:Issuer"],
        //ValidAudience = builder.Configuration["Jwt:Audience"],
        RoleClaimType = ClaimTypes.Role,
        NameClaimType = ClaimTypes.Name
    };

    options.MapInboundClaims = false;
});

//// Helper method to create a JwtSecurityToken from a ClaimsPrincipal
//    static SecurityToken CreateJwtSecurityTokenFromPrincipal(ClaimsPrincipal principal)
//    {
//        var identity = principal.Identity as ClaimsIdentity;
//        var claims = identity?.Claims ?? Enumerable.Empty<Claim>();

//        // You may want to set issuer, audience, and expiration as needed
//        var token = new JwtSecurityToken(
//            issuer: null,
//            audience: null,
//            claims: claims,
//            notBefore: DateTime.UtcNow,
//            expires: DateTime.UtcNow.AddDays(7),

//            signingCredentials: new SigningCredentials(
//                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes("this is a long string to test this")),
//                    SecurityAlgorithms.HmacSha256
//                )
//        );

//        return token;
//    }

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
builder.Services.ConfigureHttpJsonOptions(opts =>
{
    opts.SerializerOptions.Converters
        .Add(new MicrosoftSpatialGeoJsonConverter());
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
builder.Services.AnalyticsServicesConfiguration(builder.Configuration);
builder.Services.BannerServicesConfiguration(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.CompanyConfiguration(builder.Configuration);
builder.Services.EventConfiguration(builder.Configuration);
builder.Services.SubscriptionConfiguration(builder.Configuration);
builder.Services.PayToPublishConfiguration(builder.Configuration);
builder.Services.ContentConfiguration(builder.Configuration);
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
app.UseHttpsRedirection();
app.UseAuthorization();

var authGroup = app.MapGroup("/auth");
authGroup.MapAuthEndpoints();
var wishlistgroup = app.MapGroup("/api/wishlist");
wishlistgroup.MapWishlist();
var companyGroup = app.MapGroup("/api/companyprofile");
companyGroup.MapCompanyEndpoints()
    .RequireAuthorization();
var classifiedGroup = app.MapGroup("/api/classified");
classifiedGroup.MapClassifiedsEndpoints();
var servicesGroup = app.MapGroup("/api/services");
servicesGroup.MapServicesEndpoints();
var eventGroup = app.MapGroup("v2/api/event");
eventGroup.MapEventEndpoints()
    .RequireAuthorization();
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

app.MapGroup("/api/v2/content")
    .MapNewsContentEndpoints();



app.MapAllBackOfficeEndpoints();
app.MapLandingPageEndpoints();

app.MapGet("/testauth", () =>
{
    // return a list of clamis from the jwt token
    var user = ClaimsPrincipal.Current;
    if (user == null || !user.Identity.IsAuthenticated)
    {
        return Results.Unauthorized();
    }
    var claims = user.Claims.Select(c => new { c.Type, c.Value }).ToList();
    return Results.Ok(new { Message = "Authenticated", Claims = claims });
    })
    .WithName("TestAuth")
    .WithTags("Authentication")
    .WithDescription("Test authentication endpoint to verify JWT token claims.")
    .RequireAuthorization();



app.Run();
//static ClaimsPrincipal ValidateTokenFromDrupal(string tokenString)
//{
//    var tokenHandler = new JwtSecurityTokenHandler();

//    var validationParameters = new TokenValidationParameters
//    {
//        ValidateIssuer = false,
//        ValidateAudience = false,
//        ValidateLifetime = false,
//        ValidateIssuerSigningKey = false,
//        SignatureValidator = (token, parameters) =>
//        {
//            // Bypass signature validation for demo purposes
//            return new JwtSecurityToken(token);
//        },
//        //ValidIssuer = builder.Configuration["Jwt:Issuer"],
//        //ValidAudience = builder.Configuration["Jwt:Audience"],
//        RoleClaimType = ClaimTypes.Role,
//        NameClaimType = ClaimTypes.Name
//    };

//    try
//    {
//        SecurityToken validatedToken;
//        var validatedPrincipal = tokenHandler.ValidateToken(tokenString, validationParameters, out validatedToken);

//        if (validatedToken.ValidTo > DateTime.UtcNow)
//        {
//            var decodedToken = tokenHandler.ReadJwtToken(tokenString);
//            var identity = (ClaimsIdentity)validatedPrincipal.Identity!;

//            // Example: extract custom claims from JWT payload
//            // (Replace with your own logic as needed)
//            if (decodedToken.Payload.TryGetValue("drupal_user", out var drupalUserObj) && drupalUserObj is JsonElement drupalUser)
//            {
//                if (drupalUser.TryGetProperty("uid", out var uid) && uid.ValueKind == JsonValueKind.String)
//                    identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, uid.GetString()!));
//                if (drupalUser.TryGetProperty("name", out var name) && name.ValueKind == JsonValueKind.String)
//                    identity.AddClaim(new Claim(ClaimTypes.Name, name.GetString()!));
//                if (drupalUser.TryGetProperty("email", out var email) && email.ValueKind == JsonValueKind.String)
//                    identity.AddClaim(new Claim(ClaimTypes.Email, email.GetString()!));
//                if (drupalUser.TryGetProperty("is_admin", out var isAdmin))
//                    identity.AddClaim(new Claim("is_admin", isAdmin.ToString()));
//                if (drupalUser.TryGetProperty("qlnext_user_id", out var qlnextUserId) && qlnextUserId.ValueKind == JsonValueKind.String)
//                    identity.AddClaim(new Claim("qlnext_user_id", qlnextUserId.GetString()!));
//                if (drupalUser.TryGetProperty("alias", out var alias) && alias.ValueKind == JsonValueKind.String)
//                    identity.AddClaim(new Claim("alias", alias.GetString()!));
//                if (drupalUser.TryGetProperty("image", out var image) && image.ValueKind == JsonValueKind.String)
//                    identity.AddClaim(new Claim("image", image.GetString()!));
//                if (drupalUser.TryGetProperty("status", out var status) && status.ValueKind == JsonValueKind.String)
//                    identity.AddClaim(new Claim("status", status.GetString()!));
//                if (drupalUser.TryGetProperty("permissions", out var permissions) && permissions.ValueKind == JsonValueKind.Array)
//                {
//                    foreach (var perm in permissions.EnumerateArray())
//                        if (perm.ValueKind == JsonValueKind.String)
//                            identity.AddClaim(new Claim("permission", perm.GetString()!));
//                }
//                if (drupalUser.TryGetProperty("roles", out var roles) && roles.ValueKind == JsonValueKind.Array)
//                {
//                    foreach (var role in roles.EnumerateArray())
//                        if (role.ValueKind == JsonValueKind.String)
//                            identity.AddClaim(new Claim(ClaimTypes.Role, role.GetString()!));
//                }
//            }

//            return new ClaimsPrincipal(identity);
//        }
//        else
//        {
//            return validatedPrincipal;
//        }
//    }
//    catch
//    {
//        return new ClaimsPrincipal(new ClaimsIdentity());
        
//    }
//}