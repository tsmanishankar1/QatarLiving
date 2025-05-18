using MudBlazor.Services;
using QLN.AIPOV.Frontend.ChatBot.Components;
using QLN.AIPOV.FrontEnd.ChatBot;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;

// Add MudBlazor services
builder.Services.AddMudServices();

builder.Services.AddServices().AddHttpClientServices(configuration);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
