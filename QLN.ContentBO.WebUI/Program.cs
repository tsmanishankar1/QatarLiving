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
    var BOAPIBaseUrl = builder.Configuration["ServiceUrlPaths:ContentBOAPI"];
    if (string.IsNullOrWhiteSpace(BOAPIBaseUrl))
    {
        throw new InvalidOperationException("Content Back Office API URL is missing in the configuration file.");
    }

    // Add services to the container.

    builder.Services.AddRazorPages();

    builder.Services.AddServerSideBlazor();

    builder.Services.AddMudServices();
    builder.Services.AddMudServicesWithExtensions();
    builder.Services.AddMudExtensions();

    builder.Services.AddAuthentication();

    // Add this before registering AuthenticationStateProvider

    builder.Services.AddHttpContextAccessor();

    builder.Services.AddScoped<CustomAuthStateProvider>();

    builder.Services.AddScoped<AuthenticationStateProvider>(provider => provider.GetRequiredService<CustomAuthStateProvider>());

    builder.Services.AddCascadingAuthenticationState();

    builder.Services.AddAuthorizationCore(options =>
    {
        options.AddPolicy("AdminOnly", policy => policy.RequireRole("administrato"));
    });

    builder.Services.AddTransient<CustomHttpMessageHandler>();

    builder.Services.Configure<NavigationPath>(

    builder.Configuration.GetSection("NavigationPath"));

    builder.Services.AddHttpClient<INewsService, NewsService>(client =>

    {

        client.BaseAddress = new Uri(BOAPIBaseUrl);

    }).AddHttpMessageHandler<CustomHttpMessageHandler>();

    builder.Services.AddHttpClient<IEventsService, EventsService>(client =>
    {
        client.BaseAddress = new Uri(BOAPIBaseUrl);
    }).AddHttpMessageHandler<CustomHttpMessageHandler>();

    builder.Services.AddHttpClient<ICommunityService, CommunityService>(client =>
    {
        client.BaseAddress = new Uri(BOAPIBaseUrl);
    }).AddHttpMessageHandler<CustomHttpMessageHandler>();

    builder.Services.AddHttpClient<IReportService, ReportService>(client =>
    {
        client.BaseAddress = new Uri(BOAPIBaseUrl);
    }).AddHttpMessageHandler<CustomHttpMessageHandler>();


    builder.Services.AddHttpClient<IDailyLivingService, DailyService>(client =>
    {
        client.BaseAddress = new Uri(BOAPIBaseUrl);
    }).AddHttpMessageHandler<CustomHttpMessageHandler>();

    builder.Services.AddHttpClient<IBannerService, BannerService>(client =>
    {
        client.BaseAddress = new Uri(BOAPIBaseUrl);
    }).AddHttpMessageHandler<CustomHttpMessageHandler>();
    builder.Services.AddHttpClient<IServiceBOService, ServicesBOService>(client =>
    {
        client.BaseAddress = new Uri(BOAPIBaseUrl);
    }).AddHttpMessageHandler<CustomHttpMessageHandler>();

    builder.Services.AddHttpClient<IClassifiedService, ClassifiedService>(client =>
    {
        client.BaseAddress = new Uri(BOAPIBaseUrl);
    }).AddHttpMessageHandler<CustomHttpMessageHandler>();


    builder.Services.AddHttpClient<IFileUploadService, FileUploadService>(client =>
    {
        client.BaseAddress = new Uri(BOAPIBaseUrl);
    }).AddHttpMessageHandler<CustomHttpMessageHandler>();

    builder.Services.AddHttpClient<IDrupalUserService, DrupalUserService>(client =>
     {
         client.BaseAddress = new Uri(BOAPIBaseUrl);
     }).AddHttpMessageHandler<CustomHttpMessageHandler>();


    builder.Services.AddHttpClient<IStoresService, StoresService>(client =>
    {
        client.BaseAddress = new Uri(BOAPIBaseUrl);
    }).AddHttpMessageHandler<CustomHttpMessageHandler>();

    builder.Services.AddHttpClient<IPrelovedService, PrelovedService>(client =>
    {
        client.BaseAddress = new Uri(BOAPIBaseUrl);
    }).AddHttpMessageHandler<CustomHttpMessageHandler>();

    builder.Services.AddHttpClient<IItemService, ItemService>(client =>
    {
        client.BaseAddress = new Uri(BOAPIBaseUrl);
    }).AddHttpMessageHandler<CustomHttpMessageHandler>();

    builder.Services.AddHttpClient<ICollectiblesService, CollectiblesService>(client =>
    {
        client.BaseAddress = new Uri(BOAPIBaseUrl);
    }).AddHttpMessageHandler<CustomHttpMessageHandler>();

    builder.Services.AddHttpClient<IDealsService, DealsService>(client =>
    {
        client.BaseAddress = new Uri(BOAPIBaseUrl);
    }).AddHttpMessageHandler<CustomHttpMessageHandler>();

    builder.Services.AddHttpClient<ITokenService, TokenService>(client =>
    {
        client.BaseAddress = new Uri(BOAPIBaseUrl);
    }).AddHttpMessageHandler<CustomHttpMessageHandler>();

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

