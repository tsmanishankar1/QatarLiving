using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor.Extensions;
using MudBlazor.Services;
using NLog;
using NLog.Web;
using QLN.ContentBO.WebUI.Handlers;
using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Services;

// Early init of NLog to allow startup and exception logging, before host is built
var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
logger.Debug("init main");
try
{
    var builder = WebApplication.CreateBuilder(args);
    var contentBOAPIURL = builder.Configuration["ServiceUrlPaths:ContentBOAPI"];
    if (string.IsNullOrWhiteSpace(contentBOAPIURL))
    {
        throw new InvalidOperationException("Content Back Office API URL is missing in the configuration file.");
    }

    // Add services to the container.

    builder.Services.AddRazorPages();

    builder.Services.AddServerSideBlazor();

    builder.Services.AddMudServices();
    builder.Services.AddMudServicesWithExtensions();

    builder.Services.AddAuthentication();

    // Add this before registering AuthenticationStateProvider

    builder.Services.AddHttpContextAccessor();

    builder.Services.AddScoped<CookieAuthStateProvider>();

    builder.Services.AddScoped<AuthenticationStateProvider>(provider => provider.GetRequiredService<CookieAuthStateProvider>());

    builder.Services.AddCascadingAuthenticationState();

    builder.Services.AddAuthorizationCore();

    builder.Services.AddTransient<JwtTokenHeaderHandler>();

    builder.Services.Configure<NavigationPath>(

    builder.Configuration.GetSection("NavigationPath"));

    builder.Services.AddHttpClient<INewsService, NewsService>(client =>

    {

        client.BaseAddress = new Uri(contentBOAPIURL);

    }).AddHttpMessageHandler<JwtTokenHeaderHandler>();

    builder.Services.AddHttpClient<IEventsService, EventsService>(client =>
    {
        client.BaseAddress = new Uri(contentBOAPIURL);
    }).AddHttpMessageHandler<JwtTokenHeaderHandler>();

    builder.Services.AddHttpClient<ICommunityService, CommunityService>(client =>
    {
        client.BaseAddress = new Uri(contentBOAPIURL);
    }).AddHttpMessageHandler<JwtTokenHeaderHandler>();

    builder.Services.AddHttpClient<IReportService, ReportService>(client =>
    {
        client.BaseAddress = new Uri(contentBOAPIURL);
    }).AddHttpMessageHandler<JwtTokenHeaderHandler>();


    builder.Services.AddHttpClient<IDailyLivingService, DailyService>(client =>
    {
        client.BaseAddress = new Uri(contentBOAPIURL);
    }).AddHttpMessageHandler<JwtTokenHeaderHandler>();
    builder.Services.AddHttpClient<IClassifiedService, ClassifiedService>(client =>
    {
        client.BaseAddress = new Uri(contentBOAPIURL);
    }).AddHttpMessageHandler<JwtTokenHeaderHandler>();
    builder.Services.AddHttpClient<IBannerService, BannerService>(client =>
    {
        client.BaseAddress = new Uri(contentBOAPIURL);
    }).AddHttpMessageHandler<JwtTokenHeaderHandler>();

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

    app.UseHttpsRedirection();

    app.MapStaticAssets();

    app.UseRouting();

    app.MapBlazorHub();
    app.MapFallbackToPage("/_Host");

    app.Run();
}
catch (Exception exception)
{
    // NLog: catch setup errors
    logger.Error(exception, "Stopped program because of exception");
    throw;
}

