using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor.Extensions;
using MudBlazor.Services;
using QLN.ContentBO.WebUI.Interfaces;
using MudExtensions.Services;
using QLN.ContentBO.WebUI.MockServices;
using QLN.ContentBO.WebUI.Services;

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
builder.Services.AddAuthentication();
builder.Services.AddMudExtensions();

builder.Services.AddHttpClient<INewsService, NewsMockService>(client =>
{
    client.BaseAddress = new Uri(contentBOAPIURL);
}).AddHttpMessageHandler<JwtTokenHeaderHandler>();
builder.Services.AddHttpClient<IEventsService, EventsMockService>(client =>
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

