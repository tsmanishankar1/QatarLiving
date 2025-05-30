using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.Cookies;
using QLN.Web.Shared;
using QLN.Web.Shared.Pages;
using QLN.Web.Shared;
using MudBlazor;
using MudBlazor.Services;
using QLN.Web.Shared.Services;
using QLN.Web.Shared.Services.Interface;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddMudServices();

var contentVerticalAPIUrl = builder.Configuration["ServiceUrlPaths:ContentVerticalAPI"];
if (string.IsNullOrWhiteSpace(contentVerticalAPIUrl))
{
    throw new InvalidOperationException("ContentVerticalAPI URL is missing in configuration.");
}
var newsLetterSubscriptionAPIUrl = builder.Configuration["ServiceUrlPaths:NewsletterSubscriptionAPI"];
if (string.IsNullOrWhiteSpace(newsLetterSubscriptionAPIUrl))
{
    throw new InvalidOperationException("NewsletterSubscriptionAPI URL is missing in configuration.");
}

// });

#region Authentication - Cookie configuration
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.Cookie.Name = "qat";
    options.Events = new CookieAuthenticationEvents
    {
        OnValidatePrincipal = async context =>
        {
            var jwt = context.Request.Cookies["qat"];
            if (string.IsNullOrEmpty(jwt))
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync();
                return;
            }

            var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = false,
                SignatureValidator = (token, parameters) =>
                {
                    // Bypass signature validation for demo purposes
                    return new JwtSecurityToken(token);
                },
                //ValidIssuer = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>()["Jwt:Issuer"],
                //ValidAudience = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>()["Jwt:Audience"],
                //IssuerSigningKey = new SymmetricSecurityKey(
                //    Encoding.UTF8.GetBytes(
                //        context.HttpContext.RequestServices.GetRequiredService<IConfiguration>()["Jwt:Key"]!
                //    )
                //),
                RoleClaimType = ClaimTypes.Role,
                NameClaimType = ClaimTypes.Name
            };

            try
            {
                var principal = tokenHandler.ValidateToken(jwt, validationParameters, out var validatedToken);
                context.ReplacePrincipal(principal);
                context.ShouldRenew = false;
            }
            catch
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync();
            }
        }
    };
});
#endregion

// Add this before registering AuthenticationStateProvider
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CookieAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(provider => provider.GetRequiredService<CookieAuthStateProvider>());
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddScoped<ICommunityService,CommunityMockService>();

//builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<ICompanyProfileService, CompanyProfileService>();
builder.Services.Configure<ApiSettings>(
    builder.Configuration.GetSection("ApiSettings"));

builder.Services.Configure<NavigationPath>(
    builder.Configuration.GetSection("NavigationPath"));


builder.Services.AddHttpClient<ICommunityService, CommunityService>(client =>
{
    client.BaseAddress = new Uri(contentVerticalAPIUrl);
});
builder.Services.AddHttpClient<INewsLetterSubscription, NewsLetterSubscriptionService>(client =>
{
    client.BaseAddress = new Uri(newsLetterSubscriptionAPIUrl);
});

//builder.Services.AddHttpClient<IAdService, AdService>();
builder.Services.AddScoped<IAdService, AdMockService>();
builder.Services.AddHttpClient<IPostInteractionService, PostInteractionService>();
builder.Services.AddScoped<IContentService, ContentService>();
builder.Services.AddScoped<INewsService, NewsService>();
builder.Services.AddHttpClient<IEventService, EventService>(client =>
{
    client.BaseAddress = new Uri(contentVerticalAPIUrl);
});



builder.Services.AddHttpContextAccessor();

builder.Services.AddGBService(options =>
{
    options.TrackingId = builder.Configuration["GoogleAnalytics:TrackingId"];
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();


app.Run();

