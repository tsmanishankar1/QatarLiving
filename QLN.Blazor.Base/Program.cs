using GoogleAnalytics.Blazor;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor.Services;
using QLN.Web.Shared;
using QLN.Web.Shared.Contracts;
using QLN.Web.Shared.MockServices;
using QLN.Web.Shared.Pages;
using QLN.Web.Shared.Services;
using QLN.Web.Shared.Services.Interface;

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

builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(provider => provider.GetRequiredService<CustomAuthStateProvider>());

builder.Services.AddAuthorizationCore();

// builder.Services.AddCascadingAuthenticationState();

builder.Services.AddHttpClient<ApiService>();
builder.Services.AddWebSharedServices(builder.Configuration);
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
// app.UseAuthentication();
// app.UseAuthorization();
app.UseAntiforgery();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();


app.Run();

