using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.Cookies;
using QLN.Web.Shared;
using QLN.Web.Shared.Pages;
using QLN.Web.Shared;
using MudBlazor;
using MudBlazor.Services;
using QLN.Web.Shared.Services;
using Microsoft.AspNetCore.Components.Authorization;
using QLN.Web.Shared.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddMudServices();
// builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(Options=>{
//     Options.Cookie.Name = "auth-name";
//     Options.LoginPath = "/login";
//     Options.Cookie.MaxAge=TimeSpan.FromMinutes(10);

// });
builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(provider => provider.GetRequiredService<CustomAuthStateProvider>());
builder.Services.AddAuthorizationCore();
// builder.Services.AddCascadingAuthenticationState();
builder.Services.Configure<ApiSettings>(
    builder.Configuration.GetSection("ApiSettings"));

builder.Services.AddHttpClient<ApiService>();
builder.Services.AddScoped<ISubscriptionService,SubscriptionService>();
// builder.Services.AddWebSharedServices(builder.Configuration);

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
