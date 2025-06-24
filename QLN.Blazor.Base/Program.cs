using GoogleAnalytics.Blazor;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using MudBlazor;
using MudBlazor.Services;
using QLN.Web.Shared;
using QLN.Web.Shared.Contracts;
using QLN.Web.Shared.Middleware;
using QLN.Web.Shared.MockServices;
using QLN.Web.Shared.Models;
using QLN.Web.Shared.Pages;
using QLN.Web.Shared.Pages.Services;
using QLN.Web.Shared.Services;
using QLN.Web.Shared.Services.Interface;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var contentVerticalAPIUrl = builder.Configuration["ServiceUrlPaths:ContentVerticalAPI"];
var qatarLivingAPI = builder.Configuration["ServiceUrlPaths:QatarLivingAPI"];
var baseURL = builder.Configuration["ServiceUrlPaths:BaseURL"];

Console.WriteLine($"ContentVerticalAPI URL: {contentVerticalAPIUrl}");

if (string.IsNullOrWhiteSpace(contentVerticalAPIUrl))
{
    throw new InvalidOperationException("ContentVerticalAPI URL is missing in configuration.");
}

Console.WriteLine($"QatarLivingAPI URL: {qatarLivingAPI}");

if (string.IsNullOrWhiteSpace(qatarLivingAPI))
{
    throw new InvalidOperationException("QatarLivingAPI URL is missing in configuration.");
}

if (string.IsNullOrWhiteSpace(baseURL))
{
    throw new InvalidOperationException("BaseURL URL is missing in configuration.");
}

builder.Services.AddCors(options =>
{


    string[] origins = { 
                // add more as necessary
                contentVerticalAPIUrl,
                qatarLivingAPI,
                baseURL
    };

    // filter out distinct URLs
    origins = origins.Distinct().ToArray();

    options.AddDefaultPolicy(policy =>
    {
        policy
            .WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });

    Console.WriteLine("CORS Enabled for the following origins");
    foreach(var o in origins) { Console.WriteLine($"{o}"); }
});


builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    // Adjusting SignalR Timeouts
    .AddHubOptions(options =>
    {
        options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
        options.HandshakeTimeout = TimeSpan.FromSeconds(30);
        options.MaximumReceiveMessageSize = 1024 * 1024;//To Increase the MaximumReceiveMessageSize via HubOptions 1MB.

    });

builder.Services.AddMudServices();

builder.Services.AddLocalization();

builder.Services.AddHttpClient<INewsLetterSubscription, NewsLetterSubscriptionService>(client =>
{
    var newsLetterSubscriptionAPIUrl = builder.Configuration["ServiceUrlPaths:NewsletterSubscriptionAPI"];
    if (string.IsNullOrWhiteSpace(newsLetterSubscriptionAPIUrl))
    {
        throw new InvalidOperationException("NewsletterSubscriptionAPI URL is missing in configuration.");
    }
    client.BaseAddress = new Uri(newsLetterSubscriptionAPIUrl);
});

builder.Services.AddAuthentication();

// Add this before registering AuthenticationStateProvider
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CookieAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(provider => provider.GetRequiredService<CookieAuthStateProvider>());
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorizationCore();

builder.Services.AddTransient<JwtTokenHeaderHandler>();

// needs fixing
builder.Services.AddScoped<ICompanyProfileService, CompanyProfileService>();
builder.Services.AddScoped<SearchStateService>();

//builder.Services.AddHttpClient<IAdService, AdService>();
builder.Services.AddScoped<IAdService, AdMockService>();

var youtubeApiKey = builder.Configuration["YouTubeAPI:ApiKey"];
builder.Services.AddScoped(sp => new YouTubeApiService(youtubeApiKey));

builder.Services.Configure<NavigationPath>(
    builder.Configuration.GetSection("NavigationPath"));

// http clients
builder.Services.AddHttpClient<IQLAnalyticsService, QLAnalyticsService>(client =>
{
    var trackingConfig = builder.Configuration.GetSection("TrackingConfiguration").Get<TrackingConfiguration>();
    if (trackingConfig == null || string.IsNullOrWhiteSpace(trackingConfig.BaseUrl))
    {
        throw new InvalidOperationException("TrackingConfiguration is not properly configured.");
    }
    client.BaseAddress = new Uri(trackingConfig.BaseUrl);
});



builder.Services.AddHttpClient<ICommunityService, CommunityService>(client =>
{
    client.BaseAddress = new Uri(contentVerticalAPIUrl);
}).AddHttpMessageHandler<JwtTokenHeaderHandler>();

builder.Services.AddHttpClient<IPostInteractionService, PostInteractionService>(client =>
{
    client.BaseAddress = new Uri(contentVerticalAPIUrl);
}).AddHttpMessageHandler<JwtTokenHeaderHandler>();

builder.Services.AddHttpClient<IContentService, ContentService>(client =>
{
    client.BaseAddress = new Uri(contentVerticalAPIUrl);
}).AddHttpMessageHandler<JwtTokenHeaderHandler>();

builder.Services.AddHttpClient<INewsService, NewsService>(client =>
{
    client.BaseAddress = new Uri(contentVerticalAPIUrl);
}).AddHttpMessageHandler<JwtTokenHeaderHandler>();

builder.Services.AddHttpClient<IEventService, EventService>(client =>
{
    client.BaseAddress = new Uri(contentVerticalAPIUrl);
}).AddHttpMessageHandler<JwtTokenHeaderHandler>();

builder.Services.AddHttpClient<IClassifiedsServices, ClassifiedsServices>(client =>
{
    client.BaseAddress = new Uri(contentVerticalAPIUrl);
}).AddHttpMessageHandler<JwtTokenHeaderHandler>();

builder.Services.AddHttpClient<IPostDialogService, PostDialogService>(client =>
{
    client.BaseAddress = new Uri(qatarLivingAPI);
}).AddHttpMessageHandler<JwtTokenHeaderHandler>();

builder.Services.AddHttpClient<ISearchService, CommunitySearchService>(client =>
{
    client.BaseAddress = new Uri(qatarLivingAPI);
}).AddHttpMessageHandler<JwtTokenHeaderHandler>();

builder.Services.AddHttpClient<ApiService>(client =>
{
   var apiSettings = builder.Configuration.GetSection("ApiSettings").Get<ApiSettings>();
   if (apiSettings == null || string.IsNullOrWhiteSpace(apiSettings.BaseUrl))
   {
       throw new InvalidOperationException("ApiSettings is not properly configured.");
   }
   client.BaseAddress = new Uri(apiSettings.BaseUrl);
}).AddHttpMessageHandler<JwtTokenHeaderHandler>();

builder.Services.AddHttpClient<ISubscriptionService, SubscriptionService>(client =>
{
    client.BaseAddress = new Uri(baseURL);
}).AddHttpMessageHandler<JwtTokenHeaderHandler>();

builder.Services.AddHttpClient<IClassifiedDashboardService, ClassfiedDashboardService>(client =>
{
    client.BaseAddress = new Uri(baseURL);
}).AddHttpMessageHandler<JwtTokenHeaderHandler>();



builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ISimpleMemoryCache, SimpleMemoryCache>(); // add shared Banner Service

builder.Services.AddGBService(options =>
{
    options.TrackingId = builder.Configuration["GoogleAnalytics:TrackingId"];
});

var app = builder.Build();

string[] supportedCultures = ["en-US"];
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture(supportedCultures[0])
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

app.UseRequestLocalization(localizationOptions);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    //app.UseResponseCompression();
    // app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

//app.UseStaticFiles();
app.MapStaticAssets();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();


app.Run();

